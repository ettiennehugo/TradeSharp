namespace TradeSharp.InteractiveBrokers.Messages
{
  public class HistoricalNewsEndMessage
    {
        public int RequestId { get; private set; }
        public bool HasMore { get; private set; }

        public HistoricalNewsEndMessage(int requestId, bool hasMore)
        {
            RequestId = requestId;
            HasMore = hasMore;
        }
    }
}
