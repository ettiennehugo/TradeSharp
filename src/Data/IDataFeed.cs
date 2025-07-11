﻿namespace TradeSharp.Data
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
  public interface IDataFeed : IDisposable
  { 
    //constants


    //enums


    //types


    //attributes


    //properties
    Instrument Instrument { get; }
    Resolution Resolution { get; }
    DateTime From { get; }
    DateTime To { get; }
    ToDateMode ToDateMode { get; }
    int CurrentBar { get; }
    bool IsLastBar { get; }
    int Interval { get; }
    int Count { get; }
    IDataStream<DateTime> DateTime { get; }
    IDataStream<double> Open{ get; }
    IDataStream<double> High { get; }
    IDataStream<double> Low { get; }
    IDataStream<double> Close { get; }
    IDataStream<double> Volume { get; }
    IDataStream<double> BidPrice { get; }
    IDataStream<double> BidVolume { get; }
    IDataStream<double> AskPrice { get; }
    IDataStream<double> AskVolume { get; }
    IDataStream<double> LastPrice { get; }
    IDataStream<double> LastVolume { get; }

    //methods


  }
}