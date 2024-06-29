using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Singleton class to host all the other services that are common to the Broker and DataProvider plugins.
  /// </summary>
  public class ServiceHost
  {
    //constants


    //enums


    //types


    //attributes
    private static ServiceHost? s_instance;

    //properties
    public ILogger Logger { get; protected set; }
    public IPluginConfiguration Configuration { get; protected set; }
    public IDialogService DialogService { get; protected set; }
    public IDatabase Database { get; protected set; }
    public IInstrumentService InstrumentService { get; protected set; }
    public IInstrumentGroupService InstrumentGroupService { get; protected set; }
    public IExchangeService ExchangeService { get; protected set; }
    public IHost Host { get; protected set; }
    public InteractiveBrokers.BrokerPlugin BrokerPlugin { get; set; }                //should only be called by the broker when it is created
    public Client Client { get; protected set; }
    public Cache Cache { get; protected set; }
    public AccountAdapter Accounts { get; protected set; }
    public InstrumentAdapter Instruments { get; protected set; }

    //constructors
    public static ServiceHost GetInstance(ILogger logger, IHost host, IDialogService dialogService, IDatabase database, IPluginConfiguration configuration)
    {
      if (s_instance == null) s_instance = new ServiceHost(logger, host, dialogService, database, configuration);
      return s_instance;
    }

    protected ServiceHost(ILogger logger, IHost host, IDialogService dialogService, IDatabase database, IPluginConfiguration configuration)
    {
      //allocate components
      Logger = logger; 
      Configuration = configuration;
      DialogService = dialogService;
      Database = database;
      InstrumentGroupService = (IInstrumentGroupService)host.Services.GetService(typeof(IInstrumentGroupService))!;
      InstrumentService = (IInstrumentService)host.Services.GetService(typeof(IInstrumentService))!;
      ExchangeService = (IExchangeService)host.Services.GetService(typeof(IExchangeService))!;
      Host = host;
      Client = Client.GetInstance(this);
      Cache = Cache.GetInstance(this);
      Accounts = AccountAdapter.GetInstance(this);
      Instruments = InstrumentAdapter.GetInstance(this);

      //setup callback handlers for the client to the adapters
      Client.ConnectionStatus += Accounts.HandleConnectionStatus;
      Client.AccountSummary += Accounts.HandleAccountSummary;
      Client.UpdateAccountValue += Accounts.HandleUpdateAccountValue;
      Client.UpdatePortfolio += Accounts.HandleUpdatePortfolio;
      Client.Position += Accounts.HandlePosition;
      Client.OrderStatus += Accounts.HandleOrderStatus;
      Client.OpenOrder += Accounts.HandleOpenOrder;

      Client.ScannerParameters += Instruments.HandleScannerParameters;
      Client.ContractDetails += Instruments.HandleContractDetails;
      Client.ContractDetailsEnd += Instruments.HandleContractDetailsEnd;
      Client.HistoricalData += Instruments.HandleHistoricalData;
      Client.HistoricalDataUpdate += Instruments.HandleHistoricalData;
      Client.HistoricalDataEnd += Instruments.HandleHistoricalDataEnd;
      Client.UpdateMktDepth += Instruments.HandleUpdateMktDepth;
      Client.RealTimeBar += Instruments.HandleRealTimeBar;
    }

    //finalizers


    //interface implementations


    //methods



  }
}
