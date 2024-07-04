using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Exchange response structure.
  /// </summary>
  public class ExchangeResponseDto
  {
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public IList<ExchangeDto> Results { get; set; }
  }
}
