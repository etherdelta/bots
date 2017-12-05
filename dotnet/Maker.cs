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
  public class Maker : BaseBot
  {
    public Maker(EtherDeltaConfiguration config, ILogger logger = null) : base(config, logger)
    {
      var ordersPerSide = 1;
      var expires = service.GetBlockNumber().Result + 10;
      var buyOrdersToPlace = ordersPerSide - service.MyOrders.Buys.Count;
      var sellOrdersToPlace = ordersPerSide - service.MyOrders.Sells.Count;
      var buyVolumeToPlace = EtherDeltaETH;
      var sellVolumeToPlace = EtherDeltaToken;

      if (service.Orders.Buys.Count <= 0 || service.Orders.Sells.Count <= 0)
      {
        throw new Exception("Market is not two-sided, cannot calculate mid-market");
      }

      var bestBuy = decimal.Parse(service.Orders.Buys[0].price);
      var bestSell = service.Orders.Sells[0].Price;

      // Make sure we have a reliable mid market
      if (Math.Abs((bestBuy - bestSell) / (bestBuy + bestSell) / 2.0) > 0.05)
      {
        throw new Exception("Market is too wide, will not place orders");
      }

      var midMarket = (bestBuy + bestSell) / 2.0;

      var orders = new List<Task>();

      for (var i = 0; i < sellOrdersToPlace; i += 1)
      {
        var price = midMarket + ((i + 1) * midMarket * 0.05);
        var amount = service.ToEth(sellVolumeToPlace / sellOrdersToPlace, service.Config.UnitDecimals);
        Console.WriteLine($"Sell { amount.ToString("N3")} @ { price.ToString("N9")}");
        try
        {
          var order = service.CreateOrder(OrderType.Sell, expires, price, amount);
          orders.Add(order);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
        }
      }
      for (var i = 0; i < buyOrdersToPlace; i += 1)
      {
        var price = midMarket - ((i + 1) * midMarket * 0.05);
        var amount = service.ToEth(buyVolumeToPlace / price / buyOrdersToPlace, service.Config.UnitDecimals);
        Console.WriteLine($"Buy { amount.ToString("N3")} @ { price.toFixed(9)}");
        try
        {
          var order = service.CreateOrder(OrderType.Buy, expires, price, amount);
          orders.Add(order);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex);
        }
      }

      Task.WhenAll(orders).Wait();

      Console.WriteLine("Done");
    }
  }
}