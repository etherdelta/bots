using Newtonsoft.Json.Linq;
using System;

namespace EhterDelta.Bots.Dontnet
{
    public class Trade
    {
        public Trade()
        {
        }

        string TxHash { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountBase { get; set; }
        public string Side { get; set; }
        public string Buyer { get; set; }
        public string Seller { get; set; }
        public string TokenAddr { get; set; }

        public static Trade FromJson(JToken jtoken)
        {
            var trade = jtoken.ToObject<Trade>();

            if (trade.TxHash == null && jtoken["txHash"] != null)
            {
                trade.TxHash = jtoken["txHash"].ToString();
            }
            return trade;
        }

        public bool Equals(Trade other)
        {
            if (other == null)
            {
                return false;
            }
            return other.TxHash == TxHash;
        }

        public override int GetHashCode()
        {
            return TxHash.GetHashCode();
        }
    }
}