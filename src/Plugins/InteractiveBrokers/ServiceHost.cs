using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;

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
    public static ServiceHost GetInstance(IHost host, IPluginConfiguration configuration)
    {
      if (s_instance == null) s_instance = new ServiceHost(host, configuration);
      return s_instance;
    }

    protected ServiceHost(IHost host, IPluginConfiguration configuration)
    {
      //allocate components
      Configuration = configuration;
      Host = host;
      Client = Client.GetInstance(this);
      Cache = Cache.GetInstance(this);
      Accounts = AccountAdapter.GetInstance(this);
      Instruments = InstrumentAdapter.GetInstance(this);

      //setup callback handlers for the client to the adapters
      Client.AccountSummary += Accounts.HandleAccountSummary;
      Client.AccountSummaryEnd += Accounts.HandleAccountSummaryEnd;
      Client.UpdateAccountValue += Accounts.HandleUpdateAccountValue;
      Client.UpdatePortfolio += Accounts.HandleUpdatePortfolio;
      Client.Position += Accounts.HandlePosition;
      Client.PositionEnd += Accounts.HandlePositionEnd;
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
    public IHost Host { get; protected set; }
    public Client Client { get; protected set; }
    public Cache Cache { get; protected set; }
    public AccountAdapter Accounts { get; protected set; }
    public InstrumentAdapter Instruments { get; protected set; }

    //TODO define the set of 



    //methods



  }
}
