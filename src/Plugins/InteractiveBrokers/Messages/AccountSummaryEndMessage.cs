namespace TradeSharp.InteractiveBrokers.Messages
{
  public class AccountSummaryEndMessage 
    {
        public AccountSummaryEndMessage(int requestId)
        {
            RequestId = requestId;
        }

        public int RequestId { get; set; }
    }
}
