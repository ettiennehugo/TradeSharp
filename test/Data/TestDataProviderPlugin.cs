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
    List<string> m_tickers;

    //properties
    public string Name => "TestDataProvider";
    public string Description => "Test Data Provider";
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
    public virtual event ConnectionStatusHandler? ConnectionStatus;
    public virtual event RequestErrorHandler? RequestError;

    //constructors
    public TestDataProviderPlugin()
    {
      m_tickers = new List<string>();
      Commands = new List<PluginCommand>();
    }

    //finalizers


    //interface implementations
    public void Create(ILogger logger)
    {
      HasSettings = false;
      Commands = new List<PluginCommand>();
    }

    public void Connect() { IsConnected = true; }
    public void Disconnect() { IsConnected = false; }
    public void ShowSettings() { }
    public bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end) { return true; }

    //methods
    public void raiseConnected() { if (Connected != null) Connected(this, new EventArgs()); }
    public void raiseDisconnected() { if (Disconnected != null) Disconnected(this, new EventArgs()); }
    public void raiseUpdateCommands() { if (UpdateCommands != null) UpdateCommands(this, new EventArgs()); }
  }
}
