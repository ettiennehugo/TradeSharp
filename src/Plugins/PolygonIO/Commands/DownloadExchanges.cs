using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.PolygonIO.Commands
{
	/// <summary>
	/// Download Polygon exchange definitions to the local cache.
	/// </summary>
  public class DownloadExchanges
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger m_logger;
    protected IDialogService m_dialogService;
    protected IExchangeService m_exchangeService;
    protected IDatabase m_database;
    protected Client m_client;
    protected Cache m_cache;

    //properties


    //constructors
    public DownloadExchanges(ILogger logger, IDialogService dialogService, IExchangeService exchangeService, IDatabase database, Client client, Cache cache)
    {
      m_logger = logger;
      m_exchangeService = exchangeService;
      m_dialogService = dialogService;
      m_database = database;
      m_client = client;
      m_cache = cache;
    }

    //finalizers


    //interface implementations
    public void Run()
    {
        var progressDialog = m_dialogService.CreateProgressDialog("Downloading Exchanges", m_logger);
        progressDialog.StatusMessage = "Requesting exchange definitions";
        progressDialog.ShowAsync();

        IList<Messages.ExchangeDto>? results = null;
        try
        {
          results = m_client.GetExchanges(progressDialog).Result;
        }
        catch (Exception ex)
        {
          progressDialog.LogError($"Failed to download exchanges from Polygon - {ex.Message}");
          progressDialog.Complete = true;
          return;
        }

        progressDialog.LogInformation("Sending request...waiting for response");
        if (results == null)
        {
          progressDialog.LogError("Failed to download exchanges from Polygon");
          progressDialog.Complete = true;
          return;
        }

        progressDialog.Maximum = results.Count;

        progressDialog.StatusMessage = "Updating exchange definitions";
        progressDialog.LogInformation($"Received {results.Count} exchange definitions");
        foreach (var result in results)
        {
          var exchange = new PolygonIO.Exchange
          {
            Acronym = result.Acronym,
            AssetClass = result.AssetClass,
            Id = result.Id,
            Locale = result.Locale,
            Mic = result.Mic,
            Name = result.Name,
            OperatingMic = result.OperatingMic,
            ParticipantId = result.ParticipantId,
            Type = result.Type,
            Url = result.Url
          };

          m_cache.UpdateExchange(exchange);
          progressDialog.Progress++;
          if (progressDialog.CancellationTokenSource.Token.IsCancellationRequested) break;
        }
        progressDialog.Complete = true;
    }

    //methods


  }
}
