using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using WebSocket4Net;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;

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

      Orders = new Orders();
      MyOrders = new Orders();

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

    internal decimal ToEth(dynamic dynamic, object decimals)
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

    internal async Task<decimal> GetBalance(string token, string user, int decimalPlacesToUnit)
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

      return unitConversion.FromWei(balance, decimalPlacesToUnit);
    }

    internal async Task<decimal> GetEtherDeltaBalance(string token, string user, int decimalPlacesToUnit)
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

      return unitConversion.FromWei(balance, decimalPlacesToUnit);
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

        var sells = ((JArray)orders.sells).Where(_ => _["tokenGive"] != null && _["tokenGive"].ToString() == Config.Token).ToList();
        if (sells != null && sells.Count > 0)
        {
          Orders.Sells = sells;
        }

        var buys = ((JArray)orders.buys).Where(_ => _["tokenGet"] != null && _["tokenGet"].ToString() == Config.Token).ToList();
        if (buys != null && buys.Count > 0)
        {
          Orders.Buys = buys;
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

  public class Orders
  {
    public dynamic Sells { get; set; }
    public dynamic Buys { get; set; }
  }
}