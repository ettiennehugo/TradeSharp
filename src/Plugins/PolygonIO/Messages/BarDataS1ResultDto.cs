
namespace TradeSharp.PolygonIO.Messages
{
  /// <summary>
  /// Raal-time bar data results for 1 second intervals.
  /// https://polygon.io/docs/stocks/ws_stocks_a
  /// </summary>
  public class BarDataS1ResultDto
  {
    IList<BarDataS1Dto> Data { get; set; }    
  }
}
