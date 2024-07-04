using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Ticker details branding structure.
  /// </summary>
  public class TickerDetailsBrandingDto
  {
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("logo_url")]
    public string LogoUrl { get; set; }
  }
}
