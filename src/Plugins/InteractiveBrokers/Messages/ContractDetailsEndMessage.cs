namespace TradeSharp.InteractiveBrokers.Messages
{
  public class ContractDetailsEndMessage
  {
    public ContractDetailsEndMessage(int requestId)
    {
      RequestId = requestId;
    }

    public int RequestId { get; set; }
  }
}
