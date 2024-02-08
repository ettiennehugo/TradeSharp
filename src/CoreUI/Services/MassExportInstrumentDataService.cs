using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Windows implementation of the mass export of instrument data - use as singleton.
  /// </summary>
  public class MassExportInstrumentDataService : ServiceBase, IMassExportInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes
    IInstrumentService m_instrumentService;
    ILogger<MassExportInstrumentDataService> m_logger;
    ILogger? m_taskLogger;

    //constructors
    public MassExportInstrumentDataService(ILogger<MassExportInstrumentDataService> logger, IDialogService dialogService, IInstrumentService instrumentService) : base(dialogService)
    {
      Settings = new MassExportSettings();
      m_logger = logger;
      IsRunning = false;
      m_instrumentService = instrumentService;
    }

    //finalizers


    //interface implementations


    //properties
    public string DataProvider { get; set; }
    public ILogger? Logger { get => m_taskLogger; set => m_taskLogger = value; }
    public MassExportSettings Settings { get; set; }
    public bool IsRunning { get; internal set; }

    //methods
    public Task Start(CancellationToken cancellationToken = default)
    {
      if (DataProvider == string.Empty)
      {
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation("Failed to start mass export, no data provider was set");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        IsRunning = true;
        int exportInstrumentCount = 0;

        //draw up the set of instruments to export in a stack
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Starting mass export of instrument data for {m_instrumentService.Items.Count} instruments");

        //create export directories if they don't exist
        string directory = Settings.Directory;
        if (!Directory.Exists(directory)) 
        {
          if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{directory}\"");
          Directory.CreateDirectory(directory);
        }

        if (Settings.ResolutionMinute && Settings.ExportStructure == MassImportExportStructure.DiretoriesAndFiles)
        {
          directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenMinute}";
          if (!Directory.Exists(directory))
          {
            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{directory}\"");
            Directory.CreateDirectory(directory);
          }

          directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenHour}";
          if (!Directory.Exists(directory))
          {
            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{directory}\"");
            Directory.CreateDirectory(directory);
          }

          directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenDay}";
          if (!Directory.Exists(directory))
          {
            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{directory}\"");
            Directory.CreateDirectory(directory);
          }

          directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenWeek}";
          if (!Directory.Exists(directory))
          {
            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{directory}\"");
            Directory.CreateDirectory(directory);
          }

          directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenMonth}";
          if (!Directory.Exists(directory))
          {
            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{directory}\"");
            Directory.CreateDirectory(directory);
          }
        }

        //export all the data according to the defined data resolutions


        //TODO: Rework this logic to first build up the list of instruments and resolutions to process and then run the export.


        foreach (Resolution resolution in Enum.GetValues(typeof(Resolution)))
        {
          //skip resolution if not selected
          if (resolution == Resolution.Minute && !Settings.ResolutionMinute) continue;
          if (resolution == Resolution.Hour && !Settings.ResolutionHour) continue;
          if (resolution == Resolution.Day && !Settings.ResolutionDay) continue;
          if (resolution == Resolution.Week && !Settings.ResolutionWeek) continue;
          if (resolution == Resolution.Month && !Settings.ResolutionMonth) continue;

          //load up the stack with the instruments to process
          Stack<Instrument> instrumentsList = new Stack<Instrument>();
          foreach (Instrument instrument in m_instrumentService.Items) instrumentsList.Push(instrument);
          instrumentsList = reverseStack(instrumentsList);
          exportInstrumentCount += instrumentsList.Count;

          //start the list of tasks that would pop an instrument off the stack and export the data, when the stack is empty
          //or the cancellation token set the tasks would exit
          List<Task> taskPool = new List<Task>();
          for (int i = 0; i < Settings.ThreadCount; i++)
            taskPool.Add(
            Task.Run(() =>
            {
                if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Started worker thread for export for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");

                IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
                instrumentBarDataService.DataProvider = DataProvider;
                instrumentBarDataService.MassOperation = true;
                instrumentBarDataService.Resolution = resolution;

                while (instrumentsList.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                  //pop an instrument off the stack and export the data
                  Instrument? instrument = null;
                  lock (instrumentsList) 
                    if (instrumentsList.Count > 0) instrument = instrumentsList.Pop();  //only pop if there are instruments otherwise this will raise an exception
                  if (instrument == null) continue; //failed to find a an instrument to export, stack should be empty should be empty




                instrumentBarDataService.Instrument = instrument;
                  string exportFilename = getExportFileName(resolution, instrument);
                  if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Starting export for instrument data {instrument.Ticker} for resolution {resolution} to file \"{exportFilename}\"");



                  //UNCOMMENT AFTER INITIAL TESTING
                  //instrumentBarDataService.Export(exportFilename);



                }

              if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Ending worker thread for export for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");
            }, cancellationToken)
            );

          //wait for tasks to finish exporting data for this resolution
          Task.WaitAll(taskPool.ToArray());

          //exit processing if the cancellation token is set
          if (cancellationToken.IsCancellationRequested) break;
        }

        IsRunning = false;

        //output status message
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Export", $"Exported {exportInstrumentCount} files for the selected resolutions");
      });
    }

    private Stack<Instrument> reverseStack(Stack<Instrument> input)
    {
      Stack<Instrument> output = new Stack<Instrument>();
      while (input.Count > 0) output.Push(input.Pop());
      return output;
    }

    private string getExportFileName(Resolution resolution, Instrument instrument)
    {
      string filename = "";
      string resolutionStr = "";
      string extension = Settings.FileType.ToString().ToLower();

      switch (Settings.ExportStructure)
      {
        case MassImportExportStructure.DiretoriesAndFiles:          
          switch (resolution)
          {
            case Resolution.Minute:
              resolutionStr = IMassExportInstrumentDataService.TokenMinute;
              break;
            case Resolution.Hour:
              resolutionStr = IMassExportInstrumentDataService.TokenHour;
              break;
            case Resolution.Day:
              resolutionStr = IMassExportInstrumentDataService.TokenDay;
              break;
            case Resolution.Week:
              resolutionStr = IMassExportInstrumentDataService.TokenWeek;
              break;
            case Resolution.Month:
              resolutionStr = IMassExportInstrumentDataService.TokenMonth;
              break;
          }

          filename = $"{Settings.Directory}\\{resolutionStr}\\{instrument.Ticker}.{extension}";
          break;
        case MassImportExportStructure.FilesOnly:
          filename = $"{Settings.Directory}\\{instrument.Ticker}_{resolution}.{extension}";
          break;
      }
      return filename;
    }
  }
}
