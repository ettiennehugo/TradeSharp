namespace TradeSharp.Data
{
  /// <summary>
  /// Order status within a specific account.
  /// </summary>
  public enum OrderStatus
  {
    Open,
    Filled,
    Cancelled
  }

  /// <summary>
  /// Order held in an account for a specifc instrument with a specific status. Allows for custom properties to be defined so that users can access advanced
  /// features supported by specific brokers.
  /// </summary>
  public abstract class Order
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Order() 
    {
      CustomPropertyDefinitions = new Dictionary<string, string>();
      CustomProperties = new Dictionary<string, object>();
    }

    //finalizers


    //interface implementations


    //properties
    public Account Account { get; internal set; }
    public Instrument Instrument { get; internal set; }
    public OrderStatus Status { get; internal set; }
    public IDictionary<string, string> CustomPropertyDefinitions { get; internal set; }
    public IDictionary<string, object> CustomProperties { get; internal set; }

    //methods
    public abstract void Send();
    public abstract void Cancel();
  }
}
