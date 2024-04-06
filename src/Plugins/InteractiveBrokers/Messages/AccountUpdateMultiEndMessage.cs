namespace TradeSharp.InteractiveBrokers.Messages
{
    public class AccountUpdateMultiEndMessage 
    {
        public AccountUpdateMultiEndMessage(int reqId)
        {
            ReqId = ReqId;
        }

        public int ReqId { get; set; }
    }
}
