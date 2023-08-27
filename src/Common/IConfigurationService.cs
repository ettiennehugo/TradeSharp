using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
      public const string CultureFallback = "CultureFallback";  //list of culture fallbacks to use if the primary culture is not supported
      public const string TimeZone = "TimeZone";  //all data is stored in UTC and then back converted into this timezone - see TimeZone enum
      public const string DataStore = "DataStore";  //datastore to use for the application
    }

    /// <summary>
    /// Structured type for the data store settings.
    /// </summary>
    public class DataStoreConfiguration
    {
      public DataStoreConfiguration()
      {
        Typename = string.Empty;
        ConnectionString = string.Empty;
      }

      public DataStoreConfiguration(string typename, string connectionString)
      {
        Typename = typename;
        ConnectionString = connectionString;
      }

      public string Typename { get; set; }  //assembly and type to use for the datastore
      public string ConnectionString { get; set; }  //connection string to use for the datastore
    }

    //attributes


    //properties
    public CultureInfo CultureInfo { get; }
    public RegionInfo RegionInfo { get; }
    public IList<CultureInfo> CultureFallback { get; }
    public IDictionary<string, object> General { get; }
    public IDictionary<string, string> DataProviders { get; }
    public IDictionary<string, string> Brokers { get; }
    public IDictionary<string, string> Extensions { get; }
  }
}
