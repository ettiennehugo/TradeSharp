using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Ticker details entry for the ticker details response.
  /// </summary>
  public class TickerDetailsDto
  {
  [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("address")]
    public TickerDetailsAddressDto Address { get; set; }

    [JsonPropertyName("branding")]
    public TickerDetailsBrandingDto Branding { get; set; }

    [JsonPropertyName("cik")]
    public string Cik { get; set; }

    [JsonPropertyName("composite_figi")]
    public string CompositeFigi { get; set; }

    [JsonPropertyName("currency_name")]
    public string CurrencyName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("homepage_url")]
    public string HomepageUrl { get; set; }

    [JsonPropertyName("list_date")]
    public DateTime ListDate { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; }

    [JsonPropertyName("market")]
    public string Market { get; set; }

    [JsonPropertyName("market_cap")]
    public long MarketCap { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("primary_exchange")]
    public string PrimaryExchange { get; set; }

    [JsonPropertyName("round_lot")]
    public int RoundLot { get; set; }

    [JsonPropertyName("share_class_figi")]
    public string ShareClassFigi { get; set; }

    [JsonPropertyName("share_class_shares_outstanding")]
    public long ShareClassSharesOutstanding { get; set; }

    [JsonPropertyName("sic_code")]
    public string SicCode { get; set; }

    [JsonPropertyName("sic_description")]
    public string SicDescription { get; set; }

    [JsonPropertyName("ticker")]
    public string Ticker { get; set; }

    [JsonPropertyName("ticker_root")]
    public string TickerRoot { get; set; }

    [JsonPropertyName("total_employees")]
    public int TotalEmployees { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("weighted_shares_outstanding")]
    public long WeightedSharesOutstanding { get; set; }

  }
}
