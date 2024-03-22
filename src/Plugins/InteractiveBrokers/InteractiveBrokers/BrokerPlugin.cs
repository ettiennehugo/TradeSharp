using Microsoft.Extensions.Logging;
using System.Globalization;
using Tradesharp.InteractiveBrokers;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Implementation of a broker plugin for Interactive Brokers, this is typically an adapter between TradeSharp and Interactive Brokers.
  /// </summary>
  public class BrokerPlugin : TradeSharp.Data.BrokerPlugin
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
    public BrokerPlugin() : base("InteractiveBrokers") { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_clientResponseHandler = IBApiAdapter.GetInstance(logger);
      m_ip = (string)m_configuration!.Configuration[IpKey];
      m_port = (int)m_configuration!.Configuration[PortKey];
    }

    public override void Connect()
    {
      base.Connect();
      m_clientResponseHandler.Connect(m_ip, m_port);
      m_clientResponseHandler.RunAsync();
    }

    //properties
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
