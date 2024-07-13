using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Real-time/delayed streaming of stock quote entry.
  /// https://polygon.io/docs/stocks/ws_stocks_q
  /// </summary>
  public class StockQuotesDto
  {
    [JsonPropertyName("ev")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("sym")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("bx")]
    public int BidExchangeId { get; set; }

    [JsonPropertyName("bp")]
    public decimal BidPrice { get; set; }

    [JsonPropertyName("bs")]
    public int BidSize { get; set; }

    [JsonPropertyName("ax")]
    public int AskExchangeId { get; set; }

    [JsonPropertyName("ap")]
    public decimal AskPrice { get; set; }

    [JsonPropertyName("as")]
    public int AskSize { get; set; }

    [JsonPropertyName("c")]
    public int Condition { get; set; }

    [JsonPropertyName("i")]
    public int[] Indicators { get; set; }   //https://polygon.io/glossary/us/stocks/conditions-indicators

    [JsonPropertyName("t")]
    public long Timestamp { get; set; }

    [JsonPropertyName("q")]
    public int SequenceNumber { get; set; }

    [JsonPropertyName("z")]
    public int Tape { get; set; }   //see declared Contants class
  }
}
