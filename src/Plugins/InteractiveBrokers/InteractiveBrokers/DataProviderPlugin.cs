using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected IBApiAdapter m_clientResponseHandler;
    protected string m_ip;
    protected int m_port;

    //constructors
    public DataProviderPlugin(IHost serviceHost) : base("InteractiveBrokers", serviceHost) { }

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
      m_clientResponseHandler = IBApiAdapter.GetInstance(m_logger, m_serviceHost, Configuration);
      m_ip = (string)m_configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = (int)m_configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey];
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
