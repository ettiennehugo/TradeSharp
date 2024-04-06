namespace TradeSharp.InteractiveBrokers.Messages
{
  public class TickReqParamsMessage
  {
    public TickReqParamsMessage(int requestId, double minTick, string bboExchange, int snapshotPermissions)
    {
      RequestId = requestId;
      MinTick = minTick;
      BboExchange = bboExchange;
      SnapshotPermissions = snapshotPermissions;
    }

    public int RequestId { get; set; }
    public double MinTick { get; set; }
    public string BboExchange { get; set; }
    public int SnapshotPermissions { get; set; }
  }
}
