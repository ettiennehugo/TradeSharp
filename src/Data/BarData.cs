using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Bar data implementation for observable price bar intervals bar.
  /// </summary>
  public partial class BarData : ObservableObject, IBarData
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public BarData() 
    {
      Resolution = Resolution.Days;
      DateTime = DateTime.MinValue;
      Open = double.MinValue;
      High = double.MinValue;
      Low = double.MinValue;
      Close = double.MinValue;
      Volume = double.MinValue;
    }

    public BarData(Resolution resolution, DateTime dateTime, string formatMask, double open, double high, double low, double close, double volume)
    {
      Resolution = resolution; 
      DateTime = dateTime;
      PriceFormatMask = formatMask;
      Open = open;
      High = high;
      Low = low;
      Close = close;
      Volume = volume;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private Resolution m_resolution;
    [ObservableProperty] private DateTime m_dateTime;
    [ObservableProperty] private double m_open;
    [ObservableProperty] private double m_high;
    [ObservableProperty] private double m_low;
    [ObservableProperty] private double m_close;
    [ObservableProperty] private double m_volume;
    [ObservableProperty] private string m_priceFormatMask = Constants.DefaultPriceFormatMask;
    public string FormattedOpen { get => Open.ToString(PriceFormatMask); }
    public string FormattedHigh { get => High.ToString(PriceFormatMask); }
    public string FormattedLow { get => Low.ToString(PriceFormatMask); }
    public string FormattedClose { get => Close.ToString(PriceFormatMask); }
    public string FormattedVolume { get => Volume.ToString(PriceFormatMask); }

    //methods
    public IBarData Clone()
    {
      return new BarData(Resolution, DateTime, PriceFormatMask, Open, High, Low, Close, Volume); 
    }

    //NOTE: The equals is used for searching collections to find bar data.
    public override bool Equals(object? other) 
    {
      if (other == null || !(other is BarData)) return false;
      IBarData barData = (IBarData)other;
      return barData.DateTime == DateTime;
    }

    public override int GetHashCode()
    {
      return DateTime.GetHashCode();
    }

    public int CompareTo(object? o)
    {
      if (o == null || !(o is BarData)) return 1;
      IBarData barData = (IBarData)o;
      return DateTime.CompareTo(barData.DateTime);
    }
  }
}
