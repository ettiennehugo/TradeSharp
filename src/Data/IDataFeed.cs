namespace TradeSharp.Data
{
  /// <summary>
  /// To date of data feed is either pinned to specific date or it is open ended to receive new data as it becomes available.
  /// </summary>
  public enum ToDateMode
  {
    Pinned,   //To date is static
    Open,     //To date is open ended
  }

  /// <summary>
  /// Interface to be supported by data feeds. It observers the data manager price changes, potentially modifies them based on the feed attributes
  /// and then propagates them to the subscribers - so it's both an observer and an observable.
  /// </summary>
  public interface IDataFeed : Common.IObserver<PriceChange>, Common.IObservable<PriceChange>, IDisposable
  { 
    //constants


    //enums


    //types


    //attributes


    //properties
    IInstrument Instrument { get; }
    Resolution Resolution { get; }
    DateTime From { get; }
    DateTime To { get; }
    ToDateMode ToDateMode { get; }
    int CurrentBar { get; }
    bool IsLastBar { get; }
    int Interval { get; }
    int Count { get; }
    PriceDataType PriceDataType { get; }
    IDataStream<DateTime> DateTime { get; }
    IDataStream<double> Open{ get; }
    IDataStream<double> High { get; }
    IDataStream<double> Low { get; }
    IDataStream<double> Close { get; }
    IDataStream<long> Volume { get; }
    IDataStream<double> BidPrice { get; }
    IDataStream<long> BidVolume { get; }
    IDataStream<double> AskPrice { get; }
    IDataStream<long> AskVolume { get; }
    IDataStream<double> LastPrice { get; }
    IDataStream<long> LastVolume { get; }
    IDataStream<bool> Synthetic { get; }

    //methods


  }
}