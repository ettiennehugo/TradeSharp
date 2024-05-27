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


    //properties
    public IDataProviderPlugin DataProvider { get; set; }
    public ILogger Logger { get; set; }
    public MassDownloadSettings Settings { get; set; }
    [ObservableProperty] public bool m_isRunning;

    //methods
    public Task StartAsync(IProgressDialog progressDialog, IList<Instrument> instruments)
    {
      if (DataProvider == null)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogError("Failed to start mass download, no data provider was set");
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
        try
        {
          Queue<Tuple<Resolution, Instrument>> downloadCombinations = new Queue<Tuple<Resolution, Instrument>>();
          foreach (Instrument instrument in instruments)
          {
            if (Settings.ResolutionMinute) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Minute, instrument));
            if (Settings.ResolutionHour) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Hour, instrument));
            if (Settings.ResolutionDay) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Day, instrument));
            if (Settings.ResolutionWeek) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Week, instrument));
            if (Settings.ResolutionMonth) downloadCombinations.Enqueue(new Tuple<Resolution, Instrument>(Resolution.Month, instrument));
          }

          IsRunning = true;
          Stopwatch stopwatch = new Stopwatch();
          stopwatch.Start();

          int instrumentDownloadCount = 0;
          object successCountLock = new object();
          int successCount = 0;
          object failureCountLock = new object();
          int failureCount = 0;

          progressDialog.StatusMessage = $"Requesting data for {instruments.Count} instruments";
          progressDialog.Progress = 0;
          progressDialog.Minimum = 0;
          progressDialog.Maximum = downloadCombinations.Count;
          progressDialog.ShowAsync();

          List<Task> tasks = new List<Task>();
          while (downloadCombinations.Count > 0 && !progressDialog.CancellationTokenSource.IsCancellationRequested)
          {
            //allocate block of threads to download data
            while (tasks.Count < Settings.ThreadCount && downloadCombinations.Count > 0)
            {
              Tuple<Resolution, Instrument> downloadCombination = downloadCombinations.Dequeue();
              tasks.Add(Task.Run(() =>
              {
                progressDialog.LogInformation($"Requesting data for {downloadCombination.Item2.Ticker}");
                if (DataProvider!.Request(downloadCombination.Item2, downloadCombination.Item1, Settings.FromDateTime, Settings.ToDateTime))
                  lock (successCountLock) successCount++;
                else
                  lock (failureCountLock) failureCount++;
                progressDialog.Progress++;
              }));
            }

            //wait for all threads to complete and then clear the list for next block
            try { Task.WaitAll(tasks.ToArray(), progressDialog.CancellationTokenSource.Token); } catch (OperationCanceledException) { }
            tasks.Clear();
          }

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;

          if (progressDialog.CancellationTokenSource.IsCancellationRequested)
            progressDialog.StatusMessage += " - Cancelled";
          else
            progressDialog.StatusMessage = $"Mass download complete - Attempted {instrumentDownloadCount} instruments, downloaded {successCount} instruments successfully and failed on {failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})";

          progressDialog.Complete = true;
          IsRunning = false;
        }
        catch (Exception e)
        {
          IsRunning = false;
          progressDialog.LogError($"EXCEPTION: Mass download main thread failed - (Exception: \"{e.Message}\"");
        }
      });
    }
  }
}
