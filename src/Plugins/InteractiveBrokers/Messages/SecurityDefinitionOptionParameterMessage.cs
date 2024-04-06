using System.Collections.Generic;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class SecurityDefinitionOptionParameterMessage
    {
        public int ReqId { get; private set; }
        public string Exchange { get; private set; }
        public int UnderlyingConId { get; private set; }
        public string TradingClass { get; private set; }
        public string Multiplier { get; private set; }
        public HashSet<string> Expirations { get; private set; }
        public HashSet<double> Strikes { get; private set; }

        public SecurityDefinitionOptionParameterMessage(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            ReqId = reqId;
            Exchange = exchange;
            UnderlyingConId = underlyingConId;
            TradingClass = tradingClass;
            Multiplier = multiplier;
            Expirations = expirations;
            Strikes = strikes;
        }
    }
}
