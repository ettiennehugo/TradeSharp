namespace TradeSharp.InteractiveBrokers.Messages
{
  public class UpdateAccountTimeMessage
    {
        public UpdateAccountTimeMessage(string timestamp)
        {
            Timestamp = timestamp;
        }

        public string Timestamp { get; set; }
    }
}
