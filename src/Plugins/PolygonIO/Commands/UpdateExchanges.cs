using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
	/// <summary>
	/// Update TradeSharp Exchanges from the cached Polygon exchange definitions.
	/// </summary>
  public class UpdateExchanges
  {
		//constants


		//enums


		//types


		//attributes
		protected ILogger m_logger;
		protected IDialogService m_dialogService;
		protected Cache m_cache;
    protected ICountryService m_countryService;
    protected IExchangeService m_exchangeService;
		protected IDatabase m_database;

		//properties


		//constructors
		public UpdateExchanges(ILogger logger, IDialogService dialogService, ICountryService countryService, IExchangeService exchangeService, IDatabase database, Cache cache)
    {
			m_logger = logger;
			m_cache = cache;
			m_dialogService = dialogService;
      m_countryService = countryService;
			m_exchangeService = exchangeService;
			m_database = database;
    }

    //finalizers


    //interface implementations
    public async Task Run()
    {
			bool updateService = false;
			var progressDialog = m_dialogService.CreateProgressDialog("Updating Exchanges", m_logger);
			progressDialog.StatusMessage = "Updating exchanges";
			await progressDialog.ShowAsync();

			var exchanges = m_cache.GetExchanges();
			progressDialog.Maximum = exchanges.Count;
			foreach (var pioExchange in exchanges)
			{
        try
        {
					Data.Exchange? exchange = m_exchangeService.Items.FirstOrDefault(e => e.Name == pioExchange.Name);

          if (exchange == null)
					{
            exchange = new Data.Exchange(Guid.NewGuid(), Data.Exchange.DefaultAttributeSet, pioExchange.Acronym, Country.InternationalId, pioExchange.Name, TimeZoneInfo.Utc, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Data.Exchange.InternationalId);
            m_exchangeService.Update(exchange);
						updateService = true;
          }
        }
        catch (Exception ex)
        {
          m_logger.LogError($"Failed to update exchange {pioExchange.Name} - {ex.Message}");
        }

				progressDialog.Progress++;
				if (progressDialog.CancellationTokenSource.IsCancellationRequested) break;
      }


			if (updateService)
			{
				progressDialog.LogInformation("Updating exchange service");
				m_exchangeService.Refresh();
			}

      progressDialog.Complete = true;
    }

    //methods


  }
}
