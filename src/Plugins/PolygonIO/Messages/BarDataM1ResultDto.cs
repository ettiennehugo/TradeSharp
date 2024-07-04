using System.Text.Json.Serialization;

namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Raal-time bar data results for 1 minute intervals.
  /// https://polygon.io/docs/stocks/ws_stocks_am
  /// </summary>
  public class BarDataM1ResultDto
  {
    IList<BarDataM1Dto> Data { get; set; }
  }
}
