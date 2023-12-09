namespace TradeSharp.Data
{
  /// <summary>
  /// Bar data interface to represent a single bar of data over a specific time period.
  /// </summary>
  public interface IBarData: IComparable
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    Resolution Resolution { get; set; }
    DateTime DateTime { get; set; }
    double Open { get; set; }
    double High { get; set; }
    double Low { get; set; }
    double Close { get; set; }
    long Volume { get; set; }
    bool Synthetic { get; set; }

    //methods
    IBarData Clone();
  }
}
