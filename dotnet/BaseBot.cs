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
  public abstract class BaseBot
  {
    protected Service service { get; set; }

    protected decimal EtherDeltaETH { get; set; }
    protected decimal WalletETH { get; set; }
    protected decimal EtherDeltaToken { get; set; }
    protected decimal WalletToken { get; set; }

    public BaseBot(EtherDeltaConfiguration config, ILogger logger = null)
    {
      Console.Clear();
      Console.ResetColor();
      service = new Service(config, logger);

      Task[] tasks = new[] {
        GetMarket(),
        GetBalanceAsync("ETH", config.User),
        GetBalanceAsync(config.Token, config.User),
        GetEtherDeltaBalance("ETH", config.User),
        GetEtherDeltaBalance(config.Token, config.User)
      };

      Task.WaitAll(tasks);

      PrintOrders();
      PrintTrades();
      PrintWallet();

      Console.WriteLine();
    }

    private async Task<decimal> GetEtherDeltaBalance(string token, string user)
    {
      decimal balance = 0;
      try
      {
        balance = await service.GetEtherDeltaBalance(token, user);
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

    private async Task<decimal> GetBalanceAsync(string token, string user)
    {
      decimal balance = 0;

      try
      {
        balance = await service.GetBalance(token, user);
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
      Console.WriteLine();
      Console.WriteLine("Recent trades");
      Console.WriteLine("====================================");
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

      Console.ResetColor();
    }

    private void PrintOrders()
    {
      Console.WriteLine();
      Console.WriteLine("Order book");
      Console.WriteLine("====================================");
      int ordersPerSide = 10;

      List<Order> sells = service.Orders != null ? service.Orders.Sells : null;
      List<dynamic> buys = service.Orders != null ? service.Orders.Buys : null;

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
        Console.WriteLine(FormatOrder(item));
      }
      Console.ResetColor();

      if (buys.Count > 0 && sells.Count > 0)
      {
        var salesPrice = (double)(sells.Last().Price);
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

      Console.ResetColor();
    }

    private void PrintWallet()
    {
      Console.WriteLine();
      Console.WriteLine("Account balances");
      Console.WriteLine("====================================");
      Console.WriteLine($"Wallet ETH balance:         {WalletETH}");
      Console.WriteLine($"EtherDelta ETH balance:     {EtherDeltaETH}");
      Console.WriteLine($"Wallet token balance:       {WalletToken}");
      Console.WriteLine($"EtherDelta token balance:   {EtherDeltaToken}");
    }
    private string FormatItem(dynamic item)
    {
      var price = (double)item.price;
      item.ethAvailableVolume = (double)item.ethAvailableVolume;
      return $"{price.ToString("N9")} {item.ethAvailableVolume.ToString("N3"),20}";
    }

    private string FormatOrder(Order item)
    {
      return $"{item.Price.ToString("N9")} {item.EthAvailableVolume.ToString("N3"),20}";
    }

    private async Task GetMarket()
    {
      try
      {
        await service.WaitForMarket();
      }
      catch (TimeoutException)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Could not get Market!");
        Console.ResetColor();
      }
    }

    ~BaseBot()
    {
      if (service != null)
      {
        service.Close();
      }
    }
  }
}