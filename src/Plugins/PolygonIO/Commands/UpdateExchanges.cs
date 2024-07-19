using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using System.Text.Json;
using TradeSharp.Common;
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
				if (pioExchange.IsTradeSharpSupported())
				{
          try
          {
            Data.Exchange? exchange = m_exchangeService.Items.FirstOrDefault(e => (e.Id != Data.Exchange.InternationalId) && (e.Name == pioExchange.Name || e.Name == pioExchange.Acronym || e.TagStr.Contains(pioExchange.Name)));

						//try to parse the exchange from any tag data related to the Polygon plugin
						if (exchange == null)
						{
							foreach (var definedExchange in m_exchangeService.Items)
              {
                if (!string.IsNullOrEmpty(definedExchange.TagStr))
                {
									var polygonEntries = definedExchange.Tag.GetEntries(Constants.TagDataId, Constants.TagDataVersionMajor);
									foreach (var polygonEntry in polygonEntries)
									{
										try
										{
											var metaData = JsonSerializer.Deserialize<Exchange>(polygonEntry.Value);											
											if (metaData != null)
											{
                        if (pioExchange.Mic == metaData.Mic && pioExchange.OperatingMic == metaData.OperatingMic)
                        {
													exchange = definedExchange;
                          break;
                        }
                      }
											else
												progressDialog.LogWarning($"Failed to parse Polygon exchange data for {definedExchange.Name} (TagEntryVersion: {polygonEntry.Version.Major}.{polygonEntry.Version.Minor}.{polygonEntry.Version.Patch})");
                    }
										catch (Exception ex)
                    {
                      progressDialog.LogError($"Failed to parse Polygon exchange data for {definedExchange.Name} - {ex.Message} (TagEntryVersion: {polygonEntry.Version.Major}.{polygonEntry.Version.Minor}.{polygonEntry.Version.Patch})");
                    }
									}
                }
              }
						}

						//create the exchange if nothing was found
            if (exchange == null)
            {
							Country? country = m_countryService.Items.FirstOrDefault(c => c.IsoCode != Data.Country.InternationalIsoCode && c.CountryInfo.RegionInfo.TwoLetterISORegionName == pioExchange.Locale.ToUpper());
							Guid countryId = country?.Id ?? Country.InternationalId;
              exchange = new Data.Exchange(Guid.NewGuid(), Data.Exchange.DefaultAttributeSet, JsonSerializer.Serialize(pioExchange), countryId, pioExchange.Name, TimeZoneInfo.Utc, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Data.Exchange.BlankLogoId, string.Empty);
							exchange.Url = pioExchange.Url;
              exchange.Tag.Update(Constants.TagDataId, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch, JsonSerializer.Serialize(pioExchange));
              m_exchangeService.Add(exchange);
              updateService = true;
            }
						else if (exchange.Id != Data.Exchange.InternationalId && !exchange.Url.Equals(pioExchange.Url))	//ignore global exchange and check if the URL has changed
						{
							//update the defined exchange with data from Polygon
							exchange.Url = pioExchange.Url;
              exchange.Tag.Update(Constants.TagDataId, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch, JsonSerializer.Serialize(pioExchange));
              m_exchangeService.Update(exchange);
							updateService = true;
						}
          }
          catch (Exception ex)
          {
            m_logger.LogError($"Failed to update exchange {pioExchange.Name} - {ex.Message}");
          }
        }
				else
					progressDialog.LogWarning($"{pioExchange.Name} (Asset Class - {pioExchange.AssetClass}) is not supported by TradeSharp");

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
