using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary> 
  /// Implementation of the mass download of instrument data.
  /// </summary>
  public partial class MassDownloadInstrumentDataService : ServiceBase, IMassDownloadInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes
    IInstrumentService m_instrumentService;
    ILogger<MassDownloadInstrumentDataService> m_logger;

    //constructors
    public MassDownloadInstrumentDataService(ILogger<MassDownloadInstrumentDataService> logger, IDialogService dialogService, IInstrumentService instrumentService) : base(dialogService)
    {
      Settings = new MassDownloadSettings();
      m_logger = logger;
      IsRunning = false;
      m_instrumentService = instrumentService;
    }

    //finalizers


    //interface implementations


    //attributes
    int m_requestSuccessCount;
    int m_requestFailureCount;
    int m_responseSuccessCount;
    int m_responseFailureCount;

    //properties
    public IDataProviderPlugin DataProvider { get; set; }
    public ILogger Logger { get; set; }
    public MassDownloadSettings Settings { get; set; }
    [ObservableProperty] public bool m_isRunning;
    protected IProgressDialog m_progressDialog;
    protected List<Tuple<Resolution, Instrument>> m_requestsSent;

    //methods
    //NOTES:
    // - The DataProviderPlugin needs to catch any exceptions to properly handle the download/retry flow.
    // - The Massdownload will capture exceptions but that might lead to some of the requests being skipped leading to
    //   incomplete data.
    public Task StartAsync(IProgressDialog progressDialog, IList<Instrument> instruments)
    {
      m_progressDialog = progressDialog;

      if (DataProvider == null)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogError("Failed to start mass download, no data provider was set");
        return Task.CompletedTask;
      }

      if (DataProvider.IsConnected == false)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogError("Failed to start mass download, data provider is not connected");
        return Task.CompletedTask;
      }

      if (IsRunning)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogWarning("Mass download already running, returning from mass download");
        return Task.CompletedTask;
      }

      if (instruments.Count == 0)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogWarning("Failed to start mass download, no instruments were provided");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        LoadedState = LoadedState.Loading;
        DataProvider.RequestError += onRequestError;
        DataProvider.DataDownloadComplete += onDataDownloadCompleted;

        try
        {
          Queue<Tuple<Resolution, Instrument>> downloadCombinations = new Queue<Tuple<Resolution, Instrument>>();
          Dictionary<Tuple<Resolution, Instrument>,int> retryCounts = new Dictionary<Tuple<Resolution, Instrument>, int>();
          foreach (Instrument instrument in instruments)
          {
            if (Settings.ResolutionMinute) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Minutes, instrument));
            if (Settings.ResolutionHour) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Hours, instrument));
            if (Settings.ResolutionDay) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Days, instrument));
            if (Settings.ResolutionWeek) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Weeks, instrument));
            if (Settings.ResolutionMonth) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Months, instrument));
          }

          IsRunning = true;
          Stopwatch stopwatch = new Stopwatch();
          stopwatch.Start();

          m_requestsSent = downloadCombinations.ToList();   //record the expected number of requests to be sent
          object instrumentDownloadCountLock = new object();
          int instrumentDownloadCount = 0;
          object successCountLock = new object();
          m_requestSuccessCount = 0;
          m_responseSuccessCount = 0;
          m_requestFailureCount = 0;
          m_responseFailureCount = 0;
          object failureCountLock = new object();

          m_progressDialog.StatusMessage = $"Requesting data for {instruments.Count} instruments";
          m_progressDialog.Progress = 0;
          m_progressDialog.Minimum = 0;
          m_progressDialog.Maximum = downloadCombinations.Count * 2;  //*2 since we count the requests and the responses to be received
          m_progressDialog.ShowAsync();

          List<Task> tasks = new List<Task>();
          while (downloadCombinations.Count > 0 && !m_progressDialog.CancellationTokenSource.IsCancellationRequested)
          {
            //allocate block of threads to download data
            while (tasks.Count < Settings.ThreadCount && downloadCombinations.Count > 0)
            {
              Tuple<Resolution, Instrument> downloadCombination = downloadCombinations.Dequeue();
              lock (instrumentDownloadCountLock) instrumentDownloadCount++;
              tasks.Add(Task.Run(() =>
              {
                try
                {
                  m_progressDialog.LogInformation($"Requesting data for {downloadCombination.Item2.Ticker} resolution {downloadCombination.Item1}");
                  waitForHealthyConnection();

                  //send the download request
                  if (DataProvider!.Request(downloadCombination.Item2, downloadCombination.Item1, Settings.FromDateTime, Settings.ToDateTime))
                    lock (successCountLock) m_requestSuccessCount++;
                  else
                    lock (failureCountLock) m_requestFailureCount++;
                }
                catch (Exception e)
                {
                  m_progressDialog.LogError($"EXCEPTION: Request on instrument {downloadCombination.Item2.Ticker}, resolution {downloadCombination.Item1} failed - (Exception: \"{e.Message}\"");
                  //requeue the request if it is still under the retry count threshold
                  if (retryCounts.ContainsKey(downloadCombination))
                  {
                    lock (retryCounts) retryCounts[downloadCombination]++;
                    if (retryCounts[downloadCombination] < Settings.RetryCount) lock (downloadCombinations) downloadCombinations.Enqueue(downloadCombination);  //requeue the request to try again later
                  }
                  else if (Settings.RetryCount > 0)
                    lock(retryCounts) retryCounts.Add(downloadCombination, 1);
                }
                m_progressDialog.Progress++;
              }));
            }

            //wait for all threads to complete and then clear the list for next block
            try { Task.WaitAll(tasks.ToArray(), m_progressDialog.CancellationTokenSource.Token); } catch (OperationCanceledException) { }
            tasks.Clear();
          }

          if (!m_progressDialog.CancellationTokenSource.IsCancellationRequested)
          {
            //adjust the progress maximum to account for the requests that failed to be sent
            m_progressDialog.Maximum -= m_requestFailureCount;
            m_progressDialog.StatusMessage = $"Waiting for {m_requestSuccessCount} responses from requests that were successfully sent ({m_requestFailureCount} - requests failed).";

            //wait for reconnection if the data provider was disconnected
            while (m_responseSuccessCount < m_requestSuccessCount && !m_progressDialog.CancellationTokenSource.IsCancellationRequested) waitForHealthyConnection();
          }
          else
          {
            m_progressDialog.StatusMessage += " - Cancelled";
            m_progressDialog.Complete = true;
            IsRunning = false;
            LoadedState = LoadedState.Loaded;
          }

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;

          if (m_progressDialog.CancellationTokenSource.IsCancellationRequested)
            m_progressDialog.StatusMessage += " - Cancelled";
          else
          {
            m_progressDialog.LogInformation($"Attempted {instrumentDownloadCount} instrument/resolution combinations.");
            m_progressDialog.LogInformation($"Download Statistics:");
            m_progressDialog.LogInformation($"Elapsed time - {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}");
            m_progressDialog.LogInformation($"Requests successfully sent - {m_requestSuccessCount}");
            m_progressDialog.LogInformation($"Requests failured to send - {m_requestFailureCount}");
            m_progressDialog.LogInformation($"Responses successfully received - {m_responseSuccessCount}");
            m_progressDialog.LogInformation($"Responses failed to be received - {m_responseFailureCount}");
            m_progressDialog.StatusMessage = $"Mass download complete";
          }

          m_progressDialog.Complete = true;
          IsRunning = false;
          LoadedState = LoadedState.Loaded;
        }
        catch (Exception e)
        {
          LoadedState = LoadedState.Error;
          IsRunning = false;
          m_progressDialog.LogError($"EXCEPTION: Mass download main thread failed - (Exception: \"{e.Message}\"");
        }

        DataProvider.RequestError -= onRequestError;
      });
    }

    //Wait for the data provider to become connected if it is disconnected for some reason.
    protected void waitForHealthyConnection()
    {
      //wait for the data provider to become connected if for some reason it was disconnected
      if (!DataProvider.IsConnected)
      {
        bool showDisconnectedMessage = false;
        while (!DataProvider.IsConnected && !m_progressDialog.CancellationTokenSource.IsCancellationRequested)
        {
          if (!showDisconnectedMessage)
          {
            m_progressDialog.LogError("Data provider is not connected, waiting for reconnection");
            showDisconnectedMessage = true;
          }
          Thread.Sleep(1000);
        }

        if (DataProvider.IsConnected && !m_progressDialog.CancellationTokenSource.IsCancellationRequested)
          m_progressDialog.LogInformation("Data provider reconnected.");
      }
    }

    protected void onRequestError(object sender, RequestErrorArgs args)
    {
      if (args is DataDownloadErrorArgs downloadErrorArgs)
      {
        lock (m_requestsSent)
        {
          m_responseFailureCount++;
          m_logger.LogError($"Request error: {args.Message}");
          m_progressDialog.LogError(args.Message);
          var request = m_requestsSent.Find(r => r.Item1 == downloadErrorArgs.Resolution && r.Item2 == downloadErrorArgs.Instrument);
          if (request != null) m_requestsSent.Remove(request);
        }
      }
      else
      {
        m_logger.LogError($"Request error: {args.Message}");
        m_progressDialog.LogError(args.Message);
      }
    }

    protected void onDataDownloadCompleted(object sender, DataDownloadCompleteArgs args)
    {
      m_progressDialog.Progress++;
      m_progressDialog.LogInformation($"Download completed for - {args.Instrument.Ticker}, {args.Resolution}, {args.Count} bars received from {args.First.ToString() ?? "<No start date>"} to {args.Last.ToString() ?? "<No end date>"}");
      lock (m_requestsSent)
      {
        m_responseSuccessCount++;
        var request = m_requestsSent.Find(r => r.Item1 == args.Resolution && r.Item2 == args.Instrument);
        if (request != null) m_requestsSent.Remove(request);
      }
    }
  }
}
