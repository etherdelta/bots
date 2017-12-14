using System.Numerics;

namespace EhterDelta.Bots.DotNet
{
    public class EtherDeltaConfiguration
    {
        public string AddressEtherDelta { get; set; }
        public string Provider { get; set; }
        public string SocketUrl { get; set; }
        public string AbiFile { get; internal set; }
        public string TokenFile { get; internal set; }
        public string Token { get; internal set; }
        public string User { get; internal set; }
        public string PrivateKey { get; internal set; }
        public int UnitDecimals { get; internal set; }
        public BigInteger GasLimit { get; internal set; }
        public BigInteger GasPrice { get; internal set; }
    }
}