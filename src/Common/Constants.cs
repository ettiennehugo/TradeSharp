namespace TradeSharp.Common
{
  /// <summary>
  /// Global constants used by TradeSharp components.
  /// </summary>
  public static class Constants
  {
    /// <summary>
    /// Environment variable used for the TradeSharp home directory.
    /// </summary>
    public const string TradeSharpHome = "TradeSharpHome";
    public const string ConfigurationBin = "bin";
    public const string ConfigurationData = "data";
    public const string ConfigurationDir = "config";
    public const string ConfigurationFile = "tradesharp.json";
    public const string DataDir = "data";

    //Default minimum and maximum date/time values used for time sensitive data.
    public static DateTime DefaultMinimumDateTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);    //minimum date/time must be UTC as usd by the database
    public static DateTime DefaultMaximumDateTime = new DateTime(2100, 12, 31, 23, 59, 0, DateTimeKind.Utc);  //maximum date/time must be UTC as usd by the database

    //Default format masks if none specified
    public const string DefaultPriceFormatMask = "0.00";
    public const string DefaultVolumeFormatMask = "N4";   //format using comma's for thousands and 4 decimal places

  }
}
