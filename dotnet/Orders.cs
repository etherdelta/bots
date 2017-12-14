using System.Collections.Generic;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using Newtonsoft.Json.Linq;

namespace EhterDelta.Bots.DotNet
{
    public class Orders
    {
        public IEnumerable<Order> Sells { get; set; }
        public IEnumerable<Order> Buys { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
        public string Amount { get; set; }
        public decimal Price { get; set; }
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
        public decimal EthAvailableVolumeBase { get; set; }
        public string AmountFilled { get; set; }
        public int V { get; internal set; }
        public string R { get; internal set; }
        public string S { get; internal set; }
        public string ContractAddr { get; internal set; }
        public string Raw { get; internal set; }

        internal static Order FromJson(JToken jtoken)
        {
            Order order = null;
            try
            {
                order = jtoken.ToObject<Order>();
                order.V = jtoken.Value<int>("v");
                order.R = jtoken.Value<string>("r");
                order.S = jtoken.Value<string>("s");
                order.Raw = jtoken.ToString();
            }
            catch { }

            return order;
        }

        public bool Equals(Order other)
        {
            if (other == null)
            {
                return false;
            }
            return other.Id == Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}