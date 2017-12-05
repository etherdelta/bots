

using System;
using System.Linq;

namespace EhterDelta.Bots.Dontnet
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
        SocketUrl = "wss://socket.etherdelta.com/socket.io/?transport=websocket",
        Provider = "https://mainnet.infura.io/Ky03pelFIxoZdAUsr82w",
        AddressEtherDelta = "0x8d12a197cb00d4747a1fe03395095ce2a5cc6819",
        AbiFile = "../contracts/etherdelta.json",
        TokenFile = "../contracts/token.json",
        Token = "0x21692a811335301907ecd6343743791802ba7cfd",
        User = "0x6e9bcD9f07d3961444555967D5F8ACaaae1559f4",
        UnitDecimals = 18
      };

      ILogger logger = null;
      if (args.Length == 2 && args[1] == "-v")
      {
        logger = new ConsoleLogger();
      }

      if (args[0] != "taker")
      {
        new Taker(config, logger);
      }
      else
      {
        new Maker(config, logger);
      }
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