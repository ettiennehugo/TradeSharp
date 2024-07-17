using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
  public class CountriesForExchanges
  {
		//constants


		//enums


		//types


		//attributes
		protected ILogger m_logger;
		protected IDialogService m_dialogService;
		protected ICountryService m_countryService;
		protected IExchangeService m_exchangeService;
		protected IDatabase m_database;
		protected Cache m_cache;

		//properties


		//constructors
		public CountriesForExchanges(ILogger logger, IDialogService dialogService, ICountryService countryService, IExchangeService exchangeService, IDatabase database, Cache cache) 
		{
			m_logger = logger;
			m_dialogService = dialogService;
			m_countryService = countryService;
			m_exchangeService = exchangeService;
			m_database = database;
			m_cache = cache;
		}


		//finalizers


		//interface implementations


		//methods
		public async Task Run()
		{
      bool updateService = false;
      var progressDialog = m_dialogService.CreateProgressDialog("Updating Countries", m_logger);
      progressDialog.StatusMessage = "Updating  Countries required for Exchanges";
      await progressDialog.ShowAsync();

      var exchanges = m_cache.GetExchanges();
      progressDialog.Maximum = exchanges.Count;
			foreach (Exchange pioExchange in exchanges)
			{
				if (pioExchange.IsTradeSharpSupported())
				{
					if (!pioExchange.Locale.Equals(Constants.LocaleGlobal, StringComparison.OrdinalIgnoreCase))
					{
            try
            {
              var country = m_countryService.Items.FirstOrDefault((c) => c.IsoCode != CountryInfo.InternationalsoCode && c.CountryInfo.RegionInfo.TwoLetterISORegionName == pioExchange.Locale.ToUpper());
              if (country == null)
              {
                var countryInfo = Common.CountryInfo.FromTwoLetterIso(pioExchange.Locale);
                if (countryInfo == null)
                {
                  if (pioExchange.Locale != Constants.LocaleGlobal) progressDialog.LogWarning($"Country associated to locale {pioExchange.Locale} for exchange {pioExchange.Name} not defined, skipping definition.");
                }
                else
                {
                  country = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, countryInfo.RegionInfo.TwoLetterISORegionName, countryInfo.CultureInfo.Name);
                  progressDialog.LogInformation($"Creating country {country.CountryInfo.RegionInfo.Name} - {country.CountryInfo.RegionInfo.ThreeLetterISORegionName}");
                  m_countryService.Add(country);
                  updateService = true;
                }
              }
            }
            catch (Exception ex)
            {
              progressDialog.LogError($"Failed to create country for exchange {pioExchange.Name} - {ex.Message}");
            }
          }
        }
        else if (pioExchange.Locale != Constants.LocaleGlobal)
          progressDialog.LogWarning($"{pioExchange.Name} (Asset Class - {pioExchange.AssetClass}) is not supported by TradeSharp");

        progressDialog.Progress++;
        if (progressDialog.CancellationTokenSource.IsCancellationRequested) break;
      }

      if (updateService)
      {
        progressDialog.LogInformation("Updating country service");
        m_countryService.Refresh();
      }

      progressDialog.Complete = true;
    }
  }
}
