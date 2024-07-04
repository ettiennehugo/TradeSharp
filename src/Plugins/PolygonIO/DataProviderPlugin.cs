using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

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
    protected IInstrumentService m_instrumentService;
    protected Client m_client;

    //properties
    public override int ConnectionCountMax { get => 1; }

    //constructors
    public DataProviderPlugin() : base(Constants.Name, Constants.Description) { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      IsConnected = true;   //uses a REST API so we're always connected when there is a network available
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_database = (IDatabase)ServiceHost.Services.GetService(typeof(IDatabase))!;
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

      m_client = Client.GetInstance(logger, m_apiKey, m_requestLimit);
   }

    public override bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end)
    {

      //TODO
      throw new NotImplementedException();

    }

    //methods


  }
}
