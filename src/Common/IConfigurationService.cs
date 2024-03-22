using System.Globalization;

namespace TradeSharp.Common
{
  /// <summary>
  /// Configuration management, returns the parameters and types to assemble into the applications. The configuration related to data providers, data stores, broker plugins and
  /// extensions are tuples of the type used and the XML data under the relevant tag to be processed by the relevant implementation.
  /// </summary>
  public interface IConfigurationService
  {
    //constants


    //enums
    /// <summary>
    /// TimeZone to use for the data being retrieved.
    /// </summary>
    public enum TimeZone
    {
      Local,      //return data in local timezone
      Exchange,   //return data in instruments primary exchange timezone
      UTC,        //return data in coordinated universal time
    }

    //types
    /// <summary>
    /// Set of supported general settings.
    /// </summary>
    public struct GeneralConfiguration
    {
      public const string TimeZone = "TimeZone";  //all data is stored in UTC and then back converted into this timezone - see TimeZone enum
      public const string Database = "Database"; //database to use for the application
    }

    //attributes


    //properties
    public CultureInfo CultureInfo { get; }
    public RegionInfo RegionInfo { get; }
    public IDictionary<string, object> General { get; }
    public IDictionary<string, IPluginConfiguration> DataProviders { get; }
    public IDictionary<string, IPluginConfiguration> Brokers { get; }
    public IDictionary<string, IPluginConfiguration> Extensions { get; }
  }
}
