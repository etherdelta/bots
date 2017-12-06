using Nethereum.Hex.HexTypes;
using System.Collections.Generic;
using System.Numerics;

namespace EhterDelta.Bots.Dontnet
{
    public class Orders
    {
        public List<Order> Sells { get; set; }
        public List<dynamic> Buys { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
        public string Amount { get; set; }
        public double Price { get; set; }
        public string TokenGet { get; set; }
        public HexBigInteger AmountGet { get; set; }
        public string TokenGive { get; set; }
        public HexBigInteger AmountGive { get; set; }
        public BigInteger Expires { get; set; }
        public BigInteger Nonce { get; set; }
        public string User { get; set; }
        public string Updated { get; set; }
        public string AvailableVolume { get; set; }
        public decimal EthAvailableVolume { get; set; }
        public string AvailableVolumeBase { get; set; }
        public double EthAvailableVolumeBase { get; set; }
        public string AmountFilled { get; set; }
        public int V { get; internal set; }
        public string R { get; internal set; }
        public string S { get; internal set; }
    }
}