using System.Runtime.InteropServices;

namespace TradeSharp.Common
{
  /// <summary>
  /// Interface to implement to store the configuration profiles for a data providers.
  /// </summary>
  [ComVisible(true)]
  [Guid("0AEF26CA-0B01-44A8-9F5D-AF6F147CB6D5")]
  public interface IPluginConfigurationProfile
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Data provider with which this configuration is associated.
    /// </summary>
    string DataProviderName { get; }

    /// <summary>
    /// Name of the specific configuration.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Description of the specific configuration.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Dictionary of the key/value pairs in the profile for this configuration.
    /// </summary>
    IDictionary<string, object> Configuration { get; }

    //methods


  }
}
