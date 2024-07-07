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
    double Volume { get; set; }
    string PriceFormatMask { get; set; }
    string FormattedOpen { get; }
    string FormattedHigh { get; }
    string FormattedLow { get; }
    string FormattedClose { get; }
    string FormattedVolume { get; }

    //methods
    IBarData Clone();
  }
}
