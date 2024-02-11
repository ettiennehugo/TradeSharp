using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TradeSharp.Common;
using TradeSharp.Data;
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
    ILogger? m_taskLogger;

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
    public IDataProvider DataProvider { get; set; }
    public ILogger Logger { get; set; }
    public MassDownloadSettings Settings { get; set; }
    [ObservableProperty] public bool m_isRunning;

    //methods
    public Task Start(CancellationToken cancellationToken = default)
    {
      if (DataProvider == null)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogInformation("Failed to start mass download, no data provider was set");
        return Task.CompletedTask;
      }

      if (IsRunning)
      {
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation("Mass download already running, returning from mass download");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        IsRunning = true;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        int instrumentDownloadCount = 0;
        object successCountLock = new object();
        int successCount = 0;
        object failureCountLock = new object();
        int failureCount = 0;


        //TODO: implement mass download of instrument data


        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;
        IsRunning = false;

        //output status message
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Mass download complete - Attempted {instrumentDownloadCount} instruments, downloaded {successCount} instruments successfully and failed on {failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Download", $"Downloaded data for {instrumentDownloadCount} instruments for the selected resolutions");
      });
    }
  }
}
