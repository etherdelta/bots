using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.ABI.Encoders;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.KeyStore.Crypto;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace EhterDelta.Bots.DotNet
{
    public class Service
    {
        public const string ZeroToken = "0x0000000000000000000000000000000000000000";
        private WebSocket _socket;
        private const int SocketTimeout = 20000;
        private readonly ILogger _logger;
        private bool _gotMarket;

        public Service(EtherDeltaConfiguration config, ILogger configLogger)
        {
            this._logger = configLogger;
            Log("Starting");

            Orders = new Orders
            {
                Sells = new List<Order>(),
                Buys = new List<Order>()
            };

            MyOrders = new Orders
            {
                Sells = new List<Order>(),
                Buys = new List<Order>()
            };

            Trades = new List<Trade>();
            MyTrades = new List<Trade>();

            Config = config;
            Web3 = new Web3(config.Provider);
            var addressEtherDelta = Web3.ToChecksumAddress(config.AddressEtherDelta);

            // TODO: check file exists
            var abi = File.ReadAllText(config.AbiFile);
            EtherDeltaContract = Web3.Eth.GetContract(abi, addressEtherDelta);

            var tokenAbi = File.ReadAllText(config.TokenFile);
            EthContract = Web3.Eth.GetContract(tokenAbi, Config.Token);

            InitSocket();
        }

        public EtherDeltaConfiguration Config { get; }
        public Web3 Web3 { get; }
        public Contract EtherDeltaContract { get; }
        public Contract EthContract { get; }
        public Orders Orders { get; set; }
        public Orders MyOrders { get; set; }
        public IEnumerable<Trade> Trades { get; set; }
        public IEnumerable<dynamic> MyTrades { get; set; }

        internal async Task<TransactionReceipt> TakeOrder(Order order, BigInteger amount)
        {
            var funvtionInput = new object[] {
                order.TokenGet,
                order.AmountGet.Value,
                order.TokenGive,
                order.AmountGive.Value,
                order.Expires,
                order.Nonce,
                order.User,
                order.V,
                order.R.HexToByteArray(),
                order.S.HexToByteArray(),
                amount
            };

            var fnTest = EtherDeltaContract.GetFunction("testTrade");
            var willPass = await fnTest.CallAsync<bool>(funvtionInput);

            if (!willPass)
            {
                Log("Order will fail");
                throw new Exception("Order will fail");
            }

            var fnTrade = EtherDeltaContract.GetFunction("trade");
            var data = fnTrade.GetData(funvtionInput);

            var txCount = await Web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(Config.User);
            var encoded = Web3.OfflineTransactionSigner.SignTransaction(Config.PrivateKey, Config.AddressEtherDelta, amount,
                txCount, Config.GasPrice, Config.GasLimit, data);

            var txId = await Web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(encoded.EnsureHexPrefix());
            var receipt = await Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId);
            return receipt;
        }

        internal Order GetBestAvailableSell()
        {
            return Orders.Sells.FirstOrDefault();
        }

        internal Order GetBestAvailableBuy()
        {
            return Orders.Buys.FirstOrDefault();
        }

        internal async Task<BigInteger> GetBlockNumber()
        {
            return await Web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        }

        internal Order CreateOrder(OrderType orderType, BigInteger expires, BigInteger price, BigInteger amount)
        {
            var amountBigNum = orderType == OrderType.Buy ? amount / price : amount;
            var amountBaseBigNum = amount * price;
            var contractAddr = Config.AddressEtherDelta;
            var tokenGet = orderType == OrderType.Buy ? Config.Token : ZeroToken;
            var tokenGive = orderType == OrderType.Sell ? Config.Token : ZeroToken;
            var amountGet = orderType == OrderType.Buy ? amountBigNum : amountBaseBigNum;
            var amountGive = orderType == OrderType.Sell ? amountBigNum : amountBaseBigNum;
            var orderNonce = new Random().Next();

            var plainData = new object[] {
                Config.AddressEtherDelta,
                tokenGive,
                amountGet,
                tokenGive,
                amountGive,
                expires,
                orderNonce
            };

            var prms = new[] {
                new Parameter("address",1),
                new Parameter("address",1),
                new Parameter("uint256",1),
                new Parameter("address",1),
                new Parameter("uint256",1),
                new Parameter("uint256",1),
                new Parameter("uint256",1)
            };

            var data = SolidityPack(plainData, prms);
            var keystoreCrypto = new KeyStoreCrypto();
           
            var hashed = keystoreCrypto.CalculateSha256Hash(data);


            var signer = new EthereumMessageSigner();
            var newHash = signer.HashPrefixedMessage(hashed);

           
            var signatureRaw = signer.SignAndCalculateV(newHash, Config.PrivateKey);
           

            var order = new Order
            {
                AmountGet = new HexBigInteger(amountGet),
                AmountGive = new HexBigInteger(amountGive),
                TokenGet = tokenGet,
                TokenGive = tokenGive,
                ContractAddr = contractAddr,
                Expires = expires,
                Nonce = orderNonce,
                User = Config.User,
                V = signatureRaw.V,
                R = signatureRaw.R.ToHex(true),
                S = signatureRaw.S.ToHex(true)
            };

            return order;
        }
        private byte[] SolidityPack(object[] paramsArray, Parameter[] paramsTypes)
        {
            var intTypeEncoder = new IntTypeEncoder();
            var cursor = new List<byte>();
            for (var i = 0; i < paramsArray.Length; i++)
            {

                if (paramsTypes[i].Type == "address")
                {
                    cursor.AddRange(EncodePacked(paramsArray[i]));
                }
                else if (paramsTypes[i].Type == "uint256")
                {
                    cursor.AddRange(intTypeEncoder.Encode(paramsArray[i]));
                }

            }

            return cursor.ToArray();
        }

        private byte[] EncodePacked(object value)
        {
            var strValue = value as string;

            if (strValue == null) throw new Exception("Invalid type for address expected as string");

            if (strValue != null
                && !strValue.StartsWith("0x", StringComparison.Ordinal))
                value = "0x" + value;

            if (strValue.Length == 42) return strValue.HexToByteArray();

            throw new Exception("Invalid address (should be 20 bytes length): " + strValue);
        }
    

    internal void Close()
        {
            Log("Closing ...");
            if (this._socket != null && this._socket.State == WebSocketState.Open)
            {
                this._socket.Close();
            }
        }

        private void SocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message message = Message.ParseMessage(e.Message);
            Log($"Got {message.Event} event");
            switch (message.Event)
            {
                case "market":
                    UpdateOrders(message.Data.orders);
                    UpdateTrades(message.Data.trades);
                    this._gotMarket = true;
                    break;
                case "trades":
                    UpdateTrades(message.Data);
                    break;
                case "orders":
                    UpdateOrders(message.Data);
                    break;
                default:
                    Log(e.Message);
                    break;
            }
        }

        private void SocketClosed(object sender, EventArgs e)
        {
            Log("SOCKET CLOSED");
            Log(e.GetType().ToString());

            if (!(e is ClosedEventArgs ea)) return;

            this.Log(ea.Code.ToString());

            //Valid reason returned
            if (ea.Code != 1005) return;

            this.Log("Reconnecting...");
            this.InitSocket();
        }

        internal async Task<BigInteger> GetBalance(string token, string user)
        {
            BigInteger balance = 0;
            user = Web3.ToChecksumAddress(user);

            try
            {
                if (token == "ETH")
                {
                    balance = await Web3.Eth.GetBalance.SendRequestAsync(user);
                    Log("ETH - GET BALANCE");
                }
                else
                {
                    token = Web3.ToChecksumAddress(token);
                    var tokenFunction = EthContract.GetFunction("balanceOf");
                    balance = await tokenFunction.CallAsync<BigInteger>(user);
                    Log("TOKEN - GET BALANCE");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            return balance;
        }

        internal async Task<BigInteger> GetEtherDeltaBalance(string token, string user)
        {
            BigInteger balance = 0;

            try
            {
                if (token == "ETH")
                {
                    token = ZeroToken;
                }

                var tokenFunction = EtherDeltaContract.GetFunction("balanceOf");
                balance = await tokenFunction.CallAsync<BigInteger>(token, user);
                Log("ETHER DELTA - GET BALANCE");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            return balance;
        }

        private void SocketError(object sender, ErrorEventArgs e)
        {
            Log("SOCKET ERROR: ");
            Log(e.Exception.Message);
        }

        private void SocketOpened(object sender, EventArgs e)
        {
            Log("SOCKET Connected");
        }

        public async Task WaitForMarket()
        {
            Log("Wait for Market");
            this._gotMarket = false;
            this._socket.Send(new Message
            {
                Event = "getMarket",
                Data = new
                {
                    token = Config.Token,
                    user = Config.User
                }
            }.ToString());

            var gotMarketTask = Task.Run(() =>
            {
                while (!this._gotMarket)
                {
                    Task.Delay(1000).Wait();
                }
            });

            await Task.WhenAny(gotMarketTask, Task.Delay(SocketTimeout));
            Log("Market Completed ...");

            if (!gotMarketTask.IsCompletedSuccessfully)
            {
                throw new TimeoutException("Get Market timeout");
            }
        }

        private void UpdateOrders(dynamic ordersObj)
        {
            const decimal minOrderSize = 0.001m;
            if (!(ordersObj is JObject orders))
            {
                return;
            }

            var sells = ((JArray) orders["sells"])
                    .Where(jtoken =>
                               jtoken["tokenGive"] != null && jtoken["tokenGive"].ToString() == Config.Token &&
                               jtoken["ethAvailableVolumeBase"] != null && jtoken["ethAvailableVolumeBase"].ToObject<decimal>() > minOrderSize &&
                               (jtoken["deleted"] == null || jtoken["deleted"].ToObject<bool>() == false)
                          )
                    .Select(Order.FromJson)
                    .ToList();

            if (sells.Any())
            {
                Log($"Got {sells.Count} sells");
                Orders.Sells = Orders.Sells.Union(sells);
                MyOrders.Sells = MyOrders.Sells.Union(sells.Where(s => s.User == Config.User));
            }

            var buys = ((JArray) orders["buys"])
                    .Where(jtoken =>
                               jtoken["tokenGet"] != null && jtoken["tokenGet"].ToString() == Config.Token &&
                               jtoken["ethAvailableVolumeBase"] != null && jtoken["ethAvailableVolumeBase"].ToObject<decimal>() > minOrderSize &&
                               (jtoken["deleted"] == null || jtoken["deleted"].ToObject<bool>() == false)
                          )
                    .Select(Order.FromJson)
                    .ToList();

            if (!buys.Any()) return;
            {
                this.Log($"Got {buys.Count} buys");
                this.Orders.Buys = this.Orders.Buys.Union(buys);
                this.MyOrders.Buys = this.MyOrders.Buys.Union(buys.Where(s => s.User == this.Config.User));
            }
        }

        private void UpdateTrades(JArray trades)
        {
            if (trades == null)
            {
                return;
            }

            Log($"Got {trades.Count} trades");
            var tradesArray = trades
                    .Where(jtoken =>
                               jtoken["txHash"] != null &&
                               jtoken["amount"] != null && jtoken["amount"].ToObject<decimal>() > 0m
                          )
                    .Select(Trade.FromJson)
                    .ToList();

            Log($"Parsed {tradesArray.Count} trades");
            Trades = Trades.Union(tradesArray);
            Log($"total {Trades.Count()} trades");
        }


        private void Log(string message)
        {
            this._logger?.Log(message);
        }

        private void InitSocket()
        {
            this._socket = new WebSocket(Config.SocketUrl);
            this._socket.Opened += SocketOpened;
            this._socket.Error += SocketError;
            this._socket.Closed += SocketClosed;
            this._socket.MessageReceived += SocketMessageReceived;
            this._socket.OpenAsync().Wait();
        }
    }

}