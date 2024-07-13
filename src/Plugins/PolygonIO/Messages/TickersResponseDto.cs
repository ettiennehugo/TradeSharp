using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Tickers list response structure.
  /// </summary>
  public class TickersResponseDto
  {
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next_url")]
    public string NextUrl { get; set; } = string.Empty;

    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public IList<TickersDto> Results { get; set; } = new List<TickersDto>();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
  }
}
