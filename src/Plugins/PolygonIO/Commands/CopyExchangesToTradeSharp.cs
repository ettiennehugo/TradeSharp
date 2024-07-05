using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
	/// <summary>
	/// Copy cached Polygon exchange defintions to TradeSharp exchange definitions.
	/// </summary>
  public class CopyExchangesToTradeSharp
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
		public CopyExchangesToTradeSharp(IDialogService dialogService, IExchangeService exchangeService, IDatabase database, Cache cache)
    {
			m_cache = cache;
			m_dialogService = dialogService;
			m_exchangeService = exchangeService;
			m_database = database;
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
