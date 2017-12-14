using System;
using Nethereum.Util;

namespace EhterDelta.Bots.DotNet
{
    public class Taker : BaseBot
    {
        public Taker(EtherDeltaConfiguration config, ILogger logger = null) : base(config, logger)
        {
            var order = Service.GetBestAvailableSell();

            if (order != null)
            {
                Console.WriteLine($"Best available: Sell {order.EthAvailableVolume:N3} @ {order.Price:N9}");
                const decimal desiredAmountBase = 0.001m;

                var fraction = Math.Min(desiredAmountBase / order.EthAvailableVolumeBase, 1);
                try
                {
                    var uc = new UnitConversion();
                    var amount = order.AmountGet.Value * uc.ToWei(fraction);
                    Service.TakeOrder(order, amount).Wait();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("No Available order");
            }

            Console.WriteLine();
        }
    }
}