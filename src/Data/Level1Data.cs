using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// Level1 data implementation for observable data bars.
  /// </summary>
  public partial class Level1Data : ObservableObject, ILevel1Data
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Level1Data()
    {
      DateTime = DateTime.MinValue;
      Bid = double.MinValue;
      BidSize = long.MinValue;
      Ask = double.MinValue;
      AskSize = long.MinValue;
      Last = double.MinValue;
      LastSize = long.MinValue;
    }

    public Level1Data(DateTime dateTime, double bid, long bidSize, double ask, long askSize, double last, long lastSize, bool synthetic)
    {
      DateTime = dateTime;
      Bid = bid;
      BidSize = bidSize;
      Ask = ask;
      AskSize = askSize;
      Last = last;
      LastSize = lastSize;
      Synthetic = synthetic;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] DateTime m_dateTime;
    [ObservableProperty] double m_bid;
    [ObservableProperty] long m_bidSize;
    [ObservableProperty] double m_ask;
    [ObservableProperty] long m_askSize;
    [ObservableProperty] double m_last;
    [ObservableProperty] long m_lastSize;
    [ObservableProperty] bool m_synthetic;

    //methods


  }
}
