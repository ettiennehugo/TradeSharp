using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for broker accounts.
  /// </summary>
  public abstract class Account
  {
    //constants


    //enums


    //types


    //attributes
    protected string m_name;
    protected List<Position> m_positions;
    protected List<Order> m_orders;

    //constructors
    public Account(string name)
    {
      m_name = name;
      m_positions = new List<Position>();
      m_orders = new List<Order>();
    }

    //finalizers


    //interface implementations


    //properties
    public string Name { get => m_name; }
    public bool Default { get; set; }   //if multiple accounts are present this flag will be set for the default account
    IList<Position> Positions { get => m_positions; }
    IList<Order> Orders { get => m_orders; }

    //methods
    /// <summary>
    /// Order creation methods, different brokers need to implement these methods and define their own specific order types
    /// to encapsulate order behavior supported by the brokers.
    /// </summary>
    public abstract SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, double price);
    public abstract ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, double quantity);
  }
}
