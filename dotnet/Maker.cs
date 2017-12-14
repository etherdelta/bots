using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.Util;

namespace EhterDelta.Bots.DotNet
{
    public class Maker : BaseBot
    {
        public Maker(EtherDeltaConfiguration config, ILogger logger = null) : base(config, logger)
        {

            PrintMyOrders();

            const int ordersPerSide = 1;
            var expires = Service.GetBlockNumber().Result + 10;
            var buyOrdersToPlace = ordersPerSide - Service.MyOrders.Buys.Count();
            var sellOrdersToPlace = ordersPerSide - Service.MyOrders.Sells.Count();
            var buyVolumeToPlace = EtherDeltaETH;
            var sellVolumeToPlace = EtherDeltaToken;
            var bestBuy = Service.GetBestAvailableBuy();
            var bestSell = Service.GetBestAvailableSell();

            if (bestBuy == null || bestSell == null)
            {
                Console.WriteLine("Market is not two-sided, cannot calculate mid-market");
                return;
            }

            // Make sure we have a reliable mid market
            if (Math.Abs((bestBuy.Price - bestSell.Price) / (bestBuy.Price + bestSell.Price) / 2) > 0.05m)
            {
                Console.WriteLine("Market is too wide, will not place orders");
                return;
            }

            var uc = new UnitConversion();

            var midMarket = (bestBuy.Price + bestSell.Price) / 2;
            var orders = new List<Order>();

            for (var i = 0; i < sellOrdersToPlace; i += 1)
            {
                var price = midMarket + ((i + 1) * midMarket * 0.05m);
                var amount = sellVolumeToPlace / sellOrdersToPlace;
                Console.WriteLine($"Sell { amount.ToString("N3")} @ { price:N9}");
                try
                {
                    var order = Service.CreateOrder(OrderType.Sell, expires, uc.ToWei(price), amount);
                    orders.Add(order);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }

            for (var i = 0; i < buyOrdersToPlace; i += 1)
            {
                var price = midMarket - ((i + 1) * midMarket * 0.05m);
                var amount = uc.FromWei(buyVolumeToPlace) / price / buyOrdersToPlace;
                Console.WriteLine($"Buy { amount:N3} @ { price:N9}");
                try
                {
                    var order = Service.CreateOrder(OrderType.Buy, expires, uc.ToWei(price), uc.ToWei(amount));
                    orders.Add(order);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            var orderTasks = new List<Task>();
            orders.ForEach(order =>
            {
                var amount = order.TokenGive == Service.ZeroToken ? order.AmountGet : order.AmountGive;
                orderTasks.Add(Service.TakeOrder(order, amount));
            });

            try
            {
                Task.WaitAll(orderTasks.ToArray());
            }
            catch (Exception ex)
            {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Done");
        }

        void PrintMyOrders()
        {
            Console.WriteLine($"My existing buy orders: {Service.MyOrders.Buys.Count()}");
            Console.WriteLine($"My existing sell orders: {Service.MyOrders.Sells.Count()}");
        }
    }
}