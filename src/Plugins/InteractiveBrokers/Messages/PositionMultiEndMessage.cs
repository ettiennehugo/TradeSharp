namespace TradeSharp.InteractiveBrokers.Messages
{
  class PositionMultiEndMessage 
    {
        public PositionMultiEndMessage(int reqId)
        {
            ReqId = reqId;
        }

        public int ReqId { get; set; }
    }
}
