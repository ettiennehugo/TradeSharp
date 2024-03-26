using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Order held in an account for a specifc instrument with a specific status. Allows for custom properties to be defined so that users can access advanced
  /// features supported by specific brokers.
  /// </summary>
  [ComVisible(true)]
  [Guid("3C1D72CF-CDAB-401E-9049-53B1411E2E78")]
  public abstract class Order
  {
    //constants


    //enums
    /// <summary>
    /// Order status within a specific account.
    /// </summary>
    public enum OrderStatus
    {
      Open,
      Filled,
      Cancelled
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
    public Account Account { get; internal set; }
    public Instrument Instrument { get; internal set; }
    public OrderStatus Status { get; internal set; }
    public IDictionary<string, CustomProperty> CustomProperties { get; internal set; }

    //methods
    public abstract void Send();
    public abstract void Cancel();
  }
}
