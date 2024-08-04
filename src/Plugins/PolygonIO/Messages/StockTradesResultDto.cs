using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Real-time/delayed streaming of stock trade results.
  /// https://polygon.io/docs/stocks/ws_stocks_t
  /// </summary>
  public class StockTradesResultDto
  {
    public IList<StockTradesDto> Trades { get; set; }
  }
}
