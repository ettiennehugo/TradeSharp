using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Simple order type: market, limit, stop, and stop limit orders for long/short positions.
  /// </summary>
  [ComVisible(true)]
  [Guid("6995B0CB-6A57-4797-B52F-B81466A562DE")]
  public abstract class SimpleOrder: Order
  {
    //constants


    //enums
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


    //finalizers


    //interface implementations


    //properties
    public OrderType Type { get; protected set; }
    public double Quantity { get; protected set; }
    public double CostBasis { get; protected set; }

    //methods


  }
}
