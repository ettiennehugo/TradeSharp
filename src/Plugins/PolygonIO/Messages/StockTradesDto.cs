using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Real-time/delayed streaming of stock trade entry.
  /// https://polygon.io/docs/stocks/ws_stocks_t
  /// </summary>
  public class StockTradesDto
  {
    [JsonPropertyName("ev")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("sym")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int ExchangeId { get; set; }   ///https://polygon.io/docs/stocks/get_v3_reference_exchanges

    [JsonPropertyName("i")]
    public string TradeId { get; set; } = string.Empty;

    [JsonPropertyName("z")]
    public int Tape { get; set; } //see declared Contants class

    [JsonPropertyName("p")]
    public decimal Price { get; set; }

    [JsonPropertyName("s")]
    public int Size { get; set; }

    [JsonPropertyName("c")]
    public int[] Conditions { get; set; }   //https://polygon.io/glossary/us/stocks/conditions-indicators

    [JsonPropertyName("t")]
    public long Timestamp { get; set; }

    [JsonPropertyName("q")]
    public int SequenceNumber { get; set; }

    [JsonPropertyName("trfi")]
    public int TradeReportingFacilityId { get; set; }

    [JsonPropertyName("trft")]
    public long TradeReportingFacilityTimestamp { get; set; }
  }
}
