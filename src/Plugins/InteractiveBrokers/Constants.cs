namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Common constants used by the Interactive Brokers plugins.
  /// </summary>
  public class Constants
  {
    //sleep time between requests in milliseconds - set limit to be under 50 requests per second https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#requests-limitations
    //NOTE: If you set this too short it looks like the IB API starts failing and some responses are not properly processed.
    public const int IntraRequestSleep = 45;

    //Configuration constants
    public const string DefaultName = "InteractiveBrokers";    //name used for any identifiers related to the plugin name 
    public const string IpKey = "IP";                          //IP address of the IB TWS API connection
    public const string PortKey = "Port";                      //port of the IB TWS API connection
    public const string CacheKey = "Cache";                    //name of the IB local cache database under the config directory
    public const string AutoConnectKey = "AutoConnect";        //try to automatically connect when broker plugin is loaded
    public const string AutoReconnectKey = "AutoReconnect";    //try to automatically reconnect when the IB connection is dropped
    public const string AutoReconnectIntervalKey = "AutoReconnectInterval";  //interval at which reconnection is tried - see TimeSpan.Parse on valid values https://learn.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=net-8.0
    public const string MaintenanceScheduleKey = "MaintenanceSchedule";    //comma separated list of <day> <start-time>:<end-time> <timezone> schedule for when to run maintenance tasks
    
    //Auto-reconnect constants
    public const string MaintenanceScheduleEntryRegex = @"(\w+)\s+(\d{1,2}:\d{2})-(\d{1,2}:\d{2})\s+(\w+)"; //Regex pattern to parse the maintenance schedule entries    
    public const bool DefaultAutoConnect = false;             //default to automatically connect to the IB TWS API when plugin is loaded
    public const bool DefaultAutoReconnect = true;           //default to automatically reconnect to the IB TWS API when the connection is dropped
    public const int DefaultAutoReconnectIntervalMinutes = 5;     //default interval in minutes to wait before attempting to reconnect to the IB TWS API

    //Miscellaneous other constants
    public const string DefaultExchange = "SMART";        //by default we check everything against the SMART exchange that would route to the appropriate exchange - NOTE need to remain upper case for comparisons
    public const string DefaultRootInstrumentGroupName = "Interactive Brokers Classifications";    //default root instrument group used when copying IB industries and categories into TradeSharp 
    public const string DefaultRootInstrumentGroupTag = "IBIndustriesRoot";    //tag used for IB classifications root instrument group
    public const int DisconnectedSleepInterval = 30000;   //interval in milliseconds to wait between checks when the IB connection is disconnected - typically used for long running processes

    //Order status constants
    //https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#order-status-message
    //NOTE: We make these constants upper case to ensure that they are case insensitive when comparing.
    public const string OrderStatusPendingSubmit = "PENDINGSUBMIT";
    public const string OrderStatusPendingCancel = "PENDINGCANCEL";
    public const string OrderStatusPreSubmitted = "PRESUBMITTED";
    public const string OrderStatusSubmitted = "SUBMITTED";
    public const string OrderStatusApiCancelled = "APICANCELLED";
    public const string OrderStatusCancelled = "CANCELLED";
    public const string OrderStatusFilled = "FILLED";
    public const string OrderStatusInactive = "INACTIVE";

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
