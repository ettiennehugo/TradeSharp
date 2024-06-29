using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for broker plugins.
  /// </summary>
  [ComVisible(true)]
  [Guid("E7E2167A-8EDA-46C4-B610-F989A1DCB840")]
  public abstract class BrokerPlugin : Plugin, IBrokerPlugin
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public BrokerPlugin(string name, string description): base(name, description)  { }

    //finalizers


    //interface implementations


    //properties
    public abstract ObservableCollection<Account> Accounts { get; }

    //delegates
    public virtual event AccountsUpdatedHandler? AccountsUpdated;
    public virtual event AccountUpdatedHandler? AccountUpdated;
    public virtual event PositionUpdatedHandler? PositionUpdated;
    public virtual event OrderUpdatedHandler? OrderUpdated;

    //methods
    public abstract SimpleOrder CreateSimpleOrder(Account account, Instrument instrument);
    public abstract ComplexOrder CreateComplexOrder(Account account, Instrument instrument);

    public void raiseAccountsUpdated()
    {
      AccountsUpdated?.Invoke(this, new AccountsUpdatedArgs());
    }

    public void raiseAccountUpdated(AccountUpdatedArgs args)
    {
      AccountUpdated?.Invoke(this, args);
    }

    public void raisePositionUpdated(PositionUpdatedArgs args)
    {
      PositionUpdated?.Invoke(this, args);
    }

    public void raiseOrderUpdated(OrderUpdatedArgs args)
    {
      OrderUpdated?.Invoke(this, args);
    }
  }
}
