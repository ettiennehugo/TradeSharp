using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Trading position held within an account at a broker.
  /// </summary>
  [ComVisible(true)]
  [Guid("B2054674-8C40-4BAA-8BE8-D1D6CAFDC18B")]
  public class Position
  {
    //constants


    //enums
    /// <summary>
    /// Direction of the position held.
    /// </summary>
    public enum PositionDirection
    {
      Long,
      Short
    }


    //types


    //attributes


    //constructors
    public Position(Account account, Instrument instrument, PositionDirection direction, double size, double averageCost)
    {
      Account = account;
      Instrument = instrument;
      Direction = direction;
      Size = size;
      AverageCost = averageCost;
    }

    //finalizers


    //interface implementations


    //properties
    public Account Account { get; protected set; }
    public Instrument Instrument { get; protected set; }
    public PositionDirection Direction { get; protected set; }
    public double Size { get; protected set; }
    public double AverageCost { get; protected set; }

    //methods



  }
}
