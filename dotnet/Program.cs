

using System;
using System.Configuration;
using System.Diagnostics;
using System.Numerics;

namespace EhterDelta.Bots.DotNet
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 1 || args[0] != "taker" && args[0] != "maker")
            {
                Console.WriteLine("Please run with 'taker' or 'maker' argument!");
                return;
            }

            var config = new EtherDeltaConfiguration
            {
                SocketUrl = ConfigurationManager.AppSettings["SocketUrl"],
                Provider = ConfigurationManager.AppSettings["Provider"],
                AddressEtherDelta = ConfigurationManager.AppSettings["AddressEtherDelta"],
                AbiFile = ConfigurationManager.AppSettings["AbiFile"],
                TokenFile = ConfigurationManager.AppSettings["TokenFile"],
                Token = ConfigurationManager.AppSettings["Token"],
                User = ConfigurationManager.AppSettings["User"],
                PrivateKey = ConfigurationManager.AppSettings["PrivateKey"],
                UnitDecimals = int.Parse(ConfigurationManager.AppSettings["UnitDecimals"]),
                GasPrice = BigInteger.Parse(ConfigurationManager.AppSettings["GasPrice"]),
                GasLimit = BigInteger.Parse(ConfigurationManager.AppSettings["GasLimit"])
            };

            ILogger logger = null;
            if (args.Length == 2 && args[1] == "-v")
            {
                logger = new ConsoleLogger();
            }

            if (args[0] == "taker")
            {
                new Taker(config, logger);
            }
            else
            {
                new Maker(config, logger);
            }

            if (!Debugger.IsAttached) return;

            Console.WriteLine("Press enter to exit");
            while (Console.ReadKey().Key != ConsoleKey.Enter);
        }

        private class ConsoleLogger : ILogger
        {
            public void Log(string message)
            {
                Console.WriteLine($"{DateTimeOffset.Now.DateTime.ToUniversalTime()} :  {message}");
            }
        }
    }
}