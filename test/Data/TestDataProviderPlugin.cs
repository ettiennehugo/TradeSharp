using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;

namespace TradeSharp.Data.Testing
{
  public class TestDataProviderPlugin : IDataProviderPlugin
  {

    //constants


    //enums


    //types


    //attributes
    string m_name;
    List<string> m_tickers;

    //constructors
    public TestDataProviderPlugin()
    {
      m_name = "";
      m_tickers = new List<string>();
      Commands = new List<PluginCommand>();
    }

    //finalizers


    //interface implementations
    public void Create(ILogger logger)
    {
      m_name = "TestDataProvider";
      HasSettings = false;
      Commands = new List<PluginCommand>();
    }

    public void Dispose() { IsConnected = false; }

    public void Connect() { IsConnected = true; }
    public void Disconnect() { IsConnected = false; }
    public void ShowSettings() { }

    public object Request(string ticker, Resolution resolution, DateTime start, DateTime end) { return new List<BarData>(); }   //just return empty list

    //properties
    public string Name => m_name;
    public IList<string> Tickers => m_tickers;
    public int ConnectionCountMax => Environment.ProcessorCount;
    public IPluginConfiguration Configuration { get; set; }
    public IHost ServiceHost { get; set; }
    public bool IsConnected { get; internal set; }
    public bool HasSettings { get; internal set; }
    public IList<PluginCommand> Commands { get; internal set; }

    //delegates
    public virtual event EventHandler? Connected;
    public virtual event EventHandler? Disconnected;
    public virtual event EventHandler? UpdateCommands;

    //methods
    public void raiseConnected() { if (Connected != null) Connected(this, new EventArgs()); }
    public void raiseDisconnected() { if (Disconnected != null) Disconnected(this, new EventArgs()); }
    public void raiseUpdateCommands() { if (UpdateCommands != null) UpdateCommands(this, new EventArgs()); }
  }
}
