using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Exchange data entry for the exchange response.
  /// </summary>
  public class ExchangeDto
  {
    [JsonPropertyName("acronym")]
    public string Acronym { get; set; } = string.Empty;

    [JsonPropertyName("asset_class")]
    public string AssetClass { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonPropertyName("mic")]
    public string Mic { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("operating_mic")]
    public string OperatingMic { get; set; } = string.Empty;

    [JsonPropertyName("participant_id")]
    public string ParticipantId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
  }
}
