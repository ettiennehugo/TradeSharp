using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base interface to be supported by all plugins.
  /// </summary>
  [ComVisible(true)]
  [Guid("6E4AEA6B-5032-40E2-8BED-DA1D0C85C2EF")]
  public interface IPlugin
  {
    //constants


    //enums


    //types


    //attributes
  

    //properties
    string Name { get; }                                //name of the broker plugin
    IHost ServiceHost { get; set; }                     //service host for TradeSharp
    IPluginConfiguration Configuration { get; set; }    //configuration profile for the broker plugin
    bool IsConnected { get; }                           //is the plugin connected to the remote service (returns true is no connection is used)

    //methods
    /// <summary>
    /// Called on plugin creation/destruction.
    /// </summary>
    void Create(ILogger logger);
    void Destroy();

    /// <summary>
    /// Called to connect/disconnect the plugin if it needs to connect to a remote service.
    /// </summary>
    void Connect();
    void Disconnect();
  }
}
