using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Ticker details address structure.
  /// </summary>
  public class TickerDetailsAddressDto
  {
    [JsonPropertyName("address1")]
    public string Address1 { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }
  }
}
