using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for broker accounts.
  /// </summary>
  [ComVisible(true)]
  [Guid("9EA66DAD-3B05-4C14-8635-138ED999A80C")]
  public abstract partial class Account: ObservableObject
  {
    //constants


    //enums


    //types


    //attributes
    protected string m_name;
    protected ObservableCollection<Position> m_positions;
    protected ObservableCollection<Order> m_orders;

    //constructors
    public Account(string name)
    {
      m_name = name;
      m_positions = new ObservableCollection<Position>();
      m_orders = new ObservableCollection<Order>();
      CustomProperties = new Dictionary<string, CustomProperty>();
    }

    //finalizers


    //interface implementations


    //properties
    public string Name { get => m_name; }
    [ObservableProperty] private bool m_default;              //if multiple accounts are present this flag will be set for the default account
    [ObservableProperty] private string m_accountType;        //type of account
    [ObservableProperty] private string m_baseCurrency;       //base currency of the account
    [ObservableProperty] private double m_netLiquidation;     //all cash and securities in the account
    [ObservableProperty] private double m_settledCash;        //cash recognised as settled
    [ObservableProperty] private double m_buyingPower;        //currency available to trade securities
    [ObservableProperty] private double m_maintenanceMargin;  //margin require for whole account
    [ObservableProperty] private double m_positionsValue;     //currency value of all poisitions held
    [ObservableProperty] private double m_availableFunds;     //cash available for trading
    [ObservableProperty] private double m_excessLiquidity;    //cash available for trading after considering margin requirements
    [ObservableProperty] private DateTime m_lastSyncDateTime; //last time the account was synced with the broker
    public ObservableCollection<Position> Positions { get => m_positions; }  //instrument positions held in the account
    public ObservableCollection<Order> Orders { get => m_orders; }    //orders placed in the account
    public IDictionary<string, CustomProperty> CustomProperties { get; protected set; }  //other properties supported by the broker

    //methods
    /// <summary>
    /// Order creation methods, different brokers need to implement these methods and define their own specific order types
    /// to encapsulate order behavior supported by the brokers.
    /// </summary>
    public abstract SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, double price);
    public abstract ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, double quantity);
    public abstract void CancelOrder(Order order);
  }
}
