using System;
using TradeSharp.Data;

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

    //ticker market constants
    public const string TickerMarketStocks = "stocks";
    public const string TickerMarketOTC = "otc";
    public const string TickerMarketForex = "fx";
    public const string TickerMarketIndex = "indices";
    public const string TickerMarketCrypto = "crypto";

    //ticker types constants
    public const string TickerTypeCommonStock = "CS";
    public const string TickerTypeETF = "ETF";
    public const string TickerTypeMutualFund = "FUND";
    public const string TickerTypeADRC = "ADRC";    //American Depository Receipts
    public const string TickerTypeOS = "OS";
    public const string TickerTypeUnit = "UNIT";
    public const string TickerTypeWarrant = "WARRANT";
    public const string TickerTypeRight = "RIGHT";
    public const string TickerTypeETS = "ETS";
    public const string TickerTypePFD = "PFD";
    public const string TickerTypeOther = "OTHER";
    public const string TickerTypeGDR = "GDR";
    public const string TickerTypeIndex = "INDEX";
    public const string TickerTypeSP = "SP";
    public const string TickerTypeETV = "ETV";
    public const string TickerTypeETN = "ETN";
    public const string TickerTypeNYRS = "NYRS";

  }
}
