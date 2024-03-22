using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Simple order type: market, limit, stop, and stop limit orders for long/short positions.
  /// </summary>
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
    public OrderType Type { get; internal set; }
    public double Quantity { get; internal set; }
    public double CostBasis { get; internal set; }

    //methods


  }
}
