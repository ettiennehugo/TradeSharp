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
      Synthetic = true;   //above data is made up so flag it as synthetic
    }

    public BarData(Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume, bool synthetic)
    {
      Resolution = resolution; 
      DateTime = dateTime;
      Open = open;
      High = high;
      Low = low;
      Close = close;
      Volume = volume;
      Synthetic = synthetic;
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
    [ObservableProperty] private bool m_synthetic;

    //methods
    public IBarData Clone()
    {
      return new BarData(Resolution, DateTime, Open, High, Low, Close, Volume, Synthetic); 
    }

  }
}
