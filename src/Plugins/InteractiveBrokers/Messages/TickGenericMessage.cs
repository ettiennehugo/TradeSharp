using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class TickGenericMessage : MarketDataMessage
    {
        public TickGenericMessage(int requestId, int field, double price)
            : base(requestId, field)
        {
            Price = price;
        }

        public double Price { get; set; }
    }
}
