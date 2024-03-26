using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// InteractiveBrokers account.
  /// </summary>
  public class Account : TradeSharp.Data.Account
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public Account(string name) : base(name) { }

    //finalizers


    //interface implementations
    public override SimpleOrder CreateOrder(string symbol, SimpleOrder.OrderType type, double quantity, double price)
    {

      throw new NotImplementedException();

    }

    public override ComplexOrder CreateOrder(string symbol, ComplexOrder.OrderType type, double quantity)
    {

      throw new NotImplementedException();

    }

    //properties


    //methods
    public void setDefault(bool value) { Default = value; }
    public void setBaseCurrency(string value) { BaseCurrency = value; }
    public void setNetLiquidation(double value) { NetLiquidation = value; }
    public void setSettledCash(double value) { SettledCash = value; }
    public void setBuyingPower(double value) { BuyingPower = value; }
    public void setMaintenanceMargin(double value) { MaintenanceMargin = value; }
    public void setPositionsValue(double value) { PositionsValue = value; }
    public void setAvailableFunds(double value) { AvailableFunds = value; }
    public void setExcessLiquidity(double value) { ExcessLiquidity = value; }
    public void setLastSyncDateTime(DateTime value) { LastSyncDateTime = value; }
  }
}
