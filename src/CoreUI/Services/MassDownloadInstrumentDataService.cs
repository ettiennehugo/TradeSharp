using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
namespace TradeSharp.CoreUI.Services
{
  /// <summary> 
  /// Implementation of the mass download of instrument data.
  /// </summary>
  public class MassDownloadInstrumentDataService : ServiceBase, IMassDownloadInstrumentDataService
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
    public bool IsRunning { get; internal set; }

    //methods
    public Task Start(CancellationToken cancellationToken = default)
    {
      if (DataProvider == null)
      {
        if (Debugging.MassInstrumentDataDownload) m_logger.LogInformation("Failed to start mass download, no data provider was set");
        return Task.CompletedTask;
      }

      return Task.CompletedTask;    //TODO: Implement running of task.
    }
  }
}
