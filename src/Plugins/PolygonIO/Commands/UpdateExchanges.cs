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
		protected ISessionService m_sessionService;
		protected IDatabase m_database;

		//properties


		//constructors
		public UpdateExchanges(ILogger logger, IDialogService dialogService, ICountryService countryService, IExchangeService exchangeService, ISessionService sessionService, IDatabase database, Cache cache)
    {
			m_logger = logger;
			m_cache = cache;
			m_dialogService = dialogService;
      m_countryService = countryService;
			m_exchangeService = exchangeService;
			m_sessionService = sessionService;
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
                if (definedExchange.HasTagData)
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
              exchange = new Data.Exchange(Guid.NewGuid(), Data.Exchange.DefaultAttributes, string.Empty, countryId, pioExchange.Name, new List<string> { pioExchange.Acronym }, TimeZoneInfo.Utc, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Data.Exchange.BlankLogoId, string.Empty);
							exchange.Url = pioExchange.Url;
              exchange.Tag.Update(Constants.TagDataId, DateTime.UtcNow, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch, JsonSerializer.Serialize(pioExchange));
              m_exchangeService.Add(exchange);
							if (pioExchange.AssetClass == Constants.AssetClassStock) defineStockSessions(exchange);
							if (pioExchange.AssetClass == Constants.AssetClassForex) defineForexSessions(exchange);
              updateService = true;
            }
						else if (exchange.Id != Data.Exchange.InternationalId && !exchange.Url.Equals(pioExchange.Url))	//ignore global exchange and check if the URL has changed
						{
							//update the defined exchange with data from Polygon
							exchange.Url = pioExchange.Url;
              exchange.Tag.Update(Constants.TagDataId, DateTime.UtcNow, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch, JsonSerializer.Serialize(pioExchange));
              m_exchangeService.Update(exchange);
							//TBD: Should we update sessions.
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
				m_sessionService.Refresh();
			}

      progressDialog.Complete = true;
    }

    //methods
		private void defineStockSessions(Data.Exchange exchange)
		{
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Monday Pre-market", exchange.Id, DayOfWeek.Monday, new TimeOnly(4, 0), new TimeOnly(9, 29)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Monday Market", exchange.Id, DayOfWeek.Monday, new TimeOnly(9, 30), new TimeOnly(15, 59)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Monday Post-market", exchange.Id, DayOfWeek.Monday, new TimeOnly(16, 0), new TimeOnly(20, 0)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Tuesday Pre-market", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(4, 0), new TimeOnly(9, 29)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Tuesday Market", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(9, 30), new TimeOnly(15, 59)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Tuesday Post-market", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(16, 0), new TimeOnly(20, 0)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Wednesday Pre-market", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(4, 0), new TimeOnly(9, 29)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Wednesday Market", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(9, 30), new TimeOnly(15, 59)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Wednesday Post-market", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(16, 0), new TimeOnly(20, 0)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Thrusday Pre-market", exchange.Id, DayOfWeek.Thursday, new TimeOnly(4, 0), new TimeOnly(9, 29)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Thrusday Market", exchange.Id, DayOfWeek.Thursday, new TimeOnly(9, 30), new TimeOnly(15, 59)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Thrusday Post-market", exchange.Id, DayOfWeek.Thursday, new TimeOnly(16, 0), new TimeOnly(20, 0)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Friday Pre-market", exchange.Id, DayOfWeek.Friday, new TimeOnly(4, 0), new TimeOnly(9, 29)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Friday Market", exchange.Id, DayOfWeek.Friday, new TimeOnly(9, 30), new TimeOnly(15, 59)));
      m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Friday Post-market", exchange.Id, DayOfWeek.Friday, new TimeOnly(16, 0), new TimeOnly(20, 0)));
    }

		private void defineForexSessions(Data.Exchange exchange)
		{
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Monday", exchange.Id, DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Tuesday", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Wednesday", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Thrusday", exchange.Id, DayOfWeek.Thursday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Friday", exchange.Id, DayOfWeek.Friday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Saturday", exchange.Id, DayOfWeek.Saturday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
			m_sessionService.Add(new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, "Sunday", exchange.Id, DayOfWeek.Sunday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
		}
  }
}
