namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Simple order class for Interactive Brokers.
  /// </summary>
  public class SimpleOrder: TradeSharp.Data.SimpleOrder
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
