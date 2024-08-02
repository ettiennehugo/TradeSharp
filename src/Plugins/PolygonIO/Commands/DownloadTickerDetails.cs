using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
  /// <summary>
  /// Download Polygon ticker definitions to the local cache.
  /// </summary>
  public class DownloadTickerDetails
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger m_logger;
    protected IDialogService m_dialogService;
    protected Client m_client;
    protected Cache m_cache;
    protected IInstrumentService m_instrumentService;
    protected IDatabase m_database;

    //properties


    //constructors
    public DownloadTickerDetails(ILogger logger, IDialogService dialogService, IInstrumentService instrumentService, IDatabase database, Client client, Cache cache)
    {
      m_logger = logger;
      m_instrumentService = instrumentService;
      m_dialogService = dialogService;
      m_database = database;
      m_client = client;
      m_cache = cache;
    }

    // Ensure this method is marked as async to use await
    public void Run()
    {
      var progressDialog = m_dialogService.CreateProgressDialog("Downloading Ticker Details", m_logger);
      progressDialog.StatusMessage = "Requesting ticker details";
      progressDialog.ShowAsync();
      var tickers = m_cache.GetTickers();
      if (tickers == null || tickers.Count == 0)
      {
        progressDialog.StatusMessage = "No tickers found in cache - download headers first";
        progressDialog.Complete = true;
        return;
      }

      progressDialog.Maximum = tickers.Count;
      foreach (var ticker in tickers)
      {
        try
        {
          var result = m_client.GetTickerDetails(ticker.Ticker, progressDialog).Result;

          //process response if we have a valid response
          if (result != null && result.Result != null)
          {
            var tickerDetails = new TickerDetails
            {
              Ticker = result.Result.Ticker,
              TickerRoot = result.Result.TickerRoot,
              Type = result.Result.Type,
              Name = result.Result.Name,
              Description = result.Result.Description,
              Market = result.Result.Market,
              Locale = result.Result.Locale,
              CurrencyName = result.Result.CurrencyName,
              PrimaryExchange = result.Result.PrimaryExchange,
              Cik = result.Result.Cik,
              CompositeFigi = result.Result.CompositeFigi,
              ShareClassFigi = result.Result.ShareClassFigi,
              ShareClassSharesOutstanding = result.Result.ShareClassSharesOutstanding,
              MarketCap = result.Result.MarketCap,
              TotalEmployees = result.Result.TotalEmployees,
              Phone = result.Result.PhoneNumber,
              Address = result.Result.Address.Address ?? string.Empty,
              City = result.Result.Address.City ?? string.Empty,
              State = result.Result.Address.State ?? string.Empty,
              PostalCode = result.Result.Address.PostalCode ?? string.Empty,
              SicCode = result.Result.SicCode,
              SicDescription = result.Result.SicDescription,
              HomepageUrl = result.Result.HomepageUrl,
              LogoUrl = result.Result.Branding.LogoUrl ?? string.Empty,
              IconUrl = result.Result.Branding.IconUrl ?? string.Empty,
              ListDate = result.Result.ListDate,
              RoundLot = result.Result.RoundLot,
              WeightedSharesOutstanding = result.Result.WeightedSharesOutstanding,
              Active = result.Result.Active
            };

            m_cache.UpdateTickerDetails(tickerDetails);
          }
        }
        catch (Exception ex)
        {
          progressDialog.LogError($"Failed to download ticker details for {ticker.Ticker}: {ex.Message}");
        }

        progressDialog.Progress++;
        if (progressDialog.CancellationTokenSource.Token.IsCancellationRequested) break;
      }
      progressDialog.Complete = true;
    }

    //methods



  }
}
