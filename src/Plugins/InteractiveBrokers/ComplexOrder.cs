using IBApi;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Complex order implementation for Interactive Brokers.
  /// </summary>
  public class ComplexOrder: TradeSharp.Data.ComplexOrder
  {
    //constants


    //enums


    //types


    //attributes
    protected ServiceHost m_serviceHost;

    //constructors
    public ComplexOrder(Account account, Instrument instrument, ServiceHost serviceHost) : base(account, instrument)
    {
      Order = new IBApi.Order();
      Order.Account = Account.Name;
      m_serviceHost = serviceHost;
      Order.OrderId = m_serviceHost.Client.NextOrderId;
      Contract = m_serviceHost.Instruments.From(instrument)!;
      defineCustomProperties();
    }

    //finalizers


    //interface implementations


    //properties
    public IBApi.Order Order { get; protected set; }
    public Contract Contract { get; protected set; }

    //methods
    public override void Send()
    {
      m_serviceHost.Client.ClientSocket!.placeOrder(Order.OrderId, Contract, Order);
    }

    public override void Cancel()
    {
      m_serviceHost.Client.ClientSocket!.cancelOrder(Order.OrderId);
    }

    //https://ibkrcampus.com/ibkr-api-page/twsapi-ref/#order-ref
    protected void defineCustomProperties()
    {
      m_serviceHost.BrokerPlugin.defineCommonOrderProperties(this);  

      //TODO: implement custom properties for Interactive Brokers

    }

  }
}
