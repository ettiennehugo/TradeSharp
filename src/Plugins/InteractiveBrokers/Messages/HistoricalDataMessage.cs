namespace TradeSharp.InteractiveBrokers.Messages
{
  public class HistoricalDataMessage
  {
    public HistoricalDataMessage(int reqId, IBApi.Bar bar)
    {
      RequestId = reqId;
      Date = bar.Time;
      Open = bar.Open;
      High = bar.High;
      Low = bar.Low;
      Close = bar.Close;
      Volume = bar.Volume;
      Count = bar.Count;
      Wap = bar.WAP;
    }

    public int RequestId { get; set; }
    public string Date { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public long Volume { get; set; }
    public int Count { get; set; }
    public double Wap { get; set; }
    public bool HasGaps { get; set; }
  }
}
