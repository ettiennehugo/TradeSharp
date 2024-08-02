using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
  /// <summary>
  /// Download Polygon ticker header definitions to the local cache.
  /// </summary>
  public class DownloadTickers
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger m_logger;
    protected IDialogService m_dialogService;
    protected IInstrumentService m_instrumentService;
    protected IDatabase m_database;
    protected Client m_client;
    protected Cache m_cache;

    //properties


    //constructors
    public DownloadTickers(ILogger logger, IDialogService dialogService, IInstrumentService instrumentService, IDatabase database, Client client, Cache cache)
    {
      m_logger = logger;
      m_instrumentService = instrumentService;
      m_dialogService = dialogService;
      m_database = database;
      m_client = client;
      m_cache = cache;
    }

    //finalizers


    //interface implementations
    public void Run()
    {
      var progressDialog = m_dialogService.CreateProgressDialog("Downloading Ticker Definitions", m_logger);
      progressDialog.StatusMessage = "Requesting ticker definitions";
      progressDialog.ShowAsync();

      IList<Messages.TickersDto>? results = null;
      try
      {
        results = m_client.GetTickers(progressDialog).Result;
      }
      catch (Exception ex)
      {
        m_logger.LogError($"Failed to download ticker definitions from Polygon - {ex.Message}");
      }

      if (results == null)
      {
        progressDialog.StatusMessage = "Failed to download ticker definitions from Polygon";
        progressDialog.Complete = true;
        return;
      }

      progressDialog.Maximum = results.Count;

      progressDialog.StatusMessage = "Updating ticker definitions";
      foreach (var result in results)
      {
        var ticker = new Tickers
        {
          Ticker = result.Ticker,
          Active = result.Active,
          Cik = result.Cik,
          CompositeFigi = result.CompositeFigi,
          CurrencyName = result.CurrencyName,
          LastUpdatedUtc = result.LastUpdatedUtc,
          Locale = result.Locale,
          Market = result.Market,
          Name = result.Name,
          PrimaryExchange = result.PrimaryExchange,
          ShareClassFigi = result.ShareClassFigi,
          Type = result.Type,
        };

        m_cache.UpdateTickers(ticker);
        progressDialog.Progress++;
        if (progressDialog.CancellationTokenSource.Token.IsCancellationRequested) break;
        progressDialog.Complete = true;
      }
    }

    //methods



  }
}
