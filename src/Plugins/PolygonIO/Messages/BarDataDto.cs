using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Bar data entry in the bar data response.
  /// </summary>
  public class BarDataDto
  {
    [JsonPropertyName("o")]
    public double Open { get; set; }

    [JsonPropertyName("c")]
    public double Close { get; set; }

    [JsonPropertyName("h")]
    public double High { get; set; }

    [JsonPropertyName("l")]
    public double Low { get; set; }

    [JsonPropertyName("v")]
    public double Volume { get; set; }

    [JsonPropertyName("vw")]
    public double VWAP { get; set; }

    [JsonPropertyName("t")]
    public long Timestamp { get; set; }

    [JsonPropertyName("n")]
    public int TransactionCount { get; set; }

    [JsonPropertyName("otc")]
    public bool Otc { get; set; }

    public DateTime DateTime
    {
      get { return DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime; }
    }
  }
}
