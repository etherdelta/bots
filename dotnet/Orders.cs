using Nethereum.Hex.HexTypes;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json.Linq;
using System;

namespace EhterDelta.Bots.Dontnet
{
    public class Orders : Object
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

        public string Raw { get; internal set; }

        internal static Order FromJson(JToken jtoken)
        {
            var order = jtoken.ToObject<Order>();
            try
            {
                order.V = jtoken.Value<int>("v");
                order.R = jtoken.Value<string>("r");
                order.S = jtoken.Value<string>("s");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            order.Raw = jtoken.ToString();

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

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}