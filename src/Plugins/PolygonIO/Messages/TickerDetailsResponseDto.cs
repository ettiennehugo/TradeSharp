using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Ticker details response structure.
  /// </summary>
  public class TickerDetailsResponseDto
  {
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("results")]
    public IList<TickerDetailsDto> Results { get; set; }

  }
}
