using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base implementation of plugins classes.
  /// </summary>
  [ComVisible(true)]
  [Guid("C82F2509-7A77-467E-B6DD-7FD42AEE449D")]
  public partial class Plugin : IPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger m_logger;

    //constructors
    public Plugin(string name, string description) : base()
    {
      Name = name;
      Description = description;
      Commands = new List<PluginCommand>();
      IsConnected = true;   //per default assume we don't need any connection - will just work
    }

    //finalizers


    //interface implementations
    public virtual void Create(ILogger logger)
    {
      m_logger = logger;
    }

    //properties
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public IHost ServiceHost { get; set; }
    public IPluginConfiguration Configuration { get; set; }
    public IList<PluginCommand> Commands { get; protected set; }
    public virtual bool IsConnected { get; protected set; }

    //delegates
    public virtual event ConnectionStatusHandler? ConnectionStatus;    //event raised when the plugin connection status changes
    public virtual event EventHandler? UpdateCommands;                 //event raised when the plugin needs to update the command list

    //methods
    //NOTE: Raise this event only once the connection is completed AND state is setup to reflect correctly in the UI.
    public void raiseConnectionStatus() { ConnectionStatus?.Invoke(this, new ConnectionStatusArgs(IsConnected)); }
    public void raiseUpdateCommands() { UpdateCommands?.Invoke(this, new EventArgs()); }
  }
}
