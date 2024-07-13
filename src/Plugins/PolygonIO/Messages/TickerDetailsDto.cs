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
    public TickerDetailsAddressDto Address { get; set; } = new TickerDetailsAddressDto();

    [JsonPropertyName("branding")]
    public TickerDetailsBrandingDto Branding { get; set; } = new TickerDetailsBrandingDto();

    [JsonPropertyName("cik")]
    public string Cik { get; set; } = string.Empty;

    [JsonPropertyName("composite_figi")]
    public string CompositeFigi { get; set; } = string.Empty;

    [JsonPropertyName("currency_name")]
    public string CurrencyName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("homepage_url")]
    public string HomepageUrl { get; set; } = string.Empty;

    [JsonPropertyName("list_date")]
    public DateTime ListDate { get; set; } = TradeSharp.Common.Constants.DefaultMinimumDateTime;

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; set; } = string.Empty;

    [JsonPropertyName("market_cap")]
    public double MarketCap { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("primary_exchange")]
    public string PrimaryExchange { get; set; } = string.Empty;

    [JsonPropertyName("round_lot")]
    public int RoundLot { get; set; }

    [JsonPropertyName("share_class_figi")]
    public string ShareClassFigi { get; set; } = string.Empty;

    [JsonPropertyName("share_class_shares_outstanding")]
    public long ShareClassSharesOutstanding { get; set; }

    [JsonPropertyName("sic_code")]
    public string SicCode { get; set; } = string.Empty;

    [JsonPropertyName("sic_description")]
    public string SicDescription { get; set; } = string.Empty;

    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonPropertyName("ticker_root")]
    public string TickerRoot { get; set; } = string.Empty;

    [JsonPropertyName("total_employees")]
    public int TotalEmployees { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("weighted_shares_outstanding")]
    public long WeightedSharesOutstanding { get; set; }

  }
}
