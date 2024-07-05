using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
	/// <summary>
	/// Download Polygon exchange definitions to the local cache.
	/// </summary>
  public class DownloadExchanges
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;
    protected Cache m_cache;
    protected IExchangeService m_exchangeService;
    protected IDatabase m_database;

    //properties


    //constructors
    public DownloadExchanges(IDialogService dialogService, IExchangeService exchangeService, IDatabase database, Cache cache)
    {
      m_exchangeService = exchangeService;
      m_dialogService = dialogService;
      m_database = database;
      m_cache = cache;
    }

    //finalizers


    //interface implementations
    public async Task Run()
    {


      //TODO
      throw new NotImplementedException();



    }

    //methods



  }
}
