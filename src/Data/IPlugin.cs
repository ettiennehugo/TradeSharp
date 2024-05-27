using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Input;
using System.Runtime.InteropServices;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Custom commands supported by the plugins.
  /// </summary>
  public class PluginCommand 
  {
    public const string Separator = "-----";    //set name to this value to create a separator
    public string Name;
    public string Tooltip;
    public string Icon;                         // Segoe font assets icon code
    public IRelayCommand? Command;
  }

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
    string Name { get; }                                //name of the plugin
    string Description { get; }                         //description of the plugin
    IHost ServiceHost { get; set; }                     //service host for TradeSharp
    IPluginConfiguration Configuration { get; set; }    //configuration profile for the broker plugin
    IList<PluginCommand> Commands { get; }              //custom commands supported by the plugin

    //delegates
    event EventHandler? UpdateCommands;                 //event raised when the plugin needs to update the command list

    //methods
    /// <summary>
    /// Called on plugin creation/destruction.
    /// </summary>
    void Create(ILogger logger);
    void Dispose();
  }
}
