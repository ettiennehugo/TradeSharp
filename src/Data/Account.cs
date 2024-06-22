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
  public abstract partial class Account : ObservableObject
  {
    //constants


    //enums


    //types


    //attributes
    protected ObservableCollection<Position> m_positions;
    protected ObservableCollection<Order> m_orders;

    //constructors
    public Account(IBrokerPlugin brokerPlugin)
    {
      BrokerPlugin = brokerPlugin;
      Name = string.Empty;
      AccountType = string.Empty;
      Currency = string.Empty;
      NetLiquidation = decimal.Zero;
      SettledCash = decimal.Zero;
      BuyingPower = decimal.Zero;
      MaintenanceMargin = decimal.Zero;
      PositionsValue = decimal.Zero;
      AvailableFunds = decimal.Zero;
      ExcessLiquidity = decimal.Zero;
      LastSyncDateTime = DateTime.MinValue;
      m_positions = new ObservableCollection<Position>();
      m_orders = new ObservableCollection<Order>();
      CustomProperties = new Dictionary<string, CustomProperty>();
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private IBrokerPlugin m_brokerPlugin; //broker plugin associated with the account
    [ObservableProperty] private string m_name;                //name of the account
    [ObservableProperty] private bool m_default;               //if multiple accounts are present this flag will be set for the default account
    [ObservableProperty] private string m_accountType;         //type of account
    [ObservableProperty] private string m_currency;            //currency of the account
    [ObservableProperty] private decimal m_netLiquidation;     //all cash and securities in the account
    [ObservableProperty] private decimal m_settledCash;        //cash recognised as settled
    [ObservableProperty] private decimal m_buyingPower;        //currency available to trade securities
    [ObservableProperty] private decimal m_maintenanceMargin;  //margin require for whole account
    [ObservableProperty] private decimal m_positionsValue;     //currency value of all poisitions held
    [ObservableProperty] private decimal m_availableFunds;     //cash available for trading
    [ObservableProperty] private decimal m_excessLiquidity;    //cash available for trading after considering margin requirements
    [ObservableProperty] private DateTime m_lastSyncDateTime;  //last time the account was synced with the broker
    public ObservableCollection<Position> Positions { get => m_positions; }  //instrument positions held in the account
    public ObservableCollection<Order> Orders { get => m_orders; }    //orders placed in the account
    public IDictionary<string, CustomProperty> CustomProperties { get; protected set; }  //other properties supported by the broker

    //methods
    /// <summary>
    /// Order creation methods, different brokers need to implement these methods and define their own specific order types
    /// to encapsulate order behavior supported by the brokers.
    /// </summary>
    public abstract SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, decimal price);
    public abstract ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, decimal quantity);
    public abstract void CancelOrder(Order order);
  }

  /// <summary>
  /// Empty account used when no account is selected, does not support order creation or cancellation.
  /// </summary>
  public class EmptyAccount : Account
  {
    public EmptyAccount() : base(null) { Name = "No account selected"; LastSyncDateTime = DateTime.Now; }

    public override void CancelOrder(Order order)
    {
      throw new NotImplementedException("Empty account does not support order cancellation.");
    }

    public override SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, decimal price)
    {
      throw new NotImplementedException("Empty account does not support simple order creation.");
    }

    public override ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, decimal quantity)
    {
      throw new NotImplementedException("Empty account does not support complex order creation.");
    }
  }

}
