namespace TradeSharp.PolygonIO
{
  /// <summary>
  /// Polygon.io API specific constants.
  /// </summary>
  public class Constants
  {
    //general constants
    public const string Name = "PolygonIO";
    public const string Description = "Polygon.io data provider plugin";

    //configuration constants
    public const string CacheKey = "Cache";
    public const string ConfigApiKey = "ApiKey";
    public const string ConfigRequestLimit = "RequestLimit";

    //stock trade/quote tape constants
    public const int TapeNYSE = 1;
    public const int TapeAMEX = 2;
    public const int TapeNasdaq = 3;



  }
}
