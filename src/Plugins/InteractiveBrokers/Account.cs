using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// InteractiveBrokers account.
  /// </summary>
  public partial class Account : TradeSharp.Data.Account
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public Account() : base() { }

    //finalizers


    //interface implementations
    public override SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, decimal price)
    {

      throw new NotImplementedException();

    }

    public override ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, decimal quantity)
    {

      throw new NotImplementedException();

    }

    public override void CancelOrder(Data.Order order)
    {

      throw new NotImplementedException();

    }

    //properties


    //methods


  }
}
