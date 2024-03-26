using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Implementation of a broker plugin for Interactive Brokers, this is typically an adapter between TradeSharp and Interactive Brokers.
  /// </summary>
  public class BrokerPlugin : TradeSharp.Data.BrokerPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected IBApiAdapter m_clientResponseHandler;
    protected string m_ip;
    protected int m_port;


    //constructors
    public BrokerPlugin(IHost serviceHost) : base("InteractiveBrokers", serviceHost) { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_ip = (string)m_configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = (int)m_configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey];
      m_clientResponseHandler = IBApiAdapter.GetInstance(logger, m_serviceHost, Configuration);
    }

    public override void Connect()
    {
      base.Connect();
      m_clientResponseHandler.Connect(m_ip, m_port);
      m_clientResponseHandler.RunAsync();
    }

    //properties
    public override IList<TradeSharp.Data.Account> Accounts { get => (IList<TradeSharp.Data.Account>)m_clientResponseHandler.Accounts; }
    public IBApiAdapter ClientResponseHandler { get => m_clientResponseHandler; }

    //methods
    public void defineCustomProperties(Order order)
    {
      if (order is SimpleOrder)
      {

        //TODO: Define the custom properties for the order.

      }
      else if (order is ComplexOrder)
      {

        //TODO: Define the custom properties for the order.

      }
      else
      {
        m_logger.LogError("Order type not supported.");
      }
    }
  }
}
