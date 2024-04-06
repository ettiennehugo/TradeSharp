namespace TradeSharp.InteractiveBrokers.Messages
{
  public class SoftDollarTiersMessage
    {
        public int ReqId { get; private set; }
        public IBApi.SoftDollarTier[] Tiers { get; private set; }

        public SoftDollarTiersMessage(int reqId, IBApi.SoftDollarTier[] tiers)
        {
            ReqId = reqId;
            Tiers = tiers;
        }
    }
}
