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
    public string Cik { get; set; }

    [JsonPropertyName("composite_figi")]
    public string CompositeFigi { get; set; }

    [JsonPropertyName("currency_name")]
    public string CurrencyName { get; set; }

    [JsonPropertyName("last_updated_utc")]
    public DateTime LastUpdatedUtc { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; }

    [JsonPropertyName("market")]
    public string Market { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("primary_exchange")]
    public string PrimaryExchange { get; set; }

    [JsonPropertyName("share_class_figi")]
    public string ShareClassFigi { get; set; }

    [JsonPropertyName("ticker")]
    public string Ticker { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
  }
}
