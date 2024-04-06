using IBApi;

namespace TradeSharp.InteractiveBrokers.Messages
{
  public class TickPriceMessage : TickGenericMessage
    {
        public TickPriceMessage(int requestId, int field, double price, TickAttrib attribs)
            : base(requestId, field, price)
        {
            Attribs = attribs;
        }

        public TickAttrib Attribs { get; set; }
    }
}
