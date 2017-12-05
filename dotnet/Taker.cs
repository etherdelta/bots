using Nethereum.Web3;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using Nethereum.JsonRpc.Client;
using System;
using System.Numerics;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EhterDelta.Bots.Dontnet
{
  public class Taker
  {
    protected Service service { get; private set; }

    protected BigInteger BlockNumber { get; private set; }
    protected decimal EtherDeltaETH { get; private set; }
    protected decimal WalletETH { get; private set; }

    protected decimal EtherDeltaToken { get; private set; }
    protected decimal WalletToken { get; private set; }

    public Taker(EtherDeltaConfiguration config, ILogger logger = null)
    {
      Console.ForegroundColor = ConsoleColor.White;
      service = new Service(config, logger);

      //GetMarket().Wait();

      Task[] tasks = new[] {
        GetMarket(),
        GetBalanceAsync("ETH", config.User, config.UnitDecimals),
        GetBalanceAsync(config.Token, config.User, config.UnitDecimals),
        GetEtherDeltaBalance("ETH", config.User, config.UnitDecimals),
        GetEtherDeltaBalance(config.Token, config.User, config.UnitDecimals),
        GetBlockNumber()
      };

      Task.WhenAll(tasks).Wait();

      PrintOrders();
      PrintTrades();
      PrintWallet();
    }

    private async Task GetBlockNumber()
    {
      BlockNumber = await service.GetBlockNumber();
    }

    private async Task<decimal> GetEtherDeltaBalance(string token, string user, int unitDecimals)
    {
      decimal balance = 0;
      try
      {
        balance = await service.GetEtherDeltaBalance(token, user, unitDecimals);
      }
      catch (TimeoutException)
      {
        Console.WriteLine("Could not get balance");
      }

      if (token == "ETH")
      {
        EtherDeltaETH = balance;
      }
      else
      {
        EtherDeltaToken = balance;
      }
      return balance;
    }

    private async Task<decimal> GetBalanceAsync(string token, string user, int unitDecimals)
    {
      decimal balance = 0;

      try
      {
        balance = await service.GetBalance(token, user, unitDecimals);
      }
      catch (TimeoutException)
      {
        Console.WriteLine("Could not get balance");
      }

      if (token == "ETH")
      {
        WalletETH = balance;
      }
      else
      {
        WalletToken = balance;
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

          Console.ForegroundColor = trade.side == "sell" ? ConsoleColor.Red : ConsoleColor.Green;
          Console.WriteLine($"{tradeDate.ToLocalTime()} {trade.side} {tradeAmount.ToString("N3")} @ {tradePrice.ToString("N9")}");
        }
      }

      Console.ForegroundColor = ConsoleColor.White;
    }

    private void PrintOrders()
    {
      Console.WriteLine("Order book");
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

      Console.ForegroundColor = ConsoleColor.Red;
      foreach (var item in sells)
      {
        Console.WriteLine(FormatItem(item));
      }
      Console.ForegroundColor = ConsoleColor.White;

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

      Console.ForegroundColor = ConsoleColor.Green;

      if (buys != null)
      {
        foreach (var item in buys)
        {
          Console.WriteLine(FormatItem(item));
        }
      }

      Console.ForegroundColor = ConsoleColor.White;
    }

    private void PrintWallet()
    {
      Console.WriteLine($"Wallet ETH balance: {WalletETH}");
      Console.WriteLine($"EtherDelta ETH balance: {EtherDeltaETH}");
      Console.WriteLine($"Wallet token balance: {WalletToken}");
      Console.WriteLine($"EtherDelta token balance: {EtherDeltaToken}");
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