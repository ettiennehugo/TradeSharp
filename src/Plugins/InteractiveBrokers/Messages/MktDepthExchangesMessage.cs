namespace TradeSharp.InteractiveBrokers.Messages
{
  public class MktDepthExchangesMessage
    {
        public IBApi.DepthMktDataDescription[] Descriptions { get; private set; }

        public MktDepthExchangesMessage(IBApi.DepthMktDataDescription[] descriptions)
        {
            Descriptions = descriptions;
        }
    }
}
