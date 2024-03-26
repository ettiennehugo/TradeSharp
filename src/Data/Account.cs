using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for broker accounts.
  /// </summary>
  [ComVisible(true)]
  [Guid("9EA66DAD-3B05-4C14-8635-138ED999A80C")]
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
      CustomProperties = new Dictionary<string, CustomProperty>();
    }

    //finalizers


    //interface implementations


    //properties
    public string Name { get => m_name; }
    public bool Default { get; protected set; }              //if multiple accounts are present this flag will be set for the default account
    public string BaseCurrency { get; protected set; }       //bae currency of the account
    public IList<Position> Positions { get => m_positions; }
    public IList<Order> Orders { get => m_orders; }
    public double NetLiquidation { get; protected set; }     //all cash and securities in the account
    public double SettledCash { get; protected set; }        //cash recognised as settled
    public double BuyingPower { get; protected set; }        //currency available to trade securities
    public double MaintenanceMargin { get; protected set; }  //margin require for whole account
    public double PositionsValue { get; protected set; }     //currency value of all poisitions held
    public double AvailableFunds { get; protected set; }     //cash available for trading
    public double ExcessLiquidity { get; protected set; }    //cash available for trading after considering margin requirements
    public DateTime LastSyncDateTime { get; protected set; } //last time the account was synced with the broker
    public IDictionary<string, CustomProperty> CustomProperties { get; protected set; }  //other properties supported by the broker

    //methods
    /// <summary>
    /// Order creation methods, different brokers need to implement these methods and define their own specific order types
    /// to encapsulate order behavior supported by the brokers.
    /// </summary>
    public abstract SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, double price);
    public abstract ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, double quantity);
  }
}
