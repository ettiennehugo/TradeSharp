using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Exchange data entry for the exchange response.
  /// </summary>
  public class ExchangeDto
  {
    [JsonPropertyName("acronym")]
    public string Acronym { get; set; }

    [JsonPropertyName("asset_class")]
    public string AssetClass { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; }

    [JsonPropertyName("mic")]
    public string Mic { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("operating_mic")]
    public string OperatingMic { get; set; }

    [JsonPropertyName("participant_id")]
    public string ParticipantId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
  }
}
