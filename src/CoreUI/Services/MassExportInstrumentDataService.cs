using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using System.Diagnostics;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Windows implementation of the mass export of instrument data - use as singleton.
  /// </summary>
  public partial class MassExportInstrumentDataService : ServiceBase, IMassExportInstrumentDataService
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
      DataProvider = string.Empty;
      m_instrumentService = instrumentService;
    }

    //finalizers


    //interface implementations


    //properties
    public string DataProvider { get; set; }
    public ILogger? Logger { get => m_taskLogger; set => m_taskLogger = value; }
    public MassExportSettings Settings { get; set; }
    [ObservableProperty] public bool m_isRunning;

    //methods
    public Task StartAsync(IProgressDialog progressDialog)
    {
      if (DataProvider == string.Empty)
      {
        m_logger.LogInformation("Failed to start mass export, no data provider was set");
        progressDialog.LogInformation("Failed to start mass export, no data provider was set");
        return Task.CompletedTask;
      }

      if (IsRunning)
      {
        m_logger.LogInformation("Mass export already running, returning from mass export");
        progressDialog.LogInformation("Mass export already running, returning from mass export");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        LoadedState = LoadedState.Loading;
        try
        {
          IsRunning = true;
          Stopwatch stopwatch = new Stopwatch();
          stopwatch.Start();

          //load instruments from the cache into the instrument service
          m_instrumentService.Refresh();

          //draw up the set of instruments to export in a stack
          progressDialog.LogInformation($"Starting mass export of instrument data for {m_instrumentService.Items.Count} instruments");

          //create export directories if they don't exist
          string parentDirectory = Settings.Directory;
          if (!Directory.Exists(Settings.Directory))
          {
            progressDialog.LogInformation($"Creating export directory \"{Settings.Directory}\"");
            Directory.CreateDirectory(Settings.Directory);
          }

          if (Settings.ExportStructure == MassImportExportStructure.DiretoriesAndFiles)
          {
            string directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenMinute}";
            if (Settings.ResolutionMinute && !Directory.Exists(directory))
            {
              progressDialog.LogInformation($"Creating export directory \"{directory}\"");
              Directory.CreateDirectory(directory);
            }

            directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenHour}";
            if (Settings.ResolutionHour && !Directory.Exists(directory))
            {
              progressDialog.LogInformation($"Creating export directory \"{directory}\"");
              Directory.CreateDirectory(directory);
            }

            directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenDay}";
            if (Settings.ResolutionDay && !Directory.Exists(directory))
            {
              progressDialog.LogInformation($"Creating export directory \"{directory}\"");
              Directory.CreateDirectory(directory);
            }

            directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenWeek}";
            if (Settings.ResolutionWeek && !Directory.Exists(directory))
            {
              progressDialog.LogInformation($"Creating export directory \"{directory}\"");
              Directory.CreateDirectory(directory);
            }

            directory = $"{Settings.Directory}\\{IMassExportInstrumentDataService.TokenMonth}";
            if (Settings.ResolutionMonth && !Directory.Exists(directory))
            {
              progressDialog.LogInformation($"Creating export directory \"{directory}\"");
              Directory.CreateDirectory(directory);
            }
          }

          //construct the set of files to be exported
          Stack<ExportFile> exportFileList = new Stack<ExportFile>();
          if (Settings.ResolutionMinute)
            foreach (Instrument instrument in m_instrumentService.Items)
            {
              ExportFile exportFile = new ExportFile();
              exportFile.Filename = getExportFileName(Resolution.Minutes, instrument);
              exportFile.Resolution = Resolution.Minutes;
              exportFile.Instrument = instrument;
              exportFileList.Push(exportFile);
            }

          if (Settings.ResolutionHour)
            foreach (Instrument instrument in m_instrumentService.Items)
            {
              ExportFile exportFile = new ExportFile();
              exportFile.Filename = getExportFileName(Resolution.Hours, instrument);
              exportFile.Resolution = Resolution.Hours;
              exportFile.Instrument = instrument;
              exportFileList.Push(exportFile);
            }

          if (Settings.ResolutionDay)
            foreach (Instrument instrument in m_instrumentService.Items)
            {
              ExportFile exportFile = new ExportFile();
              exportFile.Filename = getExportFileName(Resolution.Days, instrument);
              exportFile.Resolution = Resolution.Days;
              exportFile.Instrument = instrument;
              exportFileList.Push(exportFile);
            }

          if (Settings.ResolutionWeek)
            foreach (Instrument instrument in m_instrumentService.Items)
            {
              ExportFile exportFile = new ExportFile();
              exportFile.Filename = getExportFileName(Resolution.Weeks, instrument);
              exportFile.Resolution = Resolution.Weeks;
              exportFile.Instrument = instrument;
              exportFileList.Push(exportFile);
            }

          if (Settings.ResolutionMonth)
            foreach (Instrument instrument in m_instrumentService.Items)
            {
              ExportFile exportFile = new ExportFile();
              exportFile.Filename = getExportFileName(Resolution.Months, instrument);
              exportFile.Resolution = Resolution.Months;
              exportFile.Instrument = instrument;
              exportFileList.Push(exportFile);
            }

          exportFileList = reverseStack(exportFileList);

          //setup progress dialog
          progressDialog.Minimum = 0;
          progressDialog.Maximum = exportFileList!.Count;
          progressDialog.Progress = 0;
          progressDialog.StatusMessage = $"Found {exportFileList.Count} instruments to export";
          progressDialog.ShowAsync();

          //export all the data according to the defined data resolutions
          object attemptedFileCountLock = new object();
          int attemptedFileCount = 0;
          object successCountLock = new object();
          int successCount = 0;
          object failureCountLock = new object();
          int failureCount = 0;

          progressDialog.LogInformation($"Starting mass export for \"{DataProvider}\" of instrument data, constructed {exportFileList.Count} files (look at attempted count for actual number of files output)");

          List<Task> taskPool = new List<Task>();
          for (int i = 0; i < Settings.ThreadCount; i++)
            taskPool.Add(
            Task.Run(() =>
            {
              progressDialog.LogInformation($"Started worker thread for export for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");

              IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
              instrumentBarDataService.DataProvider = DataProvider;
              instrumentBarDataService.MassOperation = true;

              while (exportFileList.Count > 0 && !progressDialog.CancellationTokenSource.Token.IsCancellationRequested)
              {
                //pop an instrument off the stack and export the data
                ExportFile? exportFile = null;
                lock (exportFileList)
                  if (exportFileList.Count > 0) exportFile = exportFileList.Pop();  //only pop if there are instruments otherwise this will raise an exception
                if (exportFile == null) continue; //failed to find a an instrument to export, stack should be empty should be empty

                instrumentBarDataService.Resolution = exportFile.Resolution;
                instrumentBarDataService.Instrument = exportFile.Instrument;
                instrumentBarDataService.Refresh(Settings.FromDateTime, Settings.ToDateTime);

                //skip creation of empty files if no data within the given range
                if (!Settings.CreateEmptyFiles && instrumentBarDataService.Items.Count == 0)
                {
                  progressDialog.LogInformation($"No data in range for {exportFile.Instrument.Ticker} at resolution {exportFile.Resolution}, skipping creation of file \"{exportFile.Filename}\" (Thread id: {Task.CurrentId})");
                  continue;
                }

                progressDialog.LogInformation($"Exporting {exportFile.Instrument.Ticker} for resolution {exportFile.Resolution} to file \"{exportFile.Filename}\" (Thread id: {Task.CurrentId})");

                try
                {
                  lock (attemptedFileCountLock) attemptedFileCount++;
                  ExportSettings exportSettings = new ExportSettings();
                  exportSettings.FromDateTime = Settings.FromDateTime;
                  exportSettings.ToDateTime = Settings.ToDateTime;
                  exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
                  exportSettings.Filename = exportFile.Filename;
                  exportSettings.DateTimeTimeZone = Settings.DateTimeTimeZone;
                  instrumentBarDataService.Export(exportSettings);
                  lock (successCountLock) successCount++;
                }
                catch (Exception e)
                {
                  lock (failureCountLock) failureCount++;
                  progressDialog.LogError($"EXCEPTION: Failed to export \"{exportFile.Filename}\" for {exportFile.Instrument.Ticker} at resolution {exportFile.Resolution} - (Exception: \"{e.Message}\", Thread id: {Task.CurrentId})");
                }

                //update progress after exporting a file
                progressDialog.Progress = progressDialog.Progress + 1;
              }

              progressDialog.LogInformation($"Ending worker thread for export for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");
            }, progressDialog.CancellationTokenSource.Token));

          //wait for tasks to finish exporting data for this resolution
          Task.WaitAll(taskPool.ToArray());

          progressDialog.Complete = true;
          progressDialog.StatusMessage = $"Export complete - {successCount} files successfully exported, {failureCount} failed";

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;
          IsRunning = false;

          //output status message
          progressDialog.LogInformation($"Mass Export Complete - Attempted {attemptedFileCount} files, exported {successCount} files successfully and failed on {failureCount} files (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Export Complete", $"Attempted {attemptedFileCount} files, exported {successCount} files successfully and failed on {failureCount} files (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
          LoadedState = LoadedState.Loaded;
        }
        catch (Exception e)
        {
          IsRunning = false;
          LoadedState = LoadedState.Error;
          progressDialog.LogError($"EXCEPTION: Mass export main thread failed - (Exception: \"{e.Message}\")");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "Mass Export Failed", $"Mass export main thread failed - (Exception: \"{e.Message}\"");
        }
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

      //NOTE: This normaliation will misalign the ticker used in the filename with the actual ticker in the data file,
      //      the Tag on the instrument should be used to align the ticker in the data file.
      string normalizedTicker = TradeSharp.Common.Utilities.SafeFileName(instrument.Ticker);

      switch (Settings.ExportStructure)
      {
        case MassImportExportStructure.DiretoriesAndFiles:          
          switch (resolution)
          {
            case Resolution.Minutes:
              resolutionStr = IMassExportInstrumentDataService.TokenMinute;
              break;
            case Resolution.Hours:
              resolutionStr = IMassExportInstrumentDataService.TokenHour;
              break;
            case Resolution.Days:
              resolutionStr = IMassExportInstrumentDataService.TokenDay;
              break;
            case Resolution.Weeks:
              resolutionStr = IMassExportInstrumentDataService.TokenWeek;
              break;
            case Resolution.Months:
              resolutionStr = IMassExportInstrumentDataService.TokenMonth;
              break;
          }

          filename = $"{Settings.Directory}\\{resolutionStr}\\{normalizedTicker}.{extension}";
          break;
        case MassImportExportStructure.FilesOnly:
          filename = $"{Settings.Directory}\\{normalizedTicker}_{resolution}.{extension}";
          break;
      }
      return filename;
    }
  }
}
