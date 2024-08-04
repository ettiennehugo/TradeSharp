using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Real-time/delayed streaming of stock quote results. since last response was processed.
  /// https://polygon.io/docs/stocks/ws_stocks_q
  /// </summary>
  public class StockQuotesResultDto
  {
    public IList<StockQuotesDto> Quotes { get; set; }    
  }
}
