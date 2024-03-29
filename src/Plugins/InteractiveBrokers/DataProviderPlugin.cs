using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using System.Runtime.InteropServices;


namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Data provider plugin implementation for Interactive Brokers.
  /// </summary>
  [ComVisible(true)]
  [Guid("D6BF3AE3-F358-4066-B177-D9763F927D67")]
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
      m_ip = (string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = int.Parse((string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey]);
      m_clientResponseHandler = IBApiAdapter.GetInstance(m_logger, ServiceHost, Configuration);
    }

    public override object Request(string ticker, Resolution resolution, DateTime start, DateTime end)
    {
      throw new NotImplementedException();
    }

    //properties
    public override int ConnectionCountMax => 1;  //IB limits the number of connections to 1 and it's also limited by the number of calls
    public IBApiAdapter ClientResponseHandler { get => m_clientResponseHandler; }

    //methods


  }
}
