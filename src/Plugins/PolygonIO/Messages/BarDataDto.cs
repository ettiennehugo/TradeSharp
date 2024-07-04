using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Bar data entry in the bar data response.
  /// </summary>
  public class BarDataDto
  {
    [JsonPropertyName("o")]
    public decimal OpenPrice { get; set; }

    [JsonPropertyName("c")]
    public decimal ClosePrice { get; set; }

    [JsonPropertyName("h")]
    public decimal HighPrice { get; set; }

    [JsonPropertyName("l")]
    public decimal LowPrice { get; set; }

    [JsonPropertyName("v")]
    public decimal Volume { get; set; }

    [JsonPropertyName("vw")]
    public decimal VWAP { get; set; }

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
