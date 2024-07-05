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
    protected IExchangeService m_exchangeService;
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
      m_exchangeService = (IExchangeService)ServiceHost.Services.GetService(typeof(IExchangeService))!;
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
      Commands.Add(new PluginCommand { Name = "Download Tickers", Tooltip = "Download Polygon Ticker definitions to local cache", Icon = "\uE826", Command = new AsyncRelayCommand(OnDownloadTickersAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      Commands.Add(new PluginCommand { Name = "Copy Exchanges", Tooltip = "Copy Exchanges from local cache to TradeSharp Exchagnes", Icon = "\uF22C", Command = new AsyncRelayCommand(OnCopyExchangesToTradeSharpAsync) });
      Commands.Add(new PluginCommand { Name = "Copy Tickers", Tooltip = "Copy Tickers from local cache to TradeSharp Instruments", Icon = "\uE8C8", Command = new AsyncRelayCommand(OnUpdateInstrumentsFromTickersAsync) });
    }

    public override bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end)
    {

      //TODO
      throw new NotImplementedException();

    }

    //methods
    protected async Task OnDownloadExchangesAsync()
    {
      var command = new DownloadExchanges(m_dialogService, m_exchangeService, m_database, m_cache);
      await command.Run();
    }

    protected async Task OnDownloadTickersAsync()
    {
      var command = new DownloadTickers(m_dialogService, m_instrumentService, m_database, m_cache);
      await command.Run();
    }

    protected async Task OnCopyExchangesToTradeSharpAsync()
    {
      var command = new CopyExchangesToTradeSharp(m_dialogService, m_exchangeService, m_database, m_cache);
      await command.Run();
    }

    protected async Task OnUpdateInstrumentsFromTickersAsync()
    {
      var command = new UpdateInstrumentsFromTickers(m_dialogService, m_instrumentService, m_database, m_cache);
      await command.Run();
    }

    protected void handleBarDataM1(BarDataM1ResultDto data)
    {

      //TODO
      throw new NotImplementedException();

    }

    protected void handleBarDataS1(BarDataS1ResultDto data)
    {

      //TODO
      throw new NotImplementedException();

    }

    protected void handleTrade(StockTradesResultDto data)
    {

      //TODO
      throw new NotImplementedException();

    }

    protected void handleQuote(StockQuotesResultDto data) 
    {

      //TODO
      throw new NotImplementedException();

    }
  }
}
