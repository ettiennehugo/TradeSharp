using System;
using IBApi;
using System.ComponentModel;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Simple order class for Interactive Brokers.
  /// </summary>
  public class SimpleOrder : TradeSharp.Data.SimpleOrder
  {
    //constants
    public const string PropertyAllOrNone = "AllOrNone";
    public const string PropertyDisplaySize = "DisplaySize";
    public const string PropertyTrailStopPrice = "TrailStopPrice";
    public const string PropertyTrailPercent = "TrailPercent";

    //enums


    //types


    //attributes
    protected ServiceHost m_serviceHost;

    //constructors
    public SimpleOrder(Account account, Instrument instrument, ServiceHost serviceHost) : base(account, instrument)
    {
      Order = new IBApi.Order();
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

      switch (Action)
      {
        case OrderAction.Buy:
          Order.Action = Constants.OrderActionBuy;
          break;
        case OrderAction.Sell:
          Order.Action = Constants.OrderActionSell;
          break;
        case OrderAction.BuyToCover:
          //TODO: Check that account holds short position of this type
          Order.Action = Constants.OrderActionBuy;
          break;
        case OrderAction.SellShort:
          //TODO: Check that account does not hold a long position of this type - otherwise it would result in a sale of the long position.
          Order.Action = Constants.OrderActionSell;
          break;
      }

      switch (Type)
      {
        case OrderType.Market:
          Order.OrderType = Constants.OrderTypeMarket;
          break;
        case OrderType.Limit:
          Order.OrderType = Constants.OrderTypeLimit;
          break;
        case OrderType.Stop:
          Order.OrderType = Constants.OrderTypeStop;
          break;
        case OrderType.StopLimit:
          Order.OrderType = Constants.OrderTypeStopLimit;
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

      Order.TotalQuantity = Quantity;
      Order.AuxPrice = StopPrice;   //auxiliry price is used for the stop price
      Order.LmtPrice = LimitPrice;
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
      var property = new CustomProperty(this, PropertyAllOrNone, "Indicates whether or not all the order has to be filled on a single execution", typeof(bool));
      property.PropertyChanged += (s, e) => onAllOrNoneChanged();
      CustomProperties.Add(PropertyAllOrNone, property);
      property = new CustomProperty(this, PropertyDisplaySize, "Publicly disclosed order size", typeof(int));
      property.PropertyChanged += (s, e) => onDisplaySizeChanged();
      CustomProperties.Add(PropertyDisplaySize, property);

      property = new CustomProperty(this, PropertyTrailStopPrice, "Trail stop price for TRAIL LIMIT orders", typeof(double));
      property.PropertyChanged += (s, e) => onTrailStopPriceChanged();
      CustomProperties.Add(PropertyTrailStopPrice, property);

      property = new CustomProperty(this, PropertyTrailPercent, "\tSpecifies the trailing amount of a trailing stop order as a percentage", typeof(double));
      property.PropertyChanged += (s, e) => onTrailPercentChanged();
      CustomProperties.Add(PropertyTrailPercent, property);
    }

    protected void onAllOrNoneChanged()
    {
      if (CustomProperties[PropertyAllOrNone].Value is bool allOrNone) Order.AllOrNone = allOrNone;
    }

    protected void onDisplaySizeChanged()
    {
      if (CustomProperties[PropertyDisplaySize].Value is int displaySize) Order.DisplaySize = displaySize;
    }

    protected void onTrailStopPriceChanged()
    {
      if (CustomProperties[PropertyTrailStopPrice].Value is double trailStopPrice) Order.TrailStopPrice = trailStopPrice;
    }

    protected void onTrailPercentChanged() {
      if (CustomProperties[PropertyTrailPercent].Value is double trailPercent) Order.TrailingPercent = trailPercent;
    }
  }
}
