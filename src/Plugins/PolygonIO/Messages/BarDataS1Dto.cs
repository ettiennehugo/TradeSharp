using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Second bar data real-time update entry.
  /// https://polygon.io/docs/stocks/ws_stocks_a
  /// </summary>
  public class BarDataS1Dto
  {
  [JsonPropertyName("ev")]
    public string EventType { get; set; }

    [JsonPropertyName("sym")]
    public string Symbol { get; set; }

    [JsonPropertyName("o")]
    public decimal Open { get; set; }

    [JsonPropertyName("c")]
    public decimal Close { get; set; }

    [JsonPropertyName("h")]
    public decimal High { get; set; }

    [JsonPropertyName("l")]
    public decimal Low { get; set; }

    [JsonPropertyName("v")]
    public long TickVolume { get; set; }

    [JsonPropertyName("av")]
    public long DayAccumulatedVolume { get; set; }

    [JsonPropertyName("z")]
    public long AverageTradeVolume { get; set; }

    [JsonPropertyName("a")]
    public decimal DayVWAP { get; set; }

    [JsonPropertyName("vw")]
    public decimal VWAP { get; set; }

    [JsonPropertyName("s")]
    public long StartTimestamp { get; set; }

    [JsonPropertyName("e")]
    public long EndTimestamp { get; set; }

    [JsonPropertyName("otc")]
    public bool IsOTC { get; set; }

    public DateTime StartDateTime
    {
      get { return DateTimeOffset.FromUnixTimeMilliseconds(StartTimestamp).UtcDateTime; }
    }

    public DateTime EndDateTime
    {
      get { return DateTimeOffset.FromUnixTimeMilliseconds(EndTimestamp).UtcDateTime; }
    }
  }
}
