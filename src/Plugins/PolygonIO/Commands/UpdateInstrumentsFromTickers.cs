using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
	/// <summary>
	/// Update the TradeSharp instrument definitions from the cached Polygon ticker definitions.
	/// </summary>
  public class UpdateInstrumentsFromTickers
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;
    protected Cache m_cache;
    protected IInstrumentService m_instrumentService;
    protected IDatabase m_database;

    //properties


    //constructors
    public UpdateInstrumentsFromTickers(IDialogService dialogService, IInstrumentService instrumentService, IDatabase database, Cache cache)
    {
      m_instrumentService = instrumentService;
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
