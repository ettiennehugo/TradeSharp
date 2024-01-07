using CommunityToolkit.Mvvm.ComponentModel;

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
      Resolution = Resolution.Day;
      DateTime = DateTime.MinValue;
      Open = double.MinValue;
      High = double.MinValue;
      Low = double.MinValue;
      Close = double.MinValue;
      Volume = long.MinValue;
    }

    public BarData(Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume)
    {
      Resolution = resolution; 
      DateTime = dateTime;
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
    [ObservableProperty] private long m_volume;

    //methods
    public IBarData Clone()
    {
      return new BarData(Resolution, DateTime, Open, High, Low, Close, Volume); 
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
