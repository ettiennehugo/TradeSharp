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
  public class Plugin : IPlugin
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
      Commands = new List<PluginCommand>();
    }

    //finalizers


    //interface implementations
    public virtual void Create(ILogger logger)
    {
      m_logger = logger;
    }

    public virtual void Destroy() { }

    //properties
    public string Name { get; internal set; }
    public IHost ServiceHost { get; set; }
    public IPluginConfiguration Configuration { get; set; }
    public IList<PluginCommand> Commands { get; protected set; }

    //delegates
    public virtual event EventHandler? UpdateCommands;                 //event raised when the plugin needs to update the command list

    //methods
    protected void raiseUpdateCommands() { if (UpdateCommands != null) UpdateCommands(this, new EventArgs()); }
  }
}
