using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using System.Diagnostics;

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
    private class ExportFile
    {
      public string Filename { get; set; }
      public Resolution Resolution { get; set; }
      public Instrument Instrument { get; set; }
    }

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
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        //draw up the set of instruments to export in a stack
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Starting mass export of instrument data for {m_instrumentService.Items.Count} instruments");

        //create export directories if they don't exist
        string parentDirectory = Settings.Directory;
        if (!Directory.Exists(Settings.Directory)) 
        {
          if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Creating export directory \"{Settings.Directory}\"");
          Directory.CreateDirectory(Settings.Directory);
        }

        if (Settings.ResolutionMinute && Settings.ExportStructure == MassImportExportStructure.DiretoriesAndFiles)
        {
          string directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenMinute}";
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

        //construct the set of files to be exported
        Stack<ExportFile> exportFileList = new Stack<ExportFile>();
        if (!Settings.ResolutionMinute)
          foreach (Instrument instrument in m_instrumentService.Items)
          {
            ExportFile exportFile = new ExportFile();
            exportFile.Filename = getExportFileName(Resolution.Minute, instrument);
            exportFile.Resolution = Resolution.Minute;
            exportFile.Instrument = instrument;
            exportFileList.Push(exportFile);
          }

        if (!Settings.ResolutionHour)
          foreach (Instrument instrument in m_instrumentService.Items)
          {
            ExportFile exportFile = new ExportFile();
            exportFile.Filename = getExportFileName(Resolution.Hour, instrument);
            exportFile.Resolution = Resolution.Hour;
            exportFile.Instrument = instrument;
            exportFileList.Push(exportFile);
          }

        if (!Settings.ResolutionDay)
          foreach (Instrument instrument in m_instrumentService.Items)
          {
            ExportFile exportFile = new ExportFile();
            exportFile.Filename = getExportFileName(Resolution.Day, instrument);
            exportFile.Resolution = Resolution.Day;
            exportFile.Instrument = instrument;
            exportFileList.Push(exportFile);
          }

        if (!Settings.ResolutionWeek)
          foreach (Instrument instrument in m_instrumentService.Items)
          {
            ExportFile exportFile = new ExportFile();
            exportFile.Filename = getExportFileName(Resolution.Week, instrument);
            exportFile.Resolution = Resolution.Week;
            exportFile.Instrument = instrument;
            exportFileList.Push(exportFile);
          }

        if (!Settings.ResolutionMonth)
          foreach (Instrument instrument in m_instrumentService.Items)
          {
            ExportFile exportFile = new ExportFile();
            exportFile.Filename = getExportFileName(Resolution.Month, instrument);
            exportFile.Resolution = Resolution.Month;
            exportFile.Instrument = instrument;
            exportFileList.Push(exportFile);
          }

        exportFileList = reverseStack(exportFileList);

        //export all the data according to the defined data resolutions
        int exportFileCount = 0;
        object successCountLock = new object();
        int successCount = 0;
        object failureCountLock = new object();
        int failureCount = 0;

        exportFileCount = exportFileList.Count();
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Starting mass export for \"{DataProvider}\" of instrument data for {exportFileCount} files");

        List<Task> taskPool = new List<Task>();
        for (int i = 0; i < Settings.ThreadCount; i++)
          taskPool.Add(
          Task.Run(() =>
          {
            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Started worker thread for export for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");

            IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
            instrumentBarDataService.DataProvider = DataProvider;
            instrumentBarDataService.MassOperation = true;

            while (exportFileList.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
              //pop an instrument off the stack and export the data
              ExportFile? exportFile = null;
              lock (exportFileList) 
                if (exportFileList.Count > 0) exportFile = exportFileList.Pop();  //only pop if there are instruments otherwise this will raise an exception
              if (exportFile == null) continue; //failed to find a an instrument to export, stack should be empty should be empty

              instrumentBarDataService.Resolution = exportFile.Resolution;
              instrumentBarDataService.Instrument = exportFile.Instrument;
              if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Exporting {exportFile.Instrument.Ticker} for resolution {exportFile.Resolution} to file \"{exportFile.Filename}\"");

              try
              {


                //UNCOMMENT AFTER INITIAL TESTING
                //instrumentBarDataService.Export(exportFilename);


                lock (successCountLock) successCount++;
              }
              catch (Exception e)
              {
                lock (failureCountLock) failureCount++;
                if (Debugging.MassInstrumentDataImport || Debugging.MassInstrumentDataImportException) m_logger.LogError($"EXCEPTION: Failed to export \"{exportFile.Filename}\" for {exportFile.Instrument.Ticker} at resolution {exportFile.Resolution} - (Exception: \"{e.Message}\", Thread id: {Task.CurrentId})");
              }
            }

            if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Ending worker thread for export for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");
          }, cancellationToken));

        //wait for tasks to finish exporting data for this resolution
        Task.WaitAll(taskPool.ToArray());

        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;
        IsRunning = false;

        //output status message
        if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Mass export complete - Attempted {exportFileCount} files, exported {successCount} files successfully and failed on {failureCount} files (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Export", $"Exported {exportInstrumentCount} files for the selected resolutions");
      });
    }

    private Stack<ExportFile> reverseStack(Stack<ExportFile> input)
    {
      Stack<ExportFile> output = new Stack<ExportFile>();
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
