using System.Collections;
using System.Net.NetworkInformation;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TradeSharp.PolygonIO.Commands;
using TradeSharp.PolygonIO.Messages;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace TradeSharp.PolygonIO
{
  /// <summary>
  /// Dataprovider plugin for Polygon.io - acts as an adapter between the Client class and the TradeSharp plugin service.
  /// https://polygon.io/docs/stocks/getting-started
  /// </summary>
  [ComVisible(true)]
  [Guid("78B9401F-69FC-46E0-ACB1-C6FDAF6193E4")]
  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin
  {
    //constants
    public const int DefaultLimit = 50000;

    //enums


    //types
    /// <summary>
    /// Data stored per subscription to instrument and resolution.
    /// </summary>
    protected struct SubscriptionEntry
    {
      public SubscriptionEntry(Instrument instrument, Resolution resolution, BarData barData, CancellationTokenSource cancellationTokenSource)
      {
        Instrument = instrument;
        Resolution = resolution;
        BarData = barData;
        CancellationTokenSource = cancellationTokenSource;
      }

      public Instrument Instrument;
      public Resolution Resolution;
      public BarData BarData;
      public CancellationTokenSource CancellationTokenSource;
    }

    //attributes
    protected string m_apiKey;
    protected int m_requestLimit;
    protected IDialogService m_dialogService;
    protected IDatabase m_database;
    protected ICountryService m_countryService;
    protected IExchangeService m_exchangeService;
    protected ISessionService m_sessionService;
    protected IInstrumentService m_instrumentService;
    protected Cache m_cache;
    protected Client m_client;
    protected Dictionary<int, SubscriptionEntry> m_subscriptions;

    //properties
    public override int ConnectionCountMax { get => Environment.ProcessorCount * 2; }   //PolygonIO does have a rate limit of 100 calls per second this should never reach that limit. 
    public override bool IsConnected { get => NetworkInterface.GetIsNetworkAvailable(); }

    //constructors
    public DataProviderPlugin() : base(Constants.Name, Constants.Description)
    {
      m_subscriptions = new Dictionary<int, SubscriptionEntry>();
    }

    //finalizers
    ~DataProviderPlugin()
    {
      m_client.BarDataM1Handler -= handleBarDataM1;
      m_client.BarDataS1Handler -= handleBarDataS1;
      m_client.StockTradesHandler -= handleTrade;
    }

    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_database = (IDatabase)ServiceHost.Services.GetService(typeof(IDatabase))!;
      m_countryService = (ICountryService)ServiceHost.Services.GetService(typeof(ICountryService))!;
      m_exchangeService = (IExchangeService)ServiceHost.Services.GetService(typeof(IExchangeService))!;
      m_sessionService = (ISessionService)ServiceHost.Services.GetService(typeof(ISessionService))!;
      m_instrumentService = (IInstrumentService)ServiceHost.Services.GetService(typeof(IInstrumentService))!;
      m_apiKey = (string)Configuration.Configuration[Constants.ConfigApiKey];

      try
      {
        m_requestLimit = (int)Configuration.Configuration[Constants.ConfigRequestLimit];
      }
      catch (Exception)
      {
        m_requestLimit = DefaultLimit;
      }

      m_cache = Cache.GetInstance(logger, Configuration);

      m_client = Client.GetInstance(logger, m_apiKey, m_requestLimit);
      m_client.BarDataM1Handler += handleBarDataM1;
      m_client.BarDataS1Handler += handleBarDataS1;
      m_client.StockTradesHandler += handleTrade;

      Commands.Add(new PluginCommand { Name = "Download Exchanges", Tooltip = "Download Polygon Exhange definitions to local cache", Icon = "\uE896", Command = new AsyncRelayCommand(OnDownloadExchangesAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = "Download Tickers", Tooltip = "Download Polygon Tickers to local cache", Icon = "\uE78C", Command = new AsyncRelayCommand(OnDownloadTickersAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = "Download Ticker Details", Tooltip = "Download Polygon Ticker Details to local cache", Icon = "\uE826", Command = new AsyncRelayCommand(OnDownloadTickerDetailsAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      Commands.Add(new PluginCommand { Name = "Define Countries", Tooltip = "Define countries required for Exchanges", Icon = "\uF49A", Command = new AsyncRelayCommand(OnDefineCountriesForExchangesAsync) });
      Commands.Add(new PluginCommand { Name = "Copy Exchanges", Tooltip = "Copy Exchanges from local cache to TradeSharp Exchanges", Icon = "\uF22C", Command = new AsyncRelayCommand(OnCopyExchangesToTradeSharpAsync) });
      Commands.Add(new PluginCommand { Name = "Copy Tickers", Tooltip = "Copy Tickers from local cache to TradeSharp Instruments", Icon = "\uE8C8", Command = new AsyncRelayCommand(OnUpdateInstrumentsFromTickersAsync) });
    }

    public override bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end)
    {
      bool result = false;
      int count = 0;
      var data = m_client.GetHistoricalData(instrument.Ticker, resolution, start, end, null).Result;
      if (data != null)
      {
        IList<IBarData> bars = new List<IBarData>();
        foreach (var barDto in data)
        {
          var bar = new BarData(resolution, barDto.DateTime, Common.Constants.DefaultPriceFormatMask, barDto.Open, barDto.High, barDto.Low, barDto.Close, barDto.Volume);
          bars.Add(bar);
        }
        m_database.UpdateData(this.Name, instrument.Ticker, resolution, bars);
        count = data.Count;
        result = true;
        //need to handle edge case where data response is value but no bars are returned
        if (bars.Count > 0) raiseDataDownloadComplete(instrument, resolution, count, bars[0].DateTime, bars[bars.Count - 1].DateTime);
      }
      else
        raiseRequestError(instrument, resolution, "Failed to get historical data for " + instrument.Ticker);

      return result;
    }

    private bool initSubscriptionBarData(SubscriptionEntry subscription)
    {
      IList<BarDataDto>? previousBars = null;
      var endDateTime = DateTime.Now;
      double open = -1.0;
      double high = -1.0;
      double low = -1.0;
      double close = -1.0;
      double volume = -1.0;
      bool recordBars = false;

      switch (subscription.Resolution)
      {
        case Resolution.Level1:
          //nothing to do as user want's the trade/tick data
          break;
        case Resolution.Seconds:
          previousBars = m_client.GetHistoricalData(subscription.Instrument.Ticker, Resolution.Seconds, DateTime.Now.AddMinutes(-1), endDateTime, null).Result;
          if (previousBars == null) return false;

          foreach (var bar in previousBars)
          {
            if (bar.DateTime == endDateTime.AddSeconds(-1))
            {
              open = bar.Open;
              high = bar.High;
              low = bar.Low;
              close = bar.Close;
              volume = bar.Volume;
              break;
            }
          }
          break;
        case Resolution.Minutes:
          previousBars = m_client.GetHistoricalData(subscription.Instrument.Ticker, Resolution.Seconds, DateTime.Now.AddMinutes(-1), endDateTime, null).Result;
          if (previousBars == null) return false;

          foreach (var bar in previousBars)
          {
            if (bar.DateTime.Minute == endDateTime.Minute)
            {
              if (open == -1.0) open = bar.Open;
              if (high == -1.0 || bar.High > high) high = bar.High;
              if (low == -1.0 || bar.Low < low) low = bar.Low;
              close = bar.Close;
              volume += bar.Volume;
            }
          }
          break;
        case Resolution.Hours:
          previousBars = m_client.GetHistoricalData(subscription.Instrument.Ticker, Resolution.Minutes, DateTime.Now.AddHours(-1), endDateTime, null).Result;
          if (previousBars == null) return false;

          foreach (var bar in previousBars)
          {
            if (bar.DateTime.Hour == endDateTime.Hour)
            {
              if (open == -1.0) open = bar.Open;
              if (high == -1.0 || bar.High > high) high = bar.High;
              if (low == -1.0 || bar.Low < low) low = bar.Low;
              close = bar.Close;
              volume += bar.Volume;
            }
          }
          break;
        case Resolution.Days:
          previousBars = m_client.GetHistoricalData(subscription.Instrument.Ticker, Resolution.Hours, DateTime.Now.AddDays(-1), endDateTime, null).Result;
          if (previousBars == null) return false;

          foreach (var bar in previousBars)
          {
            if (bar.DateTime.DayOfWeek == endDateTime.DayOfWeek)
            {
              if (open == -1.0) open = bar.Open;
              if (high == -1.0 || bar.High > high) high = bar.High;
              if (low == -1.0 || bar.Low < low) low = bar.Low;
              close = bar.Close;
              volume += bar.Volume;
            }
          }
          break;
        case Resolution.Weeks:
          previousBars = m_client.GetHistoricalData(subscription.Instrument.Ticker, Resolution.Hours, DateTime.Now.AddDays(-7), endDateTime, null).Result;
          if (previousBars == null) return false;

          foreach (var bar in previousBars)
          {
            if (bar.DateTime.DayOfWeek == DayOfWeek.Sunday || bar.DateTime.DayOfWeek == DayOfWeek.Monday) recordBars = true;

            if (recordBars)
            {
              if (open == -1.0) open = bar.Open;
              if (high == -1.0 || bar.High > high) high = bar.High;
              if (low == -1.0 || bar.Low < low) low = bar.Low;
              close = bar.Close;
              volume += bar.Volume;
            }
          }
          break;
        case Resolution.Months:
          previousBars = m_client.GetHistoricalData(subscription.Instrument.Ticker, Resolution.Hours, DateTime.Now.AddDays(-31), endDateTime, null).Result;
          if (previousBars == null) return false;

          foreach (var bar in previousBars)
          {
            if (bar.DateTime.Month == endDateTime.Month)
            {
              if (open == -1.0) open = bar.Open;
              if (high == -1.0 || bar.High > high) high = bar.High;
              if (low == -1.0 || bar.Low < low) low = bar.Low;
              close = bar.Close;
              volume += bar.Volume;
            }
          }
          break;
      }

      subscription.BarData = new BarData(subscription.Resolution, endDateTime, Common.Constants.DefaultPriceFormatMask, open, high, low, close, volume);
      return true;
    }

    private void updateSubsriptionBarData(SubscriptionEntry subscription, DateTime newDateTime, double open, double high, double low, double close, double volume)
    {
      switch (subscription.Resolution)
      {
        case Resolution.Seconds:
          if (subscription.BarData.DateTime.Second != newDateTime.Second)
          {
            subscription.BarData.DateTime = newDateTime;
            subscription.BarData.Open = open;
          }
          break;
        case Resolution.Minutes:
          if (subscription.BarData.DateTime.Minute != newDateTime.Minute)
          {
            subscription.BarData.DateTime = newDateTime;
            subscription.BarData.Open = open;
          }
          break;
        case Resolution.Hours:
          if (subscription.BarData.DateTime.Hour != newDateTime.Hour)
          {
            subscription.BarData.DateTime = newDateTime;
            subscription.BarData.Open = open;
          }
          break;
        case Resolution.Days:
          if (subscription.BarData.DateTime.Day != newDateTime.Day)
          {
            subscription.BarData.DateTime = newDateTime;
            subscription.BarData.Open = open;
          }
          break;
        case Resolution.Weeks:
          if (newDateTime.DayOfWeek == DayOfWeek.Sunday || newDateTime.DayOfWeek == DayOfWeek.Monday)
          {
            subscription.BarData.DateTime = newDateTime;
            subscription.BarData.Open = open;
          }
          break;
        case Resolution.Months:
          if (subscription.BarData.DateTime.Month != newDateTime.Month)
          {
            subscription.BarData.DateTime = newDateTime;
            subscription.BarData.Open = open;
          }
          break;
      }

      subscription.BarData.High = high > subscription.BarData.High ? high : subscription.BarData.High;
      subscription.BarData.Low = low < subscription.BarData.Low ? low : subscription.BarData.Low;
      subscription.BarData.Close = close;
    }

    public override bool Subscribe(Instrument instrument, Resolution resolution)
    {
      //check whether we have an existing subscription
      var key = instrument.Ticker.GetHashCode() + resolution.GetHashCode();
      if (m_subscriptions.ContainsKey(key)) return true;

      //create a new subscription entry for the instrument and resolution
      var subscription = new SubscriptionEntry(instrument, resolution, new BarData(), new CancellationTokenSource());

      //download the data required to construct a proper bar based on the requested resolution and set it up in the subscription entry
      var result = initSubscriptionBarData(subscription);

      //subscribe to the real-time data using the client
      Task.WaitAll(m_client.SubscribeToTrades(instrument.Ticker, true, subscription.CancellationTokenSource.Token));

      return result;
    }

    public override bool Unsubscribe(Instrument instrument, Resolution resolution)
    {
      //try to find subscription
      var key = instrument.Ticker.GetHashCode() + resolution.GetHashCode();
      if (m_subscriptions.ContainsKey(key))
      {
        m_subscriptions[key].CancellationTokenSource.Cancel();
        m_subscriptions.Remove(key);
        return true;
      }

      //could not find subscription
      return false;
    }


    //methods
    protected Task OnDownloadExchangesAsync()
    {
      return Task.Run(() =>
      {
        var command = new DownloadExchanges(m_logger, m_dialogService, m_exchangeService, m_database, m_client, m_cache);
        command.Run();
      });
    }

    protected Task OnDownloadTickersAsync()
    {
      return Task.Run(() =>
      {
        var command = new DownloadTickers(m_logger, m_dialogService, m_instrumentService, m_database, m_client, m_cache);
        command.Run();
      });
    }

    protected Task OnDownloadTickerDetailsAsync()
    {
      return Task.Run(() =>
      {
        var command = new DownloadTickerDetails(m_logger, m_dialogService, m_instrumentService, m_database, m_client, m_cache);
        command.Run();
      });
    }

    protected Task OnDefineCountriesForExchangesAsync()
    {
      return Task.Run(() =>
      {
        var command = new CountriesForExchanges(m_logger, m_dialogService, m_countryService, m_exchangeService, m_database, m_cache);
        command.Run();
      });
    }

    protected Task OnCopyExchangesToTradeSharpAsync()
    {
      return Task.Run(() =>
      {
        var command = new UpdateExchanges(m_logger, m_dialogService, m_countryService, m_exchangeService, m_sessionService, m_database, m_cache);
        command.Run();
      });
    }

    protected Task OnUpdateInstrumentsFromTickersAsync()
    {
      return Task.Run(() =>
      {
        var command = new UpdateInstrumentsFromTickers(m_logger, m_dialogService, m_exchangeService, m_instrumentService, m_database, m_cache);
        command.Run();
      });
    }

    public DateTime unixMillisecondsToUtc(long unixMilliseconds)
    {
      var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
      var easternZone = TimeZoneInfo.Utc;
      return TimeZoneInfo.ConvertTime(dateTimeOffset, easternZone).DateTime;
    }

    protected void handleBarDataM1(BarDataM1ResultDto data)
    {
      //merge in the latest bar data into the subscriptions
      List<SubscriptionEntry> updatedEntries = new List<SubscriptionEntry>();
      foreach (var bar in data.Data)
      {
        foreach (var entry in m_subscriptions)
          if (entry.Value.Instrument.Equals(bar.Symbol) && entry.Value.Resolution >= Resolution.Minutes)
          {
            updateSubsriptionBarData(entry.Value, bar.StartDateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.TickVolume);
            if (!updatedEntries.Contains(entry.Value)) updatedEntries.Add(entry.Value);
          }
      }

      //raise real-time data update event for all updated subscriptions
      foreach (var entry in updatedEntries)
      {
        IList<IBarData> barData = new List<IBarData> { entry.BarData };
        raiseRealTimeDataUpdate(entry.Instrument, entry.Resolution, barData, null);
      }
    }

    protected void handleBarDataS1(BarDataS1ResultDto data)
    {
      //merge in the latest bar data into the subscriptions
      List<SubscriptionEntry> updatedEntries = new List<SubscriptionEntry>();
      foreach (var bar in data.Data)
      {
        foreach (var entry in m_subscriptions)
          if (entry.Value.Instrument.Equals(bar.Symbol) && entry.Value.Resolution >= Resolution.Seconds)
          {
            updateSubsriptionBarData(entry.Value, bar.StartDateTime, (double)bar.Open, (double)bar.High, (double)bar.Low, (double)bar.Close, (double)bar.TickVolume);
            if (!updatedEntries.Contains(entry.Value)) updatedEntries.Add(entry.Value);
          }
      }

      //raise real-time data update event for all updated subscriptions
      foreach (var entry in updatedEntries)
      {
        IList<IBarData> barData = new List<IBarData> { entry.BarData };
        raiseRealTimeDataUpdate(entry.Instrument, entry.Resolution, barData, null);
      }
    }

    protected void handleTrade(StockTradesResultDto data)
    {
      //merge in the latest bar data into the subscriptions
      List<SubscriptionEntry> updatedEntries = new List<SubscriptionEntry>();
      foreach (var trade in data.Trades)
      {
        foreach (var entry in m_subscriptions)
          if (entry.Value.Instrument.Equals(trade.Symbol) && entry.Value.Resolution >= Resolution.Seconds)
          {
            //TBD: Below code converts the trade timestamp to UTC time - this might be incorrect.
            updateSubsriptionBarData(entry.Value, unixMillisecondsToUtc(trade.Timestamp), (double)trade.Price, (double)trade.Price, (double)trade.Price, (double)trade.Price, (double)trade.Size);
            if (!updatedEntries.Contains(entry.Value)) updatedEntries.Add(entry.Value);
          }
      }

      //raise real-time data update event for all updated subscriptions
      foreach (var entry in updatedEntries)
      {
        IList<IBarData> barData = new List<IBarData> { entry.BarData };
        raiseRealTimeDataUpdate(entry.Instrument, entry.Resolution, barData, null);
      }
    }


    // TBD: - this is used for order book quotes, only used once L2 data is supported.
    //protected void handleQuote(StockQuotesResultDto data) 
    //{

    //  //TODO - implement real-time bar data handling
    //  // - Update all resolutions for the instrument from seconds to months
    //  //   - Make sure to check that the bar is still in the scope of the subscription
    //  // - look up subscription entry and update bar data and raise real-time data update event
    //  throw new NotImplementedException();

    //}

  }
}
