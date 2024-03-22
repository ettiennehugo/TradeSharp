using TradeSharp.Data;

namespace InteractiveBrokers
{
  /// <summary>
  /// InteractiveBrokers account.
  /// </summary>
  public class Account : TradeSharp.Data.Account
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public Account(string name) : base(name) { }

    //finalizers


    //interface implementations
    public override SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, double price)
    {
      throw new NotImplementedException();
    }

    public override ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, double quantity)
    {
      throw new NotImplementedException();
    }

    //properties


    //methods


  }
}
