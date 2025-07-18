﻿using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
  /// <summary>
  /// Payload for the Polygon IO instrument metadata.
  /// </summary>
  public class InstrumentMetaDataV1
  {
    public Tickers Ticker { get; set; } = null;
    public TickerDetails Details { get; set; } = null;
  }

	/// <summary>
	/// Update the TradeSharp instrument definitions from the cached Polygon ticker definitions.
	/// </summary>
  public class UpdateInstrumentsFromTickers
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger m_logger;
    protected IDialogService m_dialogService;
    protected Cache m_cache;
    protected IExchangeService m_exchangeService;
    protected IInstrumentService m_instrumentService;
    protected IDatabase m_database;

    //properties


    //constructors
    public UpdateInstrumentsFromTickers(ILogger logger, IDialogService dialogService, IExchangeService exchangeService, IInstrumentService instrumentService, IDatabase database, Cache cache)
    {
      m_logger = logger;
      m_exchangeService = exchangeService;
      m_instrumentService = instrumentService;
      m_dialogService = dialogService;
      m_database = database;
      m_cache = cache;
    }

    //finalizers


    //interface implementations
    public void Run()
    {
      bool updateService = false;
      var progressDialog = m_dialogService.CreateProgressDialog("Updating Instruments", m_logger);
      progressDialog.StatusMessage = "Updating instruments";
      progressDialog.ShowAsync();

      var tickers = m_cache.GetTickers();
      var tickerDetails = m_cache.GetTickerDetails();
      progressDialog.Maximum = tickers.Count;

      foreach (var pioTicker in tickers)
      {
        if (pioTicker.IsTradeSharpSupported())
        {
          try
          {
            var pioTickerDetails = tickerDetails.FirstOrDefault(td => td.Ticker == pioTicker.Ticker);
            var instrument = m_instrumentService.Cache.Items.FirstOrDefault(i => i.Ticker == pioTicker.Ticker || i.AlternateTickers.Contains(pioTicker.Ticker));

            //find the exchange associated with the Instrument
            Guid exchangeId = Data.Exchange.InternationalId;
            var exchange = m_exchangeService.Items.FirstOrDefault(e => e.Name == pioTicker.PrimaryExchange || e.TagStr.Contains(pioTicker.PrimaryExchange));
            if (exchange != null) exchangeId = exchange.Id;

            //create update the instrument
            bool instrumentCreated = false;
            if (instrument == null)
            {
              instrumentCreated = true;
              progressDialog.LogInformation($"Creating new instrument {pioTicker.Ticker} - {pioTicker.Name}");
              if (pioTicker.Market == Constants.TickerMarketStocks)
                instrument = new Data.Stock(pioTicker.Ticker, Instrument.DefaultAttributes, string.Empty, pioTicker.GetInstrumentType(), Array.Empty<string>(), pioTicker.Name, pioTicker.Name, Common.Constants.DefaultMinimumDateTime, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, exchangeId, Array.Empty<Guid>(), string.Empty);
              else
                instrument = new Data.Instrument(pioTicker.Ticker, Instrument.DefaultAttributes, string.Empty, pioTicker.GetInstrumentType(), Array.Empty<string>(), pioTicker.Name, pioTicker.Name, Common.Constants.DefaultMinimumDateTime, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, exchangeId, Array.Empty<Guid>(), string.Empty);
            }

            if (pioTickerDetails != null && instrument is Stock stock)
            {
              if (!instrumentCreated) progressDialog.LogInformation($"Updating stock {pioTicker.Ticker} - {pioTicker.Name}");
              stock.InceptionDate = pioTickerDetails.ListDate;
              stock.MarketCap = pioTickerDetails.MarketCap;
              stock.SharesOutstanding = pioTickerDetails.ShareClassSharesOutstanding;
              stock.EmployeeCount = pioTickerDetails.TotalEmployees;
              stock.Address = pioTickerDetails.Address;
              stock.City = pioTickerDetails.City;
              stock.State = pioTickerDetails.State;
              stock.Zip = pioTickerDetails.PostalCode;
              stock.PhoneNumber = pioTickerDetails.Phone;
              stock.Url = pioTickerDetails.HomepageUrl;
            }

            var metaData = new InstrumentMetaDataV1 { Ticker = pioTicker, Details = pioTickerDetails };
            instrument.Tag.Update(Constants.TagDataId, DateTime.UtcNow, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch, JsonSerializer.Serialize(metaData));
            if (instrumentCreated)
              m_instrumentService.Add(instrument);
            else
              m_instrumentService.Update(instrument);
            updateService = true;
          }
          catch (Exception ex)
          {
            progressDialog.LogError($"Failed to update instrument {pioTicker.Ticker} - {ex.Message}");
          }
        }
        else
         progressDialog.LogWarning($"Skipping unsupported ticker - {pioTicker.Ticker}, {pioTicker.Market}, {pioTicker.Type}");

        progressDialog.Progress++;
        if (progressDialog.CancellationTokenSource.Token.IsCancellationRequested) break;
      }

      if (updateService)
      {
        progressDialog.LogInformation("Refreshing instrument model");
        m_instrumentService.Refresh();
      }
        
      progressDialog.Complete = true;      
    }

    //methods


  }
}
