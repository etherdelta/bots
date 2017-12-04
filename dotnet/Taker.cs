using Nethereum.Web3;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using Nethereum.JsonRpc.Client;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EhterDelta.Bots.Dontnet
{
  class Taker
  {
    public Service service { get; private set; }

    public Taker(ILogger logger = null)
    {
      var config = new EtherDeltaConfiguration
      {
        SocketUrl = "wss://socket.etherdelta.com/socket.io/?transport=websocket",
        Provider = "https://mainnet.infura.io/Ky03pelFIxoZdAUsr82w",
        AddressEtherDelta = "0x8d12a197cb00d4747a1fe03395095ce2a5cc6819",
        AbiFile = "../contracts/etherdelta.json",
        TokenFile = "../contracts/token.json",
        Token = "0x8f3470a7388c05ee4e7af3d01d8c722b0ff52374",
        User = "0x6e9bcD9f07d3961444555967D5F8ACaaae1559f4",
        UnitDecimals = 18
      };

      service = new Service(config, logger);

      GetMarket().Wait();

      Console.WriteLine("Order book");

      PrintOrders();
      PrintTrades();

      Task[] tasks = new[] {
        GetBalanceAsync("ETH", config.User, config.UnitDecimals),
        GetBalanceAsync(config.Token, config.User, config.UnitDecimals),
        GetEtherDeltaBalance("ETH", config.User, config.UnitDecimals),
        GetEtherDeltaBalance(config.Token, config.User, config.UnitDecimals)
      };

      Task.WhenAll(tasks).Wait();
    }

    private async Task<decimal> GetEtherDeltaBalance(string token, string user, int unitDecimals)
    {
      decimal balance = 0;
      try
      {
        balance = await service.GetEtherDeltaBalance(token, user, unitDecimals);
        Console.WriteLine($"Ether Delta {token} balance: {balance}");
      }
      catch (TimeoutException)
      {
        Console.WriteLine("Could not get balance");
      }
      return balance;
    }

    private async Task<decimal> GetBalanceAsync(string token, string user, int unitDecimals)
    {
      decimal balance = 0;

      try
      {
        balance = await service.GetBalance(token, user, unitDecimals);
        Console.WriteLine($"Wallet {token} balance: {balance}");
      }
      catch (TimeoutException)
      {
        Console.WriteLine("Could not get balance");
      }

      return balance;
    }

    private void PrintTrades()
    {
      Console.WriteLine("Recent trades");
      int numTrades = 10;

      var trades = service.Trades;

      if (trades != null && trades.GetType() == typeof(JArray))
      {
        trades = ((JArray)trades).Take(numTrades).ToArray();
        foreach (var trade in trades)
        {
          var tradePrice = (double)((JValue)trade.price);
          var tradeAmount = (double)((JValue)trade.amount);
          var tradeDate = (DateTime)((JValue)trade.date);

          Console.WriteLine($"{tradeDate.ToLocalTime()} {trade.side} {tradeAmount.ToString("N3")} @ {tradePrice.ToString("N9")}");
        }
      }
    }

    private void PrintOrders()
    {
      int ordersPerSide = 10;

      List<JToken> sells = service.Orders != null ? service.Orders.Sells : null;
      List<JToken> buys = service.Orders != null ? service.Orders.Buys : null;

      if (sells == null || buys == null)
      {
        Console.WriteLine("No sell or buy orders");
        return;
      }

      sells = sells.Take(ordersPerSide).Reverse().ToList();
      buys = buys.Take(ordersPerSide).ToList();

      foreach (var item in sells)
      {
        Console.WriteLine(FormatItem(item));
      }

      if (buys.Count > 0 && sells.Count > 0)
      {
        var salesPrice = (double)(sells[sells.Count - 1]["price"]);
        var buysPrice = (double)(buys[0]["price"]);
        Console.WriteLine($"---- Spread ({(salesPrice - buysPrice).ToString("N9")}) ----");
      }
      else
      {
        Console.WriteLine("--------");
      }

      if (buys != null)
      {
        foreach (var item in buys)
        {
          Console.WriteLine(FormatItem(item));
        }
      }

    }

    private string FormatItem(dynamic item)
    {
      item.price = (double)item.price;
      item.ethAvailableVolume = (double)item.ethAvailableVolume;
      return $"{item.price.ToString("N9")} {item.ethAvailableVolume.ToString("N3")}";
    }

    private async Task GetMarket()
    {
      try
      {
        await service.WaitForMarket();
      }
      catch (TimeoutException)
      {
        Console.WriteLine("Could not get Market");
      }
    }

    ~Taker()
    {
      if (service != null)
      {
        service.Close();
      }
    }
  }
}