namespace TradeSharp.InteractiveBrokers.Messages
{
  public class HeadTimestampMessage
    {
        public int ReqId { get; private set; }
        public string HeadTimestamp { get; private set; }

        public HeadTimestampMessage(int reqId, string headTimestamp)
        {
            ReqId = reqId;
            HeadTimestamp = headTimestamp;
        }
    }
}
