using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WebSocket4Net;
using System.Text;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Signer;
using Nethereum.ABI.Encoders;

namespace EhterDelta.Bots.Dontnet
{
    public class Service
    {
        private const string ZeroToken = "0x0000000000000000000000000000000000000000";
        private WebSocket socket;
        const int socketTimeout = 20000;
        private ILogger logger;
        private bool gotMarket = false;

        public Service(EtherDeltaConfiguration config, ILogger configLogger)
        {
            logger = configLogger;
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

            var prms = new Parameter[] {
                new Parameter("address",1),
                new Parameter("address",1),
                new Parameter("uint256",1),
                new Parameter("address",1),
                new Parameter("uint256",1),
                new Parameter("uint256",1),
                new Parameter("uint256",1),
            };

            var pe = new ParametersEncoder();
            var data = pe.EncodeParameters(prms, plainData);

            var ms = new MessageSigner();
            var signature = ms.HashAndSign(data, Config.PrivateKey);

            var ethEcdsa = MessageSigner.ExtractEcdsaSignature(signature);

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
                V = ethEcdsa.V,
                R = ethEcdsa.R.ToHex(true),
                S = ethEcdsa.S.ToHex(true),
            };

            return order;
        }

        internal void Close()
        {
            Log("Closing ...");
            if (socket != null && socket.State == WebSocketState.Open)
            {
                socket.Close();
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
                    gotMarket = true;
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

            var ea = e as ClosedEventArgs;
            if (ea != null)
            {
                Log(ea.Code.ToString());
                if (ea.Code == 1005) // no reason given
                {
                    Log("Reconnecting...");
                    InitSocket();
                }
            }
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

        private void SocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
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
            gotMarket = false;
            socket.Send(new Message
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
                while (!gotMarket)
                {
                    Task.Delay(1000).Wait();
                }
            });

            var completed = await Task.WhenAny(gotMarketTask, Task.Delay(socketTimeout));
            Log("Market Completed ...");

            if (!gotMarketTask.IsCompletedSuccessfully)
            {
                throw new TimeoutException("Get Market timeout");
            }
        }

        private void UpdateOrders(dynamic ordersObj)
        {
            var minOrderSize = 0.001m;
            var orders = ordersObj as JObject;
            if (orders == null)
            {
                return;
            }

            var sells = ((JArray)orders["sells"])
                .Where(jtoken =>
                    jtoken["tokenGive"] != null && jtoken["tokenGive"].ToString() == Config.Token &&
                    jtoken["ethAvailableVolumeBase"] != null && jtoken["ethAvailableVolumeBase"].ToObject<decimal>() > minOrderSize &&
                    (jtoken["deleted"] == null || jtoken["deleted"].ToObject<bool>() == false)
                )
              .Select(jtoken => Order.FromJson(jtoken));

            if (sells != null && sells.Count() > 0)
            {
                Log($"Got {sells.Count()} sells");
                Orders.Sells = Orders.Sells.Union(sells);
                MyOrders.Sells = MyOrders.Sells.Union(sells.Where(s => s.User == Config.User));
            }

            var buys = ((JArray)orders["buys"])
                .Where(jtoken =>
                    jtoken["tokenGet"] != null && jtoken["tokenGet"].ToString() == Config.Token &&
                    jtoken["ethAvailableVolumeBase"] != null && jtoken["ethAvailableVolumeBase"].ToObject<decimal>() > minOrderSize &&
                    (jtoken["deleted"] == null || jtoken["deleted"].ToObject<bool>() == false)
                )
              .Select(jtoken => Order.FromJson(jtoken));

            if (buys != null && buys.Count() > 0)
            {
                Log($"Got {buys.Count()} buys");
                Orders.Buys = Orders.Buys.Union(buys);
                MyOrders.Buys = MyOrders.Buys.Union(buys.Where(s => s.User == Config.User));
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
              .Select(jtoken => Trade.FromJson(jtoken));

            Log($"Parsed {tradesArray.Count()} trades");
            Trades = Trades.Union(tradesArray);
            Log($"total {Trades.Count()} trades");
        }


        private void Log(string message)
        {
            if (logger != null)
            {
                logger.Log(message);
            }
        }

        private void InitSocket()
        {
            socket = new WebSocket(Config.SocketUrl);
            socket.Opened += SocketOpened;
            socket.Error += SocketError;
            socket.Closed += SocketClosed;
            socket.MessageReceived += SocketMessageReceived;
            socket.OpenAsync().Wait();
        }
    }

}