

using System;
using System.Linq;

namespace EhterDelta.Bots.Dontnet
{
  class Program
  {
    static void Main(string[] args)
    {
      new Taker(
      //new ConsoleLogger()
      );
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