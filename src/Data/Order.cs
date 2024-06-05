using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
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

    //types


    //attributes


    //constructors
    public Order() 
    {
      CustomProperties = new Dictionary<string, CustomProperty>();
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private Account m_account;
    [ObservableProperty] private Instrument m_instrument;
    [ObservableProperty] private OrderStatus m_status;
    [ObservableProperty] private double m_quantity;
    [ObservableProperty] private double m_filled;
    [ObservableProperty] private double m_remaining;
    [ObservableProperty] private double m_averageFillPrice;
    [ObservableProperty] private double m_lastFillPrice;
    public IDictionary<string, CustomProperty> CustomProperties { get; internal set; }

    //methods
    public abstract void Send();
    public abstract void Cancel();
  }
}
