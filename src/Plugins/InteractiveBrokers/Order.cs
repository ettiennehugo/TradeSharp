using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  public partial class Order: TradeSharp.Data.Order
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Order(int orderId) : base() 
    {
      OrderId = orderId;
    }

    //finalizers


    //interface implementations


    //properties
    public int OrderId { get; protected set; }

    //methods
    public override void Send()
    {
      throw new NotImplementedException();
    }

    public override void Cancel()
    {
      throw new NotImplementedException();
    }

  }
}
