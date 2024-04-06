namespace TradeSharp.InteractiveBrokers.Messages
{
  public class ScannerEndMessage
    {
        public ScannerEndMessage(int requestId)
        {
             RequestId = requestId;
        }

        public int RequestId { get; set; }
    }
}
