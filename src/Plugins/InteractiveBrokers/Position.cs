namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Position class for Interactive Brokers.
  /// </summary>
  public class Position : TradeSharp.Data.Position
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Position(Data.Account account, Data.Instrument instrument, PositionDirection direction, double size, double averageCost) : base(account, instrument, direction, size, averageCost) { }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void setDirection(PositionDirection value) { Direction = value; }
    public void setSize(double value) { Size = value; }
    public void setAverageCost(double value) { AverageCost = value; }
  }
}
