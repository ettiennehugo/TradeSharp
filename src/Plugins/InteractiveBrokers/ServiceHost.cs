using Microsoft.Extensions.Hosting;
using TradeSharp.Common;
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

    //constructors
    public static ServiceHost GetInstance(IHost host, IDialogService dialogService, IPluginConfiguration configuration)
    {
      if (s_instance == null) s_instance = new ServiceHost(host, dialogService, configuration);
      return s_instance;
    }

    protected ServiceHost(IHost host, IDialogService dialogService, IPluginConfiguration configuration)
    {
      //allocate components
      Configuration = configuration;
      DialogService = dialogService;
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


    //properties
    public IPluginConfiguration Configuration { get; protected set; }
    public IDialogService DialogService { get; protected set; }
    public IHost Host { get; protected set; }
    public InteractiveBrokers.BrokerPlugin BrokerPlugin { get; set; }                //should only be called by the broker when it is created
    public InteractiveBrokers.DataProviderPlugin DataProviderPlugin { get; set; }    //should only be called by the data provider when it is created
    public Client Client { get; protected set; }
    public Cache Cache { get; protected set; }
    public AccountAdapter Accounts { get; protected set; }
    public InstrumentAdapter Instruments { get; protected set; }


    //TODO define the set of 



    //methods



  }
}
