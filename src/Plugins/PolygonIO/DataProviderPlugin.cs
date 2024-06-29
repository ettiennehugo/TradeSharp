using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace TradeSharp.PolygonIO
{
  /// <summary>
  /// Dataprovider plugin for Polygon.io.
  /// https://polygon.io/docs/stocks/getting-started
  /// </summary>
  [ComVisible(true)]
  [Guid("78B9401F-69FC-46E0-ACB1-C6FDAF6193E4")]
  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected string m_apiBaseUrl;
    protected string m_apiVersion;
    protected string m_apiKey;
    protected IDialogService m_dialogService;
    protected IDatabase m_database;

    //properties


    //constructors
    public DataProviderPlugin(): base(Constants.Name, Constants.Description)  { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      IsConnected = true;   //uses a REST API so we're always connected when there is a network available
      m_apiBaseUrl = (string)Configuration.Configuration[Constants.ConfigApiBaseUrl];
      m_apiKey = (string)Configuration.Configuration[Constants.ConfigApiKey];
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_database = (IDatabase)ServiceHost.Services.GetService(typeof(IDatabase))!;
    }

    public override bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end)
    {
      throw new NotImplementedException();
    }

    //methods


  }
}
