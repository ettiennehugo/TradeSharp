namespace TradeSharp.InteractiveBrokers.Messages
{
  public class ConnectionStatusMessage
    {
        public bool IsConnected { get; }

        public ConnectionStatusMessage(bool isConnected)
        {
            IsConnected = isConnected;
        }

        
    }
}
