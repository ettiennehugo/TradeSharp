using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Order status within a specific account.
  /// </summary>
  public enum OrderStatus
  {
    PendingSubmit,  //order is pending submission
    PendingCancel,  //order is pending cancellation
    Inactive,       //order was received but was rejected or cancelled
    Open,           //order is open/waiting to be filled
    Filled,         //order is filled
    Cancelled       //order is cancelled
  }

  /// <summary>
  /// Order time in force.
  /// </summary>
  public enum OrderTimeInForce
  {
    Day,
    [Description("Good Till Cancelled")]
    GTC,
    [Description("Good Till Date")]
    GTD,
    [Description("Immediate or Cancelled")]
    IOC,
    [Description("Fill or Kill")]
    FOK 
  }

  /// <summary>
  /// Order held in an account for a specifc instrument with a specific status. Allows for custom properties to be defined so that users can access advanced
  /// features supported by specific brokers.
  /// </summary>
  [ComVisible(true)]
  [Guid("3C1D72CF-CDAB-401E-9049-53B1411E2E78")]
  public abstract partial class Order: ObservableObject
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Order(Account account, Instrument instrument) 
    {
      Account = account;
      Instrument = instrument;
      Status = OrderStatus.PendingSubmit;
      TimeInForce = OrderTimeInForce.Day;
      GoodTillDate = DateTime.Now.AddDays(1);
      CustomProperties = new Dictionary<string, CustomProperty>();
    }

    //finalizers


    //interface implementations


    //properties
    public Instrument Instrument { get; private set; }
    [ObservableProperty] private Account m_account;
    [ObservableProperty] private OrderStatus m_status;
    [ObservableProperty] private OrderTimeInForce m_timeInForce;
    [ObservableProperty] private DateTime m_goodTillDate;   //use for GTC and GTD tif orders
    [ObservableProperty] private double m_quantity;
    [ObservableProperty] private double m_filled;
    [ObservableProperty] private double m_remaining;
    [ObservableProperty] private decimal m_averageFillPrice;
    [ObservableProperty] private decimal m_lastFillPrice;
    public IDictionary<string, CustomProperty> CustomProperties { get; internal set; }

    //methods
    public abstract void Send();
    public abstract void Cancel();
  }
}
