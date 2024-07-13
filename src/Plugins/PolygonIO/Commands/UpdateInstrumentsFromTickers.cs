using Microsoft.Extensions.Logging;
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
    public async Task Run()
    {
      var progressDialog = m_dialogService.CreateProgressDialog("Updating Instruments", m_logger);
      progressDialog.StatusMessage = "Updating instruments";
      await progressDialog.ShowAsync();

      var tickers = m_cache.GetTickers();
      var tickerDetails = m_cache.GetTickerDetails();
      progressDialog.Maximum = tickers.Count;

      foreach (var pioTicker in tickers)
      {
        if (isTradeSharpSupported(pioTicker))
        {
          try
          {
            var pioTickerDetails = tickerDetails.FirstOrDefault(td => td.Ticker == pioTicker.Ticker);
            var instrument = m_instrumentService.Items.FirstOrDefault(i => i.Ticker == pioTicker.Ticker || i.AlternateTickers.Contains(pioTicker.Ticker));

            //find the exchange associated with the Instrument
            Guid exchangeId = Data.Exchange.InternationalId;
            var exchange = m_exchangeService.Items.FirstOrDefault(e => e.Name == pioTicker.PrimaryExchange);
            if (exchange != null) exchangeId = exchange.Id;

            //create update the instrument
            if (instrument == null)
            {
              if (pioTicker.Market == Constants.TickerMarketStocks)
              {
                instrument = new Data.Stock(pioTicker.Ticker, Instrument.DefaultAttributeSet, pioTicker.Ticker, getInstrumentType(pioTicker), Array.Empty<string>(), pioTicker.Name, pioTicker.Name, Common.Constants.DefaultMinimumDateTime, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, exchangeId, Array.Empty<Guid>(), string.Empty);
              }
              else
                instrument = new Data.Instrument(pioTicker.Ticker, Instrument.DefaultAttributeSet, pioTicker.Ticker, getInstrumentType(pioTicker), Array.Empty<string>(), pioTicker.Name, pioTicker.Name, Common.Constants.DefaultMinimumDateTime, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, exchangeId, Array.Empty<Guid>(), string.Empty);
            }

            if (pioTickerDetails != null && instrument is Stock stock)
            {
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


            //TODO: Update item in Items list.


            m_instrumentService.Update(instrument);
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

      progressDialog.Complete = true;
      
    }

    //methods
    protected bool isTradeSharpSupported(Tickers ticker)
    {
      if (ticker.Market.Equals(Constants.TickerMarketStocks, StringComparison.OrdinalIgnoreCase) || ticker.Market.Equals(Constants.TickerMarketOTC, StringComparison.OrdinalIgnoreCase))
      {
        if (ticker.Type.Equals(Constants.TickerTypeCommonStock) || ticker.Market.Equals(Constants.TickerMarketOTC, StringComparison.OrdinalIgnoreCase)) return true;
        if (ticker.Type.Equals(Constants.TickerTypeETF)) return true;
      }
      if (ticker.Market.Equals(Constants.TickerMarketForex, StringComparison.OrdinalIgnoreCase)) return true;

      return false;
    }

    protected InstrumentType getInstrumentType(Tickers ticker)
    {
      if (string.IsNullOrEmpty(ticker.Market)) return InstrumentType.Unknown;
      if (ticker.Market.Equals(Constants.TickerMarketStocks, StringComparison.OrdinalIgnoreCase) || ticker.Market.Equals(Constants.TickerMarketOTC, StringComparison.OrdinalIgnoreCase))
      {
        if (ticker.Type.Equals(Constants.TickerTypeCommonStock) || ticker.Market.Equals(Constants.TickerMarketOTC, StringComparison.OrdinalIgnoreCase)) return InstrumentType.Stock;
        if (ticker.Type.Equals(Constants.TickerTypeETF)) return InstrumentType.ETF;
      }
      if (ticker.Market.Equals(Constants.TickerMarketForex, StringComparison.OrdinalIgnoreCase)) return InstrumentType.Forex;
      return InstrumentType.Unknown;
    }
  }
}
