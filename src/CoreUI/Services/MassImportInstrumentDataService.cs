using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.WinCoreUI.Services
{
  /// <summary>
  /// Windows implementation of the mass import of instrument data - use as singleton.
  /// </summary>
  public partial class MassImportInstrumentDataService : ServiceBase, IMassImportInstrumentDataService
  {
    //constants


    //enums


    //types
    private class ImportFile
    {
      public string Filename { get; set; }
      public string Ticker { get; set; }
      public Resolution Resolution { get; set; }
    }

    //attributes
    IInstrumentService m_instrumentService;
    ILogger<MassImportInstrumentDataService> m_logger;
    ILogger? m_taskLogger;

    //constructors
    public MassImportInstrumentDataService(ILogger<MassImportInstrumentDataService> logger, IDialogService dialogService, IInstrumentService instrumentService) : base(dialogService)
    {
      Settings = new MassImportSettings();
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
    public MassImportSettings Settings { get; set; }
    [ObservableProperty] public bool m_isRunning;

    //methods
    public Task StartAsync(IProgressDialog progressDialog)
    {
      if (DataProvider == string.Empty)
      {
        if (Debugging.MassInstrumentDataImport) m_logger.LogInformation("Failed to start mass import, no data provider was set");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        LoadedState = LoadedState.Loading;
        try
        {
          IsRunning = true;

          object successCountLock = new object();
          int successCount = 0;
          object failureCountLock = new object();
          int failureCount = 0;

          //scan the import directory and construct the set of files to import
          Stopwatch stopwatch = new Stopwatch();
          stopwatch.Start();

          //load the instruments from the cache into the service
          m_instrumentService.Refresh();

          //start mass import
          Stack<ImportFile>? importFiles = null;
          if (Settings.ImportStructure == MassImportExportStructure.DiretoriesAndFiles)
            importFiles = scanImportDirectory();
          else if (Settings.ImportStructure == MassImportExportStructure.FilesOnly)
            importFiles = scanImportFiles();

          //exit if no files were found
          if (importFiles == null || importFiles.Count == 0)
          {
            IsRunning = false;
            if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"No files found to import in \"{Settings.Directory}\"");
            m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Import", $"No files found to import in \"{Settings.Directory}\"");
            return;
          }

          //setup progress dialog
          progressDialog.Minimum = 0;
          progressDialog.Maximum = importFiles!.Count;
          progressDialog.Progress = 0;
          progressDialog.StatusMessage = $"Found {importFiles.Count} files to import";
          progressDialog.ShowAsync();

          if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Starting mass import for \"{DataProvider}\" of instrument data for {progressDialog.Maximum:G0} files from \"{Settings.Directory}\"");

          //start the requested set of threads to import the data from the list of files
          List<Task> taskPool = new List<Task>();
          for (int i = 0; i < Settings.ThreadCount; i++)
            taskPool.Add(
              Task.Run(() =>
              {
                if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Started worker thread for mass import into data provider tables \"{DataProvider}\" (Thread id: {Task.CurrentId})");

                //get the instrument bar data service to import files
                IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
                instrumentBarDataService.DataProvider = DataProvider;
                instrumentBarDataService.MassOperation = true;

                //keep on importing files until the list is empty or the cancellation token is set
                while (importFiles!.Count > 0 && !progressDialog.CancellationTokenSource.Token.IsCancellationRequested)
                {
                  ImportFile? importFile = null;
                  lock (importFiles)
                    if (importFiles.Count > 0) importFile = importFiles.Pop();  //only pop if there are items otherwise this will raise an exception
                  if (importFile == null) continue; //failed to find a file to import, import files should be empty

                  //catch any import errors and log them to keep thread going
                  try
                  {
                    if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Importing \"{importFile.Filename}\" for {importFile.Ticker}, resolution {importFile.Resolution} (Thread id: {Task.CurrentId})");

                    //import the data
                    ImportSettings importSettings = new ImportSettings();
                    importSettings.FromDateTime = Settings.FromDateTime;
                    importSettings.ToDateTime = Settings.ToDateTime;
                    importSettings.ReplaceBehavior = Settings.ReplaceBehavior;
                    importSettings.Filename = importFile.Filename;
                    importSettings.DateTimeTimeZone = Settings.DateTimeTimeZone;
                    instrumentBarDataService.Resolution = importFile.Resolution;
                    instrumentBarDataService.Instrument = m_instrumentService.GetItem(importFile.Ticker);

                    if (instrumentBarDataService.Instrument != null)
                    {
                      instrumentBarDataService.Import(importSettings);
                      lock (successCountLock) successCount++;
                    }
                    else
                    {
                      if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Failed to find instrument {importFile.Ticker} definition for \"{importFile.Filename}\" at resolution {importFile.Resolution} (Thread id: {Task.CurrentId})");
                      lock (failureCountLock) failureCount++;
                    }
                  }
                  catch (Exception e)
                  {
                    lock (failureCountLock) failureCount++;
                    if (Debugging.MassInstrumentDataImport || Debugging.MassInstrumentDataImportException) m_logger.LogError($"EXCEPTION: Failed to import \"{importFile.Filename}\" for {importFile.Ticker} at resolution {importFile.Resolution} - (Exception: \"{e.Message}\", Thread id: {Task.CurrentId})");
                  }

                  //update progress after importing a file
                  progressDialog.Progress = progressDialog.Progress + 1;
                }

                if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Ending worker thread for mass import into data provider tables \"{DataProvider}\" (Thread id: {Task.CurrentId})");
              }, progressDialog.CancellationTokenSource.Token)
            );

          Task.WaitAll(taskPool.ToArray());

          progressDialog.Complete = true;
          progressDialog.StatusMessage = $"Import complete - {successCount} files successfully imported, {failureCount} failed";

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;
          IsRunning = false;

          //output status message
          if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Mass import complete - Found {progressDialog.Maximum:G0} files, imported {successCount} files successfully and failed on {failureCount} files (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Import Complete", $"Found {progressDialog.Maximum:G0} files, imported {successCount} files successfully and failed on {failureCount} files (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
          LoadedState = LoadedState.Loaded;
        }
        catch (Exception e)
        {
          IsRunning = false;
          LoadedState = LoadedState.Error;
          if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"EXCEPTION: Mass import main thread failed - (Exception: \"{e.Message}\"");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "Mass Import Failed", $"Mass import main thread failed - (Exception: \"{e.Message}\"");
        }
      });
    }

    private Stack<ImportFile> reverseStack(Stack<ImportFile> input)
    {
      Stack<ImportFile> output = new Stack<ImportFile>();
      while (input.Count > 0) output.Push(input.Pop());
      return output;
    }

    private Stack<ImportFile> scanImportDirectory()
    {
      Stack<ImportFile> importFiles = new Stack<ImportFile>();
      string extension = Settings.FileType.ToString().ToLower();
      string[] subDirectories = Directory.GetDirectories(Settings.Directory);

      //get the minute resolution files
      string minuteDirectory = string.Empty;
      if (Settings.ResolutionMinute)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenMinute), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenMinutes), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenM1), true) == 0)
          {
            minuteDirectory = subDirectory;
            break;
          }

        if (minuteDirectory != string.Empty)
        {
          string[] minuteFiles = Directory.GetFiles(minuteDirectory, $"*.{extension}");
          foreach (string filename in minuteFiles)
          {
            ImportFile importFile = new ImportFile();
            importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
            if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
            importFile.Filename = filename;
            importFile.Resolution = Resolution.Minute;
            importFiles.Push(importFile);
          }
        }
      }

      //get the hour resolution files
      string hourDirectory = string.Empty;
      if (Settings.ResolutionHour)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenHour), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenHours), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenHourly), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenH), true) == 0)
          {
            hourDirectory = subDirectory;
            break;
          }

        if (hourDirectory != string.Empty)
        {
          string[] hourFiles = Directory.GetFiles(hourDirectory, $"*.{extension}");
          foreach (string filename in hourFiles)
          {
            ImportFile importFile = new ImportFile();
            importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
            if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
            importFile.Filename = filename;
            importFile.Resolution = Resolution.Hour;
            importFiles.Push(importFile);
          }
        }
      }

      //get the day resolution files
      string dayDirectory = string.Empty;
      if (Settings.ResolutionDay)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenDay), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenDays), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenDaily), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenD), true) == 0)
          {
            dayDirectory = subDirectory;
            break;
          }

        if (dayDirectory != string.Empty)
        {
          string[] dayFiles = Directory.GetFiles(dayDirectory, $"*.{extension}");
          foreach (string filename in dayFiles)
          {
            ImportFile importFile = new ImportFile();
            importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
            if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
            importFile.Filename = filename;
            importFile.Resolution = Resolution.Day;
            importFiles.Push(importFile);
          }
        }
      }

      //get the week resolution files
      string weekDirectory = string.Empty;
      if (Settings.ResolutionWeek)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenWeek), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenWeeks), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenWeekly), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenW), true) == 0)
          {
            weekDirectory = subDirectory;
            break;
          }

        if (weekDirectory != string.Empty)
        {
          string[] weekFiles = Directory.GetFiles(weekDirectory, $"*.{extension}");
          foreach (string filename in weekFiles)
          {
            ImportFile importFile = new ImportFile();
            importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
            if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
            importFile.Filename = filename;
            importFile.Resolution = Resolution.Week;
            importFiles.Push(importFile);
          }
        }
      }

      //get the month resolution files
      string monthDirectory = string.Empty;
      if (Settings.ResolutionMonth)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenMonth), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenMonths), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenMonthly), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(Settings.Directory, IMassImportInstrumentDataService.TokenM), true) == 0)
          {
            monthDirectory = subDirectory;
            break;
          }

        if (monthDirectory != string.Empty)
        {
          string[] monthFiles = Directory.GetFiles(monthDirectory, $"*.{extension}");
          foreach (string filename in monthFiles)
          {
            ImportFile importFile = new ImportFile();
            importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
            if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
            importFile.Filename = filename;
            importFile.Resolution = Resolution.Month;
            importFiles.Push(importFile);
          }
        }
      }

      if (Debugging.MassInstrumentDataImport)
      {
        m_logger.LogInformation($"Found minute resolution directory \"{minuteDirectory}\" (Thread id: {Task.CurrentId})");
        m_logger.LogInformation($"Found hour resolution directory \"{hourDirectory}\" (Thread id: {Task.CurrentId})");
        m_logger.LogInformation($"Found day resolution directory \"{dayDirectory}\" (Thread id: {Task.CurrentId})");
        m_logger.LogInformation($"Found week resolution directory \"{weekDirectory}\" (Thread id: {Task.CurrentId})");
        m_logger.LogInformation($"Found month resolution directory \"{monthDirectory}\" (Thread id: {Task.CurrentId})");
      }

      return reverseStack(importFiles);
    }

    private Stack<ImportFile> scanImportFiles()
    {
      Stack<ImportFile> importFiles = new Stack<ImportFile>();
      string extension = Settings.FileType.ToString().ToLower();

      //get the minute resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] minuteFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenMinute}.{extension}");
      if (minuteFiles.Length == 0) minuteFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenMinute}.{extension}", SearchOption.TopDirectoryOnly);    
      if (minuteFiles.Length == 0) minuteFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenM1}.{extension}", SearchOption.TopDirectoryOnly);    
      if (minuteFiles.Length == 0) minuteFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenM1}.{extension}", SearchOption.TopDirectoryOnly);

      //get the hour resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] hourFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenHour}.{extension}");
      if (hourFiles.Length == 0) hourFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenHour}.{extension}", SearchOption.TopDirectoryOnly);
      if (hourFiles.Length == 0) hourFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenH}.{extension}", SearchOption.TopDirectoryOnly);
      if (hourFiles.Length == 0) hourFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenH}.{extension}", SearchOption.TopDirectoryOnly);

      //get the day resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] dayFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenDay}.{extension}");
      if (dayFiles.Length == 0) dayFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenDay}.{extension}", SearchOption.TopDirectoryOnly);
      if (dayFiles.Length == 0) dayFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenD}.{extension}", SearchOption.TopDirectoryOnly);
      if (dayFiles.Length == 0) dayFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenD}.{extension}", SearchOption.TopDirectoryOnly);

      //get the week resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] weekFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenWeek}.{extension}");
      if (weekFiles.Length == 0) weekFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenWeek}.{extension}", SearchOption.TopDirectoryOnly);
      if (weekFiles.Length == 0) weekFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenW}.{extension}", SearchOption.TopDirectoryOnly);
      if (weekFiles.Length == 0) weekFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenW}.{extension}", SearchOption.TopDirectoryOnly);

      //get the month resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] monthFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenMonth}.{extension}");
      if (monthFiles.Length == 0) monthFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenMonth}.{extension}", SearchOption.TopDirectoryOnly);
      if (monthFiles.Length == 0) monthFiles = Directory.GetFiles(Settings.Directory, $"*_{IMassImportInstrumentDataService.TokenM}.{extension}", SearchOption.TopDirectoryOnly);
      if (monthFiles.Length == 0) monthFiles = Directory.GetFiles(Settings.Directory, $"*.{IMassImportInstrumentDataService.TokenM}.{extension}", SearchOption.TopDirectoryOnly);

      if (Debugging.MassInstrumentDataImport) m_logger.LogInformation($"Import file scan found the following files: Minute({minuteFiles.Length}) Hour({hourFiles.Length}) Day({dayFiles.Length}) Week({weekFiles.Length}) Month({monthFiles.Length})");

      //add the files to the import list
      if (Settings.ResolutionMinute)
        foreach (string filename in minuteFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Minute;
          importFiles.Push(importFile);
        }

      if (Settings.ResolutionHour)
        foreach (string filename in hourFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Hour;
          importFiles.Push(importFile);
        }

      if (Settings.ResolutionDay)
        foreach (string filename in dayFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Day;
          importFiles.Push(importFile);
        }

      if (Settings.ResolutionWeek)
        foreach (string filename in weekFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Week;
          importFiles.Push(importFile);
        }

      if (Settings.ResolutionMonth)
        foreach (string filename in monthFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Month;
          importFiles.Push(importFile);
        }

      return reverseStack(importFiles);
    }

  }
}