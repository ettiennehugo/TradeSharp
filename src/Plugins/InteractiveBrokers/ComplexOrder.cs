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
      //only send order if it's brand new or cancelled
      if (Status != OrderStatus.New || Status != OrderStatus.Cancelled) return;

      Order.Account = Account.Name;
      Order.Rule80A = Constants.Rule80AIndividual;


      throw new NotImplementedException();


      //TODO - look at the sample app to see how you should fill in the order object details.
      // https://ibkrcampus.com/ibkr-api-page/twsapi-ref/#order-ref
      // https://ibkrcampus.com/ibkr-api-page/twsapi-ref/#comboleg-ref - it looks like the BUY/SELL side of combo orders are set in the ComboLeg object that goes into the order.

      switch (Type)
      {
        case OrderType.OSO:
          Order.OrderType = Constants.OrderTypeLimit;
        break;
        case OrderType.OCO:

        break;
        case OrderType.Bracket:

        break;
        case OrderType.Fade:

        break;
      }

      switch (TimeInForce)
      {
        case OrderTimeInForce.GTC:
          Order.Tif = Constants.OrderTimeInForceGTC;
          Order.ActiveStartTime = DateTime.Now.ToUniversalTime().ToString("yyyyMMdd HH:mm:ss Z");
          Order.ActiveStopTime = DateTime.Now.ToUniversalTime().AddMonths(6).ToString("yyyyMMdd HH:mm:ss Z");
          break;
        case OrderTimeInForce.GTD:
          Order.Tif = Constants.OrderTimeInForceGTD;
          Order.GoodTillDate = GoodTillDate.ToUniversalTime().ToString("yyyyMMdd HH:mm:ss Z");
          break;
        case OrderTimeInForce.IOC:
          Order.Tif = Constants.OrderTimeInForceIOC;
          break;
        case OrderTimeInForce.FOK:
          Order.Tif = Constants.OrderTimeInForceFOK;
          break;
        default:
          Order.Tif = Constants.OrderTimeInForceDay;
          break;
      }




      Order.Transmit = true;  //need to set this for TWS to send the order to the market

      Status = OrderStatus.PendingSubmit;
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
