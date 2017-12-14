using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Util;

namespace EhterDelta.Bots.DotNet
{
    public abstract class BaseBot
    {
        protected Service Service { get; set; }

        protected BigInteger EtherDeltaETH { get; set; }
        protected BigInteger WalletETH { get; set; }
        protected BigInteger EtherDeltaToken { get; set; }
        protected BigInteger WalletToken { get; set; }

        protected BaseBot(EtherDeltaConfiguration config, ILogger logger = null)
        {
            Console.Clear();
            Console.ResetColor();
            Service = new Service(config, logger);

            Task[] tasks = {
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

        private async Task<BigInteger> GetEtherDeltaBalance(string token, string user)
        {
            BigInteger balance = 0;
            try
            {
                balance = (BigInteger) await this.Service.GetEtherDeltaBalance(token, user);
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

        private async Task<BigInteger> GetBalanceAsync(string token, string user)
        {
            BigInteger balance = 0;

            try
            {
                balance = (BigInteger) await this.Service.GetBalance(token, user);
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
            const int numTrades = 10;

            if (Service.Trades != null)
            {
                var trades = Service.Trades.Take(numTrades);
                foreach (var trade in trades)
                {
                    Console.ForegroundColor = trade.Side == "sell" ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.WriteLine($"{trade.Date.ToLocalTime()} {trade.Side} {trade.Amount:N3} @ {trade.Price:N9}");
                }
            }

            Console.ResetColor();
        }

        private void PrintOrders()
        {
            Console.WriteLine();
            Console.WriteLine("Order book");
            Console.WriteLine("====================================");
            const int ordersPerSide = 10;

            if (!Service.Orders.Sells.Any() && !Service.Orders.Buys.Any())
            {
                Console.WriteLine("No sell or buy orders");
                return;
            }

            var sells = Service.Orders.Sells.Take(ordersPerSide).Reverse().ToList();
            var buys = Service.Orders.Buys.Take(ordersPerSide).ToList();

            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var order in sells)
            {
                Console.WriteLine(FormatOrder(order));
            }
            Console.ResetColor();

            if (buys.Any() && sells.Any())
            {
                var salesPrice = sells.Last().Price;
                var buysPrice = buys.Last().Price;
                Console.WriteLine($"---- Spread ({(salesPrice - buysPrice):N9}) ----");
            }
            else
            {
                Console.WriteLine("--------");
            }

            Console.ForegroundColor = ConsoleColor.Green;

                foreach (var order in buys)
                {
                    Console.WriteLine(FormatOrder(order));
                }

            Console.ResetColor();
        }

        private void PrintWallet()
        {
            var uc = new UnitConversion();
            Console.WriteLine();
            Console.WriteLine("Account balances");
            Console.WriteLine("====================================");
            Console.WriteLine($"Wallet ETH balance:         {uc.FromWei(this.WalletETH):N18}");
            Console.WriteLine($"EtherDelta ETH balance:     {uc.FromWei(this.EtherDeltaETH):N18}");
            Console.WriteLine($"Wallet token balance:       {uc.FromWei(this.WalletToken):N18}");
            Console.WriteLine($"EtherDelta token balance:   {uc.FromWei(this.EtherDeltaToken):N18}");
        }

        private string FormatOrder(Order order)
        {
            return $"{order.Price:N9} {order.EthAvailableVolume,20:N3}";
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
            this.Service?.Close();
        }
    }
}