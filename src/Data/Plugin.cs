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
    protected string m_name;
    protected IPluginConfiguration? m_configuration;
    protected IHost m_serviceHost;
    protected ILogger m_logger;

    //constructors
    public Plugin(string name, IHost serviceHost)
    {
      m_name = name;
      m_configuration = null;
      m_serviceHost = serviceHost;
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

    //properties
    public string Name => m_name;
    public IPluginConfiguration Configuration { get => m_configuration!; set => m_configuration = value; }
    public bool IsConnected { get; protected set; }

    //methods



  }
}
