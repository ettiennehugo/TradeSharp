using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base implementation of plugins classes.
  /// </summary>
  [ComVisible(true)]
  [Guid("C82F2509-7A77-467E-B6DD-7FD42AEE449D")]
  public class Plugin
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger m_logger;

    //constructors
    public Plugin(string name)
    {
      Name = name;
      HasSettings = false;
      CustomCommands = new List<CustomCommand>();
    }

    //finalizers


    //interface implementations
    public virtual void Create(ILogger logger)
    {
      m_logger = logger;
    }

    public virtual void Destroy() { }
    public virtual void Connect() { }
    public virtual void Disconnect() { }
    public virtual void ShowSettings() { }

    //properties
    public string Name { get; internal set; }
    public IHost ServiceHost { get; set; }
    public IPluginConfiguration Configuration { get; set; }
    public virtual bool IsConnected { get; protected set; }
    public virtual bool HasSettings { get; protected set; }
    public IList<CustomCommand> CustomCommands { get; protected set; }

    //delegates
    public virtual event EventHandler? Connected;                      //event raised when the plugin connects to the remote service
    public virtual event EventHandler? Disconnected;                   //event raised when the plugin disconnects from the remote service
    public virtual event EventHandler? UpdateCommands;                 //event raised when the plugin needs to update the command list

    //methods
    protected void raiseConnected() { if (Connected != null) Connected(this, new EventArgs()); }
    protected void raiseDisconnected() { if (Disconnected != null) Disconnected(this, new EventArgs()); }
    protected void raiseUpdateCommands() { if (UpdateCommands != null) UpdateCommands(this, new EventArgs()); }
  }
}
