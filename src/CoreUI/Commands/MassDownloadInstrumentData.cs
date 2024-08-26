using System.Diagnostics;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Commands;

namespace TradeSharp.CoreUI.Services
{
    /// <summary> 
    /// Implementation of the mass download of instrument data.
    /// </summary>
    public partial class MassDownloadInstrumentData:  Command, IMassDownloadInstrumentData
  {
    //constants


    //enums


    //types


    //attributes
    IInstrumentService m_instrumentService;

    //constructors
    public MassDownloadInstrumentData() : base()
    {
      m_instrumentService = (IInstrumentService)m_serviceHost.GetService(typeof(IInstrumentService))!;
    }

    //finalizers


    //interface implementations


    //attributes
    int m_requestSuccessCount;
    int m_requestFailureCount;
    int m_responseSuccessCount;
    int m_responseFailureCount;
    IProgressDialog m_progressDialog;
    IMassDownloadInstrumentData.Context m_context;
    IDataProviderPlugin? m_dataProvider;

    //properties
    protected List<Tuple<Resolution, Instrument>> m_requestsSent;

    //methods
    //NOTES:
    // - The DataProviderPlugin needs to catch any exceptions to properly handle the download/retry flow.
    // - The Massdownload will capture exceptions but that might lead to some of the requests being skipped leading to
    //   incomplete data.
    public override Task StartAsync(IProgressDialog progressDialog, object? context)
    {
      m_progressDialog = progressDialog;

      if (context is not IMassDownloadInstrumentData.Context m_context)
      {
        State = CommandState.Failed;
        progressDialog.LogError("Failed to start mass download, no data provider was set");
        return Task.CompletedTask;
      }

      IPluginsService pluginService = (IPluginsService)m_serviceHost.GetService(typeof(IPluginsService))!;
      m_dataProvider = pluginService.GetDataProviderPlugin(m_context.DataProvider);

      if (m_dataProvider == null)
      {
        State = CommandState.Failed;
        progressDialog.LogError("Failed to start mass download, data provider not found");
        return Task.CompletedTask;
      }

      if (!m_dataProvider.IsConnected)
      {
        State = CommandState.Failed;
        progressDialog.LogError("Failed to start mass download, data provider is not connected");
        return Task.CompletedTask;
      }

      if (m_context.Instruments.Count == 0)
      {
        State = CommandState.Failed;
        progressDialog.LogWarning("No instruments were provided");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        State = CommandState.Running;
        m_dataProvider.RequestError += onRequestError;
        m_dataProvider.DataDownloadComplete += onDataDownloadCompleted;

        try
        {
          Queue<Tuple<Resolution, Instrument>> downloadCombinations = new Queue<Tuple<Resolution, Instrument>>();
          Dictionary<Tuple<Resolution, Instrument>,int> retryCounts = new Dictionary<Tuple<Resolution, Instrument>, int>();
          foreach (Instrument instrument in m_context.Instruments)
          {
            if (m_context.Settings.ResolutionMinute) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Minutes, instrument));
            if (m_context.Settings.ResolutionHour) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Hours, instrument));
            if (m_context.Settings.ResolutionDay) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Days, instrument));
            if (m_context.Settings.ResolutionWeek) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Weeks, instrument));
            if (m_context.Settings.ResolutionMonth) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Months, instrument));
          }

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

          m_progressDialog.StatusMessage = $"Requesting data for {m_context.Instruments.Count} instruments";
          m_progressDialog.Progress = 0;
          m_progressDialog.Minimum = 0;
          m_progressDialog.Maximum = downloadCombinations.Count * 2;  //*2 since we count the requests and the responses to be received
          m_progressDialog.ShowAsync();

          List<Task> tasks = new List<Task>();
          while (downloadCombinations.Count > 0 && !m_progressDialog.CancellationTokenSource.IsCancellationRequested)
          {
            //allocate block of threads to download data
            while (tasks.Count < m_context.Settings.ThreadCount && downloadCombinations.Count > 0)
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
                  if (m_dataProvider.Request(downloadCombination.Item2, downloadCombination.Item1, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime))
                    lock (successCountLock) m_requestSuccessCount++;
                  else
                    retryRequest(downloadCombination, retryCounts, downloadCombinations, failureCountLock);
                }
                catch (Exception e)
                {
                  m_progressDialog.LogError($"Request on instrument {downloadCombination.Item2.Ticker}, resolution {downloadCombination.Item1} failed - (Exception: \"{e.Message}\"");
                  retryRequest(downloadCombination, retryCounts, downloadCombinations, failureCountLock);
                }
                m_progressDialog.Progress++;
              }));
            }

            //wait for all threads to complete and then clear the list for next block to download
            try { Task.WaitAll(tasks.ToArray(), m_progressDialog.CancellationTokenSource.Token); } catch (OperationCanceledException) { }
            tasks.Clear();
          }

          if (!m_progressDialog.CancellationTokenSource.IsCancellationRequested)
          {
            //adjust the progress maximum to account for the requests that failed to be sent
            m_progressDialog.Maximum -= m_requestFailureCount;
            m_progressDialog.StatusMessage = $"Waiting for {m_requestSuccessCount - m_requestSuccessCount} responses from requests that were successfully sent ({m_requestFailureCount} - requests failed).";

            //wait for reconnection if the data provider was disconnected
            while (m_responseSuccessCount < m_requestSuccessCount && !m_progressDialog.CancellationTokenSource.IsCancellationRequested) waitForHealthyConnection();
          }
          else
          {
            m_progressDialog.StatusMessage += " - Cancelled";
            m_progressDialog.Complete = true;
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
            m_progressDialog.LogInformation($"Outstanding responses - {m_requestsSent.Count}");

            foreach (var request in m_requestsSent)
              m_progressDialog.LogError($"Request failed for {request.Item2.Ticker} resolution {request.Item1}");
            m_progressDialog.StatusMessage = $"Mass download complete";
          }

          m_progressDialog.Complete = true;
          State = CommandState.Completed;
        }
        catch (Exception e)
        {
          State = CommandState.Failed;
          m_progressDialog.LogError($"EXCEPTION: Mass download main thread failed - (Exception: \"{e.Message}\"");
        }

        m_dataProvider.RequestError -= onRequestError;
      });
    }

    /// <summary>
    /// Wait for the data provider to become connected if it is disconnected for some reason.
    /// </summary>
    protected void waitForHealthyConnection()
    {
      if (!m_dataProvider.IsConnected)
      {
        bool showDisconnectedMessage = false;
        while (!m_dataProvider.IsConnected && !m_progressDialog.CancellationTokenSource.IsCancellationRequested)
        {
          if (!showDisconnectedMessage)
          {
            m_progressDialog.LogError("Data provider is not connected, waiting for reconnection");
            showDisconnectedMessage = true;
          }
          Thread.Sleep(1000);
        }

        if (m_dataProvider.IsConnected && !m_progressDialog.CancellationTokenSource.IsCancellationRequested)
          m_progressDialog.LogInformation("Data provider reconnected.");
      }
    }

    protected void retryRequest(Tuple<Resolution, Instrument> downloadCombination, Dictionary<Tuple<Resolution, Instrument>, int> retryCounts, Queue<Tuple<Resolution, Instrument>> downloadCombinations, object failureCountLock)
    {
      if (m_context.Settings.RetryCount > 0)
      {
        //create/check the retry count for the request in question
        if (retryCounts.ContainsKey(downloadCombination))
        {
          lock (retryCounts) retryCounts[downloadCombination]++;
          if (retryCounts[downloadCombination] < m_context.Settings.RetryCount) 
          {
            m_progressDialog.LogWarning($"Request failed for {downloadCombination.Item2.Ticker} resolution {downloadCombination.Item1} - retrying ({retryCounts[downloadCombination]}/{m_context.Settings.RetryCount})");
            lock (downloadCombinations) downloadCombinations.Enqueue(downloadCombination);  //requeue the request to try again later
            m_progressDialog.Maximum++; //calling method will increment progress, so offset that here
          } 
          else
          {
            lock (failureCountLock) m_requestFailureCount++;
            m_progressDialog.LogError($"Request failed for {downloadCombination.Item2.Ticker} resolution {downloadCombination.Item1} - retry limit reached");
          }
        }
        else
        {
          lock (retryCounts) retryCounts.Add(downloadCombination, 1);
          m_progressDialog.LogError($"Request failed for {downloadCombination.Item2.Ticker} resolution {downloadCombination.Item1}");
        }
      }
      else
        lock (failureCountLock) m_requestFailureCount++;
    }

    protected void onRequestError(object sender, RequestErrorArgs args)
    {
      if (args is DataDownloadErrorArgs downloadErrorArgs)
      {
        lock (m_requestsSent)
        {
          m_responseFailureCount++;
          m_progressDialog.LogError(args.Message);
          var request = m_requestsSent.Find(r => r.Item1 == downloadErrorArgs.Resolution && r.Item2 == downloadErrorArgs.Instrument);
          if (request != null) m_requestsSent.Remove(request);
        }
      }
      else
      {
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
