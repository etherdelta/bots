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
  public class Taker : BaseBot
  {
    public Taker(EtherDeltaConfiguration config, ILogger logger = null) : base(config, logger)
    {
      var order = service.GetBestAvailableSell();

      if (order != null)
      {
        Console.WriteLine($"Best available: Sell {order.EthAvailableVolume.ToString("N3")} @ {order.Price.ToString("N9")}");
        var desiredAmountBase = 0.001;

        var fraction = Math.Min(desiredAmountBase / order.EthAvailableVolumeBase, 1);
        service.TakeOrder(order, fraction).Wait();
      }
      else
      {
        Console.WriteLine("No Available order");
      }


      Console.WriteLine();
    }
  }
}