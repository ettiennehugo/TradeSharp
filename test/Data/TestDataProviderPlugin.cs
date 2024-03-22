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
    }

    //finalizers


    //interface implementations
    public void Create(ILogger logger)
    {
      m_name = "TestDataProvider";
    }

    public void Destroy() { IsConnected = false; }

    public void Connect() { IsConnected = true; }
    public void Disconnect() { IsConnected = false; }

    public object Request(string ticker, Resolution resolution, DateTime start, DateTime end) { return new List<BarData>(); }   //just return empty list

    //properties
    public string Name => m_name;
    public IList<string> Tickers => m_tickers;
    public int ConnectionCountMax => Environment.ProcessorCount;
    public IPluginConfiguration Configuration { get; set; }
    public bool IsConnected { get; internal set; }

    //methods


  }
}
