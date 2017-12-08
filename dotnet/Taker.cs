using System;

namespace EhterDelta.Bots.Dontnet
{
    public class Taker : BaseBot
    {
        public Taker(EtherDeltaConfiguration config, ILogger logger = null) : base(config, logger)
        {
            var order = Service.GetBestAvailableSell();

            if (order != null)
            {
                Console.WriteLine($"Best available: Sell {order.EthAvailableVolume.ToString("N3")} @ {order.Price.ToString("N9")}");
                var desiredAmountBase = 0.001m;

                var fraction = Math.Min(desiredAmountBase / order.EthAvailableVolumeBase, 1);
                try
                {
                    Service.TakeOrder(order, fraction).Wait();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                    }
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