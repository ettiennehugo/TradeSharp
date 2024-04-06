namespace TradeSharp.InteractiveBrokers.Messages
{
  public class ScannerParametersMessage
    {
        public ScannerParametersMessage(string data)
        {
            XmlData = data;
        }

        public string XmlData { get; set; }
    }
}
