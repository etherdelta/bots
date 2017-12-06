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

namespace EhterDelta.Bots.Dontnet
{
    public class Service
    {
        private const string ZeroToken = "0x0000000000000000000000000000000000000000";
        private WebSocket socket;
        const int SocketMessageTimeout = 30000;
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

        private ILogger logger;

        public Service(EtherDeltaConfiguration config, ILogger configLogger)
        {
            logger = configLogger;
            Log("Starting");

            Orders = new Orders
            {
                Sells = new List<Order>(),
                Buys = new List<dynamic>()
            };

            MyOrders = new Orders
            {
                Sells = new List<Order>(),
                Buys = new List<dynamic>()
            };

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

        internal async Task TakeOrder(Order order, double fraction)
        {
            Console.WriteLine(order.AmountGet);
            var amount = 1 * fraction;

            // var maxGas = 250000;
            // var gasPriceWei = 1000000000;   // 1 Gwei

            var txCount = await Web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(Config.User);
            var fn = EtherDeltaContract.GetFunction("testTrade");

            var resp = await fn.CallAsync<bool>(
              order.TokenGet,
              order.AmountGet.Value,
              order.TokenGive,
              order.AmountGive.Value,
              order.Expires,
              txCount.Value,
              order.User,
              order.V,
              order.R,
              order.S,
               amount,
               Config.User
            );


            var encoded = Web3.OfflineTransactionSigner.SignTransaction(Config.PrivateKey, Config.AddressEtherDelta, 10,
                       txCount.Value);

            /**
            
            
       var privateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
       var senderAddress = "0x12890d2cce102216644c59daE5baed380d84830c";
       var receiveAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
       var web3 = new Web3Geth();

       var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(senderAddress);
       var encoded = Web3.OfflineTransactionSigner.SignTransaction(privateKey, receiveAddress, 10,
           txCount.Value);

       Assert.True(Web3.OfflineTransactionSigner.VerifyTransaction(encoded));

       Debug.WriteLine(Web3.OfflineTransactionSigner.GetSenderAddress(encoded));
       Assert.Equal(senderAddress.EnsureHexPrefix().ToLower(), Web3.OfflineTransactionSigner.GetSenderAddress(encoded).EnsureHexPrefix().ToLower());

       var txId = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync("0x" + encoded);
       var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId);
       while (receipt == null)
       {
           Thread.Sleep(1000);
           receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txId);
       }

       Assert.Equal(txId, receipt.TransactionHash);
       return true;
        */

        }

        internal Order GetBestAvailableSell()
        {
            return Orders.Sells.FirstOrDefault();
        }

        internal async Task<BigInteger> GetBlockNumber()
        {
            return await Web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        }

        internal async Task CreateOrder(OrderType orderType, BigInteger expires, BigInteger price, BigInteger amount)
        {
            await Task.Delay(1);

            var amountBigNum = amount;
            var amountBaseBigNum = amount * price;
            var contractAddr = Config.AddressEtherDelta;
            var tokenGet = orderType == OrderType.Buy ? Config.Token : ZeroToken;
            var tokenGive = orderType == OrderType.Sell ? Config.Token : ZeroToken;
            var amountGet = orderType == OrderType.Buy ? toWei(amountBigNum, Config.UnitDecimals) : toWei(amountBaseBigNum, Config.UnitDecimals);
            var amountGive = orderType == OrderType.Sell ? toWei(amountBigNum, Config.UnitDecimals) : toWei(amountBaseBigNum, Config.UnitDecimals);
            var orderNonce = new Random().Next();


            //new Nethereum.Signer.MessageSigner().ToString
        }

        internal BigInteger ToEth(dynamic dynamic, object decimals)
        {
            throw new NotImplementedException();
        }

        private string toWei(BigInteger amountBigNum, int unitDecimals)
        {
            throw new NotImplementedException();
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
            switch (message.Event)
            {
                case "market":
                    UpdateOrders(message.Data.orders);
                    UpdateTrades(message.Data.trades);
                    Market = message.Data;
                    break;
                default:
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

        internal async Task<decimal> GetBalance(string token, string user)
        {
            var unitConversion = new UnitConversion();
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

            return unitConversion.FromWei(balance, Config.UnitDecimals);
        }

        internal async Task<decimal> GetEtherDeltaBalance(string token, string user)
        {
            var unitConversion = new UnitConversion();
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

            return unitConversion.FromWei(balance, Config.UnitDecimals);
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
            Market = null;
            socket.Send(new Message
            {
                Event = "getMarket",
                Data = new
                {
                    token = Config.Token,
                    user = Config.User
                }
            }.ToString());

            var gotMarket = Task.Run(() =>
            {
                while (Market == null) { Task.Delay(1000); }
            });

            var completed = await Task.WhenAny(gotMarket, Task.Delay(SocketMessageTimeout));

            if (completed != gotMarket)
            {
                throw new TimeoutException("Get Market timeout");
            }
        }

        private void UpdateOrders(dynamic orders)
        {
            //var minOrderSize = 0.001;

            if (orders == null)
            {
                return;
            }

            if (orders.GetType() == typeof(JObject))
            {
                var sells = ((JArray)orders.sells)
                  .Where(_ => _["tokenGive"] != null && _["tokenGive"].ToString() == Config.Token)
                  .Select(_ => _.ToObject<Order>())
                  .ToList();

                if (sells != null && sells.Count() > 0)
                {
                    Orders.Sells = sells;
                }

                var buys = ((JArray)orders.buys).Where(_ => _["tokenGet"] != null && _["tokenGet"].ToString() == Config.Token).ToList();
                if (buys != null && buys.Count > 0)
                {
                    Orders.Buys = buys.ToList<dynamic>();
                }
            }

            // TODO: update MyOrders
        }

        private void UpdateTrades(dynamic trades)
        {
            if (trades == null)
            {
                return;
            }

            if (trades.GetType() == typeof(JArray))
            {
                Trades = trades;
            }
        }

        public EtherDeltaConfiguration Config { get; }
        public Web3 Web3 { get; }
        public Contract EtherDeltaContract { get; }
        public Contract EthContract { get; }
        public Orders Orders { get; set; }
        public Orders MyOrders { get; set; }
        public dynamic Trades { get; set; }
        public List<dynamic> MyTrades { get; set; }
        public dynamic Market { get; private set; }
    }
}