using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.Common;
using static TradeSharp.Data.IDataStoreService;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Transactions;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;

namespace TradeSharp.Data
{
  /// <summary>
  /// Data feed implementation used to access data from the IDataManager, in general the Curent and Data properties must be implemented
  /// specific for each data manager to allow quick access to the underlying data.
  /// </summary>
  public class DataFeed : IDataFeed
  {
    //constants


    //enums


    //types


    //attributes
    protected IConfigurationService m_configuration;
    protected IDataProvider m_dataProvider;
    protected IDataStoreService m_dataStore;
    protected DataStream<DateTime> m_dateTime;
    protected DateTime[] m_dateTimeData;
    protected DataStream<double> m_open;
    protected double[] m_openData;
    protected DataStream<double> m_high;
    protected double[] m_highData;
    protected DataStream<double> m_low;
    protected double[] m_lowData;
    protected DataStream<double> m_close;
    protected double[] m_closeData;
    protected DataStream<long> m_volume;
    protected long[] m_volumeData;
    protected DataStream<double> m_bidPrice;
    protected double[] m_bidPriceData;
    protected DataStream<long> m_bidVolume;
    protected long[] m_bidVolumeData;
    protected DataStream<double> m_askPrice;
    protected double[] m_askPriceData;
    protected DataStream<long> m_askVolume;
    protected long[] m_askVolumeData;
    protected DataStream<double> m_lastPrice;
    protected double[] m_lastPriceData;
    protected DataStream<long> m_lastVolume;
    protected long[] m_lastVolumeData;
    protected DataStream<bool> m_synthetic;
    protected bool[] m_syntheticData;

    //constructors
    public DataFeed(IConfigurationService configuration, IDataStoreService dataStore, IDataProvider dataProvider, Instrument instrument, Resolution resolution, int interval, DateTime from, DateTime to, ToDateMode toDateMode, PriceDataType priceDataType) : base()
    {
      if (interval == 0) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
      if (from > to) throw new ArgumentOutOfRangeException(nameof(from), "From must be less than or equal to To.");

      //set general attributes
      m_configuration = configuration;
      m_dataStore = dataStore;
      m_dataProvider = dataProvider;
      Instrument = instrument;
      From = from;
      To = to;
      ToDateMode = toDateMode;
      Resolution = resolution;
      CurrentBar = 0;
      Interval = interval;
      PriceDataType = priceDataType;

      //create/initialize the data streams and associated data buffers
      m_dateTime = new DataStream<DateTime>();
      m_dateTimeData = Array.Empty<DateTime>();
      m_open = new DataStream<double>();
      m_openData = Array.Empty<double>();
      m_high = new DataStream<double>();
      m_highData = Array.Empty<double>();
      m_low = new DataStream<double>();
      m_lowData = Array.Empty<double>();
      m_close = new DataStream<double>();
      m_closeData = Array.Empty<double>();
      m_volume = new DataStream<long>();
      m_volumeData = Array.Empty<long>();
      m_synthetic = new DataStream<bool>();
      m_syntheticData = Array.Empty<bool>();
      m_bidPrice = new DataStream<double>();
      m_bidPriceData = Array.Empty<double>();
      m_bidVolume = new DataStream<long>();
      m_bidVolumeData = Array.Empty<long>();
      m_askPrice = new DataStream<double>();
      m_askPriceData = Array.Empty<double>();
      m_askVolume = new DataStream<long>();
      m_askVolumeData = Array.Empty<long>();
      m_lastPrice = new DataStream<double>();
      m_lastPriceData = Array.Empty<double>();
      m_lastVolume = new DataStream<long>();
      m_lastVolumeData = Array.Empty<long>();

      //load the data values based on inputs
      refreshDataCache();
    }

    //finalizers
    public void Dispose()
    {
      m_dateTime.Dispose();
      m_open.Dispose();
      m_high.Dispose();
      m_low.Dispose();
      m_close.Dispose();
      m_volume.Dispose();
      m_synthetic.Dispose();
      m_bidPrice.Dispose();
      m_bidVolume.Dispose();
      m_askPrice.Dispose();
      m_askVolume.Dispose();
      m_lastPrice.Dispose();
      m_lastVolume.Dispose();
    }

    //interface implementations

    //public void OnChange(IEnumerable<PriceChange> changes)
    //{
    //  //merge in the price changes received
    //  bool refreshRequired = false;
    //  DateTime fromDate = System.DateTime.MinValue;
    //  DateTime toDate = System.DateTime.MaxValue;
    //  foreach (PriceChange change in changes)
    //    if (change.Instrument.Id == Instrument.Id && change.Resolution == Resolution && change.From >= From && (change.To <= To || ToDateMode == ToDateMode.Open))
    //    {
    //      //flag that data update is required
    //      refreshRequired = true;
    //      if (ToDateMode == ToDateMode.Open && change.To > To) To = change.To; //adjust to date if change is beyond current to date with open to date mode

    //      //record from/to dates for observer updates
    //      if (fromDate == System.DateTime.MinValue && toDate == System.DateTime.MaxValue)
    //      {
    //        fromDate = change.From;
    //        toDate = change.To;
    //      }
    //      else if (change.From < fromDate) fromDate = change.From;
    //      else if (change.To > toDate) toDate = change.To;
    //    }

    //  //refresh the data caches
    //  if (refreshRequired)
    //  {
    //    PriceChange priceChange = new PriceChange();
    //    priceChange.ChangeType = PriceChangeType.Update;
    //    priceChange.Instrument = Instrument;
    //    priceChange.Resolution = Resolution;
    //    priceChange.From = fromDate;
    //    priceChange.To = toDate;
    //    m_priceChanges.Add(priceChange);
    //    refreshDataCache();
    //  }

    //  //notify any observers of the price changes if required
    //  if (m_priceChanges.Count > 0)
    //    foreach (var observerKV in m_priceChangeObservers)
    //      if (observerKV.Value.TryGetTarget(out var observer))
    //        observer.OnChange(m_priceChanges);
    //      else
    //        m_priceChangeObservers.TryRemove(observerKV.Key, out _);

    //  m_priceChanges.Clear();
    //}

    //properties
    public Instrument Instrument { get; }
    public DateTime From { get; }
    public DateTime To { get; internal set; }
    public ToDateMode ToDateMode { get; }
    public Resolution Resolution { get; }
    public int CurrentBar { get; internal set; }
    public bool IsLastBar { get { return Count == 0 || CurrentBar == Count - 1; } }
    public int Interval { get; }
    public int Count { get; internal set; }
    public PriceDataType PriceDataType { get; }
    public IDataStream<DateTime> DateTime => m_dateTime;
    public IDataStream<double> Open => m_open;
    public IDataStream<double> High => m_high;
    public IDataStream<double> Low => m_low;
    public IDataStream<double> Close => m_close;
    public IDataStream<long> Volume => m_volume;
    public IDataStream<double> BidPrice => m_bidPrice;
    public IDataStream<long> BidVolume => m_bidVolume;
    public IDataStream<double> AskPrice => m_askPrice;
    public IDataStream<long> AskVolume => m_askVolume;
    public IDataStream<double> LastPrice => m_lastPrice;
    public IDataStream<long> LastVolume => m_lastVolume;
    public IDataStream<bool> Synthetic => m_synthetic;

    //methods
    protected void refreshDataCache()
    {
      DataCache dataCache = m_dataStore.GetInstrumentData(m_dataProvider.Name, Instrument.Id, Instrument.Ticker, Resolution, From, To, PriceDataType);
      IConfigurationService.TimeZone timeZone = (IConfigurationService.TimeZone)m_configuration.General[IConfigurationService.GeneralConfiguration.TimeZone];


      switch (Resolution)
      {
        case Resolution.Minute:
        case Resolution.Hour:
        case Resolution.Day:
        case Resolution.Week:
        case Resolution.Month:
          {
            BarData barData = (BarData)dataCache.Data;
            Count = (int)Math.Ceiling((double)barData.Count / Interval);
            if (Resolution == Resolution.Minute && (From.Minute % Interval != 0)) Count++;  //add an extra partial bar if the from date is not aligned to the interval
            m_dateTimeData = new DateTime[Count];
            m_openData = new double[Count];
            m_highData = new double[Count];
            m_lowData = new double[Count];
            m_closeData = new double[Count];
            m_volumeData = new long[Count];
            m_syntheticData = new bool[Count];

            int index = 0;
            int subBarIndex = 0;  //if interval is larger than 1 this would be the sub-bar index in the original bar data that needs to be merged into the current bar of the output data

            for (int barDataIndex = 0; barDataIndex < barData.Count; barDataIndex++)
            {
              //convert the bar data date time to the correct time zone based on the configuration
              DateTime barDataDateTime = barData.DateTime[barDataIndex];
              switch (timeZone)
              {
                case IConfigurationService.TimeZone.Local:
                  barDataDateTime = barDataDateTime.ToLocalTime();
                  break;
                case IConfigurationService.TimeZone.Exchange:
                  Exchange exchange = m_dataStore.GetExchange(Instrument.PrimaryExchangeId) ?? throw new ArgumentException($"Failed to find primary exchange for instrument {Instrument.Ticker} ({Instrument.Name})");
                  barDataDateTime = TimeZoneInfo.ConvertTimeFromUtc(barDataDateTime, exchange.TimeZone);
                  break;
              }

              //always setup the first bar from the data store, the first and last bars of the returned data may not be aligned with the interval for minutes/hours since
              //they can contain partial values based on the requested from/to dates
              if (subBarIndex == 0)
              {
                m_dateTimeData[index] = barDataDateTime;
                m_openData[index] = barData.Open[barDataIndex];
                m_highData[index] = barData.High[barDataIndex];
                m_lowData[index] = barData.Low[barDataIndex];
                m_volumeData[index] = barData.Volume[barDataIndex];
                m_syntheticData[index] = barData.Synthetic[barDataIndex];

                //handle edge case where bar resolution is minute and interval is larger than 1, we need to align the first bar to the interval date/time even if we do not have FULL data for it in the range
                // e.g. 9:38 as the first bar at interval 5 need to be aligned to 9:35 for the first bar
                if (barDataIndex == 0 && Resolution == Resolution.Minute && Interval > 1)
                {
                  subBarIndex = m_dateTimeData[index].Minute % Interval;
                  m_dateTimeData[index] = m_dateTimeData[index].AddMinutes(-subBarIndex);
                }
              }
              else
              {
                m_highData[index] = Math.Max(barData.High[barDataIndex], m_highData[index]);
                m_lowData[index] = Math.Min(barData.Low[barDataIndex], m_lowData[index]);
                m_volumeData[index] += barData.Volume[barDataIndex];
                m_syntheticData[index] |= barData.Synthetic[barDataIndex];
              }

              m_closeData[index] = barData.Close[barDataIndex];

              subBarIndex++;

              if (subBarIndex == Interval)
              {
                subBarIndex = 0;
                index++;
              }
            }

            m_dateTimeData = m_dateTimeData.Reverse().ToArray();
            m_openData = m_openData.Reverse().ToArray();
            m_highData = m_highData.Reverse().ToArray();
            m_lowData = m_lowData.Reverse().ToArray();
            m_closeData = m_closeData.Reverse().ToArray();
            m_volumeData = m_volumeData.Reverse().ToArray();
            m_syntheticData = m_syntheticData.Reverse().ToArray();

            m_dateTime.Data = m_dateTimeData;
            m_open.Data = m_openData;
            m_high.Data = m_highData;
            m_low.Data = m_lowData;
            m_close.Data = m_closeData;
            m_volume.Data = m_volumeData;
            m_lastPrice.Data = m_lastPriceData = m_closeData;
            m_lastVolume.Data = m_lastVolumeData = m_volumeData;
            m_synthetic.Data = m_syntheticData;

            m_bidPriceData = Array.Empty<double>();
            m_bidPrice.Data = m_bidPriceData;
            m_bidVolumeData = Array.Empty<long>();
            m_bidVolume.Data = m_bidVolumeData;
            m_askPriceData = Array.Empty<double>();
            m_askPrice.Data = m_askPriceData;
            m_askVolumeData = Array.Empty<long>();
            m_askVolume.Data = m_askVolumeData;
          }
          break;
        case Resolution.Level1:
          {
            Level1Data level1Data = (Level1Data)dataCache.Data;

            for (int i = 0; i < level1Data.Count; i++)
              switch (timeZone)
              {
                case IConfigurationService.TimeZone.Local:
                  level1Data.DateTime[i] = level1Data.DateTime[i].ToLocalTime();
                  break;
                case IConfigurationService.TimeZone.Exchange:
                  Exchange exchange = m_dataStore.GetExchange(Instrument.PrimaryExchangeId) ?? throw new ArgumentException($"Failed to find primary exchange for instrument {Instrument.Ticker} ({Instrument.Name})");
                  level1Data.DateTime[i] = TimeZoneInfo.ConvertTimeFromUtc(level1Data.DateTime[i], exchange.TimeZone);
                  break;
              }

            Count = (int)Math.Ceiling((double)level1Data.Count / Interval);

            if (Interval > 1)
            {
              m_dateTimeData = new DateTime[Count];
              m_openData = new double[Count];
              m_highData = new double[Count];
              m_lowData = new double[Count];
              m_closeData = new double[Count];
              m_volumeData = new long[Count];

              m_syntheticData = new bool[Count];

              int index = 0;
              int subBarIndex = 0;

              for (int level1DataIndex = 0; level1DataIndex < level1Data.Count; level1DataIndex++)
              {
                if (subBarIndex == 0)
                {
                  m_dateTimeData[index] = level1Data.DateTime[level1DataIndex];
                  m_openData[index] = level1Data.Last[level1DataIndex];
                  m_highData[index] = level1Data.Last[level1DataIndex];
                  m_lowData[index] = level1Data.Last[level1DataIndex];
                  m_volumeData[index] = level1Data.LastSize[level1DataIndex];
                  m_syntheticData[index] = level1Data.Synthetic[level1DataIndex];
                }
                else
                {
                  m_highData[index] = Math.Max(level1Data.Last[level1DataIndex], m_highData[index]);
                  m_lowData[index] = Math.Min(level1Data.Last[level1DataIndex], m_lowData[index]);
                  m_volumeData[index] += level1Data.LastSize[level1DataIndex];
                  m_syntheticData[index] |= level1Data.Synthetic[level1DataIndex];
                }

                m_closeData[index] = level1Data.Last[level1DataIndex];

                subBarIndex++;

                if (subBarIndex == Interval)
                {
                  subBarIndex = 0;
                  index++;
                }
              }

              m_dateTimeData = m_dateTimeData.Reverse().ToArray();
              m_openData = m_openData.Reverse().ToArray();
              m_highData = m_highData.Reverse().ToArray();
              m_lowData = m_lowData.Reverse().ToArray();
              m_closeData = m_closeData.Reverse().ToArray();
              m_volumeData = m_volumeData.Reverse().ToArray();
              m_syntheticData = m_syntheticData.Reverse().ToArray();

              m_dateTime.Data = m_dateTimeData;
              m_open.Data = m_openData;
              m_high.Data = m_highData;
              m_low.Data = m_lowData;
              m_close.Data = m_closeData;
              m_volume.Data = m_volumeData;
              m_lastPrice.Data = m_lastPriceData = m_closeData;
              m_lastVolume.Data = m_lastVolumeData = m_volumeData;
              m_synthetic.Data = m_syntheticData;
            }
            else
            {
              m_dateTimeData = level1Data.DateTime.Reverse().ToArray();
              m_openData = level1Data.Last.Reverse().ToArray();
              m_highData = level1Data.Last.Reverse().ToArray();
              m_lowData = level1Data.Last.Reverse().ToArray();
              m_closeData = level1Data.Last.Reverse().ToArray();
              m_volumeData = level1Data.LastSize.Reverse().ToArray();
              m_lastPriceData = level1Data.Last.Reverse().ToArray();
              m_lastVolumeData = level1Data.LastSize.Reverse().ToArray();
              m_syntheticData = level1Data.Synthetic.Reverse().ToArray();

              m_dateTime.Data = m_dateTimeData;
              m_open.Data = m_openData;
              m_high.Data = m_highData;
              m_low.Data = m_lowData;
              m_close.Data = m_closeData;
              m_volume.Data = m_volumeData;
              m_synthetic.Data = m_syntheticData;
              m_lastPrice.Data = m_lastPriceData;
              m_lastVolume.Data = m_lastVolumeData;
            }

            //TODO: How will we align these ticks with the bars IF the interval is greater than 1? You need to
            //      look at the date time of the current bar index and then find the first tick(s) that align with it or
            //      return a tick list.
            m_bidPriceData = level1Data.Bid.Reverse().ToArray();
            m_bidVolumeData = level1Data.BidSize.Reverse().ToArray();
            m_askPriceData = level1Data.Ask.Reverse().ToArray();
            m_askVolumeData = level1Data.AskSize.Reverse().ToArray();
            m_lastPriceData = level1Data.Last.Reverse().ToArray();
            m_lastVolumeData = level1Data.LastSize.Reverse().ToArray();

            m_bidPrice.Data = m_bidPriceData;
            m_bidVolume.Data = m_bidVolumeData;
            m_askPrice.Data = m_askPriceData;
            m_askVolume.Data = m_askVolumeData;
          }
          break;
        default:
          throw new NotImplementedException("Unknown resolution");
      }
    }

    /// <summary>
    /// Reset the data feed to the beginning.
    /// </summary>
    public void Reset()
    {
      CurrentBar = 0;
      m_dateTime.CurrentBar = CurrentBar;
      m_open.CurrentBar = CurrentBar;
      m_high.CurrentBar = CurrentBar;
      m_low.CurrentBar = CurrentBar;
      m_close.CurrentBar = CurrentBar;
      m_volume.CurrentBar = CurrentBar;
      m_synthetic.CurrentBar = CurrentBar;
      m_bidPrice.CurrentBar = CurrentBar;
      m_bidVolume.CurrentBar = CurrentBar;
      m_askPrice.CurrentBar = CurrentBar;
      m_askVolume.CurrentBar = CurrentBar;
    }

    /// <summary>
    /// Returns whether there is a next bar to iterate over.
    /// </summary>
    public bool HasNext()
    {
      return CurrentBar < Count;
    }

    /// <summary>
    /// Moves the data feed one bar forward. and returns whether the move was successful.
    /// </summary>
    public bool Next()
    {
      if (CurrentBar < Count)
      {
        CurrentBar++;
        m_dateTime.CurrentBar = CurrentBar;
        m_open.CurrentBar = CurrentBar;
        m_high.CurrentBar = CurrentBar;
        m_low.CurrentBar = CurrentBar;
        m_close.CurrentBar = CurrentBar;
        m_volume.CurrentBar = CurrentBar;
        m_synthetic.CurrentBar = CurrentBar;
        m_bidPrice.CurrentBar = CurrentBar;
        m_bidVolume.CurrentBar = CurrentBar;
        m_askPrice.CurrentBar = CurrentBar;
        m_askVolume.CurrentBar = CurrentBar;
        return true;
      }

      return false;
    }
  }
}
