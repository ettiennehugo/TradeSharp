namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Common constants used by the Interactive Brokers plugins.
  /// </summary>
  public class Constants
  {
    //Configuration constants
    public const string DefaultName = "InteractiveBrokers";    //name used for any identifiers related to the plugin name 
    public const string IpKey = "IP";
    public const string PortKey = "Port";
    public const string CacheKey = "Cache";

    //Miscellaneous other constants
    public const string DefaultExchange = "SMART";    //by default we check everything against the SMART exchange that would route to the appropriate exchange
    public const string DefaultRootInstrumentGroupName = "Interactive Brokers Classifications";    //default root instrument group used when copying IB industries and categories into TradeSharp 
    public const string DefaultRootInstrumentGroupTag = "IBIndustriesRoot";    //tag used for IB classifications root instrument group

    /// <summary>
    /// Supported contract types.
    /// </summary>
    public const string ContractTypeStock = "STK";
    public const string ContractTypeETF = "ETF";
    public const string ContractTypeCFD = "CFD";
    public const string ContractTypeBond = "BOND";
    public const string ContractTypeFuture = "FUT";
    public const string ContractTypeFutureOption = "FOP";
    public const string ContractTypeOption = "OPT";
    public const string ContractTypeIndex = "IND";
    public const string ContractTypeForex = "CASH";
    public const string ContractTypeMutualFund = "FUND";
    public const string ContractTypeFutureCombo = "BAG";
    public const string ContractTypeStockCombo = "CS";
    public const string ContractTypeIndexCombo = "IND";
    public const string ContractTypeCommodity = "CMDTY";

    /// <summary>
    /// Supported security types for stocks.
    /// </summary>
    public const string StockTypeETF = "ETF";
    public const string StockTypeREIT = "REIT";
    public const string StockTypeCommon = "COMMON";
    public const string StockTypeETN = "ETN";
    public const string StockTypeADR = "ADR";
    public const string StockTypeLtdPart = "LTD PART";
    public const string StockTypeClosedEndedFund = "CLOSED-END FUND";
    public const string StockTypeUnit = "UNIT";
    public const string StockTypeMLP = "MLP";
    public const string StockTypePreferred = "PREFERRED";
    public const string StockTypeNYRegularShares = "NY REG SHRS";
    public const string StockTypeRight = "RIGHT";
    public const string StockTypeConversionPreferred = "CONVPREFERRED";
    public const string StockTypeRoyaltyTRust = "ROYALTY TRST";
    public const string StockTypeUSDomenstic = "US DOMESTIC";

    /// <summary>
    /// Historical durations and bar size types.
    /// </summary>
    public const string DurationSeconds = "S";
    public const string DurationDays = "D";
    public const string DurationWeeks = "W";
    public const string DurationMonths = "M";
    public const string DurationYears = "Y";

    /// <summary>
    /// Supported bar sizes.
    /// </summary>
    public const string BarSize1Sec = "1 secs";      //only available for 6-months back
    public const string BarSize5Sec = "5 secs";      //only available for 6-months back
    public const string BarSize10Sec = "10 secs";    //only available for 6-months back
    public const string BarSize15Sec = "15 secs";    //only available for 6-months back
    public const string BarSize30Sec = "30 secs";    //only available for 6-months back
    public const string BarSize1Min = "1 min";
    public const string BarSize2Min = "2 mins";
    public const string BarSize3Min = "3 mins";
    public const string BarSize5Min = "5 mins";
    public const string BarSize10Min = "10 mins";
    public const string BarSize15Min = "15 mins";
    public const string BarSize20Min = "20 mins";
    public const string BarSize30Min = "30 mins";
    public const string BarSize1Hour = "1 hour";
    public const string BarSize2Hour = "2 hours";
    public const string BarSize3Hour = "3 hours";
    public const string BarSize4Hour = "4 hours";
    public const string BarSize8Hour = "8 hours";
    public const string BarSize1Day = "1 day";
    public const string BarSize1Week = "1W";
    public const string BarSize1Month = "1M";


    /// <summary>
    /// Set of response message DateTime formats to parse.
    /// </summary>
    public static string[] DateTimeFormats = { "yyyyMMdd HH:mm:ss", "yyyyMMdd HHmmss", "yyyyMMdd" };

  }
}
