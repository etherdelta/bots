using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EhterDelta.Bots.Dontnet
{
    public abstract class BaseBot
    {
        protected Service Service { get; set; }

        protected decimal EtherDeltaETH { get; set; }
        protected decimal WalletETH { get; set; }
        protected decimal EtherDeltaToken { get; set; }
        protected decimal WalletToken { get; set; }

        public BaseBot(EtherDeltaConfiguration config, ILogger logger = null)
        {
            Console.Clear();
            Console.ResetColor();
            Service = new Service(config, logger);

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
                balance = await Service.GetEtherDeltaBalance(token, user);
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
                balance = await Service.GetBalance(token, user);
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

            if (Service.Trades != null)
            {
                var trades = Service.Trades.Take(numTrades);
                foreach (var trade in trades)
                {
                    Console.ForegroundColor = trade.Side == "sell" ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.WriteLine($"{trade.Date.ToLocalTime()} {trade.Side} {trade.Amount.ToString("N3")} @ {trade.Price.ToString("N9")}");
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

            if (Service.Orders.Sells.Count() == 0 && Service.Orders.Buys.Count() == 0)
            {
                Console.WriteLine("No sell or buy orders");
                return;
            }

            var sells = Service.Orders.Sells.Take(ordersPerSide).Reverse();
            var buys = Service.Orders.Buys.Take(ordersPerSide);

            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var order in sells)
            {
                Console.WriteLine(FormatOrder(order));
            }
            Console.ResetColor();

            if (buys.Count() > 0 && sells.Count() > 0)
            {
                var salesPrice = sells.Last().Price;
                var buysPrice = buys.Last().Price;
                Console.WriteLine($"---- Spread ({(salesPrice - buysPrice).ToString("N9")}) ----");
            }
            else
            {
                Console.WriteLine("--------");
            }

            Console.ForegroundColor = ConsoleColor.Green;

            if (buys != null)
            {
                foreach (var order in buys)
                {
                    Console.WriteLine(FormatOrder(order));
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

        private string FormatOrder(Order order)
        {
            return $"{order.Price.ToString("N9")} {order.EthAvailableVolume.ToString("N3"),20}";
        }

        private async Task GetMarket()
        {
            try
            {
                await Service.WaitForMarket();
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
            if (Service != null)
            {
                Service.Close();
            }
        }
    }
}