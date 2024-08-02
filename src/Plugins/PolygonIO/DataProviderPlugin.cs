using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TradeSharp.PolygonIO.Commands;
using TradeSharp.PolygonIO.Messages;
using CommunityToolkit.Mvvm.Input;

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


    //properties
    public override int ConnectionCountMax { get => 1; }  

    //constructors
    public DataProviderPlugin() : base(Constants.Name, Constants.Description) { }

    //finalizers
    ~DataProviderPlugin()
    {
      m_client.BarDataM1Handler -= handleBarDataM1;
      m_client.BarDataS1Handler -= handleBarDataS1;
      m_client.StockTradesHandler -= handleTrade;
      m_client.StockQuotesHandler -= handleQuote;
    }

    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      IsConnected = true;   //uses a REST API so we're always connected when there is a network available
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
      m_client.StockQuotesHandler += handleQuote;

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
      Task.Run(async () => {
        var data = await m_client.GetHistoricalData(instrument.Ticker, resolution, start, end, null);
        if (data != null)
        { 
          IList<IBarData> bars = new List<IBarData>();
          foreach (var barDto in data)
          {
            var bar = new BarData(resolution, barDto.DateTime, Common.Constants.DefaultPriceFormatMask, barDto.Open, barDto.High, barDto.Low, barDto.Close, barDto.Volume);
            bars.Add(bar);
          }
          m_database.UpdateData(this.Name, instrument.Ticker, resolution, bars);
        }
      });
      return true;
    }

    public override bool Subscribe(Instrument instrument, Resolution resolution)
    {

      //TODO
      // - Resolve the next historical data for the requested definition
      //   - Do one download request for the historical data from the previous bar end to the current time in the current bar, e.g. 1 week data would request the daily data from the end of the
      //     last week to the current time in the current week
      //   - Create a subscription entry for the instrument, resolution and historical bar data retrieved
      // - Subscribe to the trade updates
      // - For each new trade coming in
      //   - get the subscription entry
      //   - merge in the new trade data (open, high, low, close)
      //   - raise the real-time update event for the specific entry
      
      throw new NotImplementedException();

    }

    //methods
    protected Task OnDownloadExchangesAsync()
    {
      return Task.Run(() => {
        var command = new DownloadExchanges(m_logger, m_dialogService, m_exchangeService, m_database, m_client, m_cache);
        command.Run();
      });
    }

    protected Task OnDownloadTickersAsync()
    {
      return Task.Run(() => {
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
      return Task.Run(() => {
        var command = new CountriesForExchanges(m_logger, m_dialogService, m_countryService, m_exchangeService, m_database, m_cache);
        command.Run();
      });
    }

    protected Task OnCopyExchangesToTradeSharpAsync()
    {
      return Task.Run(() => {
        var command = new UpdateExchanges(m_logger, m_dialogService, m_countryService, m_exchangeService, m_sessionService, m_database, m_cache);
        command.Run();
      });
    }

    protected Task OnUpdateInstrumentsFromTickersAsync()
    {
      return Task.Run(() => {
        var command = new UpdateInstrumentsFromTickers(m_logger, m_dialogService, m_exchangeService, m_instrumentService, m_database, m_cache);
        command.Run();
      });
    }

    protected void handleBarDataM1(BarDataM1ResultDto data)
    {

      //TODO - implement real-time bar data handling
      throw new NotImplementedException();

    }

    protected void handleBarDataS1(BarDataS1ResultDto data)
    {

      //TODO - implement real-time bar data handling
      throw new NotImplementedException();

    }

    protected void handleTrade(StockTradesResultDto data)
    {

      //TODO - implement real-time bar data handling
      throw new NotImplementedException();

    }

    protected void handleQuote(StockQuotesResultDto data) 
    {

      //TODO - implement real-time bar data handling
      throw new NotImplementedException();

    }
  }
}
