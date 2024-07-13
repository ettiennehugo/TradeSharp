using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Result entry for the tickers response.
  /// </summary>
  public class TickersDto
  {
  [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("cik")]
    public string Cik { get; set; } = string.Empty;

    [JsonPropertyName("composite_figi")]
    public string CompositeFigi { get; set; } = string.Empty;

    [JsonPropertyName("currency_name")]
    public string CurrencyName { get; set; } = string.Empty;

    [JsonPropertyName("last_updated_utc")]
    public DateTime LastUpdatedUtc { get; set; } = DateTime.Now;

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("primary_exchange")]
    public string PrimaryExchange { get; set; } = string.Empty;

    [JsonPropertyName("share_class_figi")]
    public string ShareClassFigi { get; set; } = string.Empty;

    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
  }
}
