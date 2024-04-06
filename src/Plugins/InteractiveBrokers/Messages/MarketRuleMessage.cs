using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class MarketRuleMessage
    {
        public int MarketruleId { get; private set; }
        public PriceIncrement[] PriceIncrements { get; private set; }

        public MarketRuleMessage(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            MarketruleId = marketRuleId;
            PriceIncrements = priceIncrements;
        }
    }
}
