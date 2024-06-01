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
      BidSize = double.MinValue;
      Ask = double.MinValue;
      AskSize = double.MinValue;
      Last = double.MinValue;
      LastSize = double.MinValue;
    }

    public Level1Data(DateTime dateTime, double bid, double bidSize, double ask, double askSize, double last, double lastSize)
    {
      DateTime = dateTime;
      Bid = bid;
      BidSize = bidSize;
      Ask = ask;
      AskSize = askSize;
      Last = last;
      LastSize = lastSize;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] DateTime m_dateTime;
    [ObservableProperty] double m_bid;
    [ObservableProperty] double m_bidSize;
    [ObservableProperty] double m_ask;
    [ObservableProperty] double m_askSize;
    [ObservableProperty] double m_last;
    [ObservableProperty] double m_lastSize;

    //methods


  }
}
