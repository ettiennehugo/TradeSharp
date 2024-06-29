using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Simple order type: market, limit, stop, and stop limit orders for long/short positions.
  /// </summary>
  [ComVisible(true)]
  [Guid("6995B0CB-6A57-4797-B52F-B81466A562DE")]
  public abstract partial class SimpleOrder: Order
  {
    //constants


    //enums
    public enum OrderAction
    {
      Buy,
      Sell,
      BuyToCover,
      SellShort
    }

    /// <summary>
    /// Basic order type supported by most brokers.
    /// </summary>summary>
    public enum OrderType
    {
      Market,
      Limit,
      Stop,
      StopLimit
    }

    //types


    //attributes


    //constructors
    public SimpleOrder(Account account, Instrument instrument): base(account, instrument) { }


    //finalizers


    //interface implementations


    //properties

    [ObservableProperty] OrderType m_type;
    [ObservableProperty] OrderAction m_action;
    [ObservableProperty] double m_costBasis;
    [ObservableProperty] double m_stopPrice;
    [ObservableProperty] double m_limitPrice;

    //methods


  }
}
