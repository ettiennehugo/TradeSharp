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


    //constructors


    //finalizers


    //interface implementations


    //properties
    public long OrderId { get; protected set; }

    //methods
    public override void Send() 
    {
    
      //TODO send the order to the broker.

    }

    public override void Cancel() 
    {
    
      //TODO cancel the order at the broker.
    
    }
  }
}
