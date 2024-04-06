namespace TradeSharp.InteractiveBrokers.Messages
{
  public class TickSizeMessage : MarketDataMessage
    {
        public TickSizeMessage(int requestId, int field, decimal size) : base(requestId, field)
        {
            Size = size;
        }

        public decimal Size { get; set; }
    }
}
