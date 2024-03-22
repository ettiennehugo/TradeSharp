using Microsoft.Extensions.Logging;
using Tradesharp.InteractiveBrokers;
using TradeSharp.Data;


namespace InteractiveBrokers
{
  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin
  {
    //constants
    public string IpKey = "IP";
    public string PortKey = "Port";
    public string CacheKey = "Cache";

    //enums


    //types


    //attributes
    protected IBApiAdapter m_clientResponseHandler;
    protected string m_ip;
    protected int m_port;

    //constructors
    public DataProviderPlugin() : base("InteractiveBrokers") { }

    //finalizers


    //interface implementations
    public override void Connect()
    {
      base.Connect();
      m_clientResponseHandler.Connect(m_ip, m_port);
      m_clientResponseHandler.RunAsync();
    }

    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_clientResponseHandler = IBApiAdapter.GetInstance(m_logger);
      m_ip = (string)m_configuration!.Configuration[IpKey];
      m_port = (int)m_configuration!.Configuration[PortKey];
    }

    public override object Request(string ticker, Resolution resolution, DateTime start, DateTime end)
    {
      throw new NotImplementedException();
    }

    //properties
    public IBApiAdapter ClientResponseHandler { get => m_clientResponseHandler; }

    //methods


  }
}
