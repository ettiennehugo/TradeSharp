using System.Runtime.InteropServices;

namespace TradeSharp.Common
{
  /// <summary>
  /// Interface to be implemented by data provider configuration classes.
  /// </summary>
  [ComVisible(true)]
  [Guid("AB62E77B-8656-4F66-B760-8C9237FBCE47")]
  public interface IPluginConfiguration
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Name of the plugin.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Assembly to load for the plugin.
    /// </summary>
    string Assembly { get; set; }

    /// <summary>
    /// Dictionary of the key/value pairs in the profile for this configuration.
    /// </summary>
    IDictionary<string, object> Configuration { get; }

    //methods

  }
}
