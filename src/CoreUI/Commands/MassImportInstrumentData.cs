using System.Diagnostics;
using TradeSharp.Data;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Commands;

namespace TradeSharp.CoreUI.Services
{
    /// <summary>
    /// Windows implementation of the mass import of instrument data - use as singleton.
    /// </summary>
    public partial class MassImportInstrumentData : Command, IMassImportInstrumentData
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
    IMassImportInstrumentData.Context m_context;
    IProgressDialog m_progressDialog;

    //properties


    //constructors
    public MassImportInstrumentData() : base()
    {
      m_instrumentService = (IInstrumentService)m_serviceHost.GetService(typeof(IInstrumentService))!;
    }

    //finalizers


    //interface implementations


    //methods
    public override Task StartAsync(IProgressDialog progressDialog, object? context)
    {
      m_progressDialog = progressDialog;
      if (context is not IMassImportInstrumentData.Context m_context)
      {
        State = CommandState.Failed;
        m_progressDialog.LogError("Invalid command context provided");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        State = CommandState.Running;
        try
        {
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
          if (m_context.Settings.ImportStructure == MassImportExportStructure.DiretoriesAndFiles)
            importFiles = scanImportDirectory();
          else if (m_context.Settings.ImportStructure == MassImportExportStructure.FilesOnly)
            importFiles = scanImportFiles();

          //exit if no files were found
          if (importFiles == null || importFiles.Count == 0)
          {
            State = CommandState.Failed;
            m_progressDialog.LogInformation($"No files found to import in \"{m_context.Settings.Directory}\"");
            return;
          }

          //setup progress dialog
          m_progressDialog.Minimum = 0;
          m_progressDialog.Maximum = importFiles!.Count;
          m_progressDialog.Progress = 0;
          m_progressDialog.StatusMessage = $"Found {importFiles.Count} files to import";
          m_progressDialog.ShowAsync();

          m_progressDialog.LogInformation($"Starting mass import for \"{m_context.DataProvider}\" of instrument data for {m_progressDialog.Maximum:G0} files from \"{m_context.Settings.Directory}\"");

          //start the requested set of threads to import the data from the list of files
          List<Task> taskPool = new List<Task>();
          for (int i = 0; i < m_context.Settings.ThreadCount; i++)
            taskPool.Add(
              Task.Run(() =>
              {
                m_progressDialog.LogInformation($"Started worker thread for mass import into data provider tables \"{m_context.DataProvider}\" (Thread id: {Task.CurrentId})");

                //get the instrument bar data service to import files
                IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
                instrumentBarDataService.DataProvider = m_context.DataProvider;
                instrumentBarDataService.MassOperation = true;

                //keep on importing files until the list is empty or the cancellation token is set
                while (importFiles!.Count > 0 && !m_progressDialog.CancellationTokenSource.Token.IsCancellationRequested)
                {
                  ImportFile? importFile = null;
                  lock (importFiles)
                    if (importFiles.Count > 0) importFile = importFiles.Pop();  //only pop if there are items otherwise this will raise an exception
                  if (importFile == null) continue; //failed to find a file to import, import files should be empty

                  //catch any import errors and log them to keep thread going
                  try
                  {
                    m_progressDialog.LogInformation($"Importing \"{importFile.Filename}\" for {importFile.Ticker}, resolution {importFile.Resolution} (Thread id: {Task.CurrentId})");

                    //import the data
                    ImportSettings importSettings = new ImportSettings();
                    importSettings.FromDateTime = m_context.Settings.FromDateTime;
                    importSettings.ToDateTime = m_context.Settings.ToDateTime;
                    importSettings.ReplaceBehavior = m_context.Settings.ReplaceBehavior;
                    importSettings.Filename = importFile.Filename;
                    importSettings.DateTimeTimeZone = m_context.Settings.DateTimeTimeZone;
                    instrumentBarDataService.Resolution = importFile.Resolution;
                    instrumentBarDataService.Instrument = m_instrumentService.GetItem(importFile.Ticker);

                    if (instrumentBarDataService.Instrument != null)
                    {
                      instrumentBarDataService.Import(importSettings);
                      lock (successCountLock) successCount++;
                    }
                    else
                    {
                      m_progressDialog.LogInformation($"Failed to find instrument {importFile.Ticker} definition for \"{importFile.Filename}\" at resolution {importFile.Resolution} (Thread id: {Task.CurrentId})");
                      lock (failureCountLock) failureCount++;
                    }
                  }
                  catch (Exception e)
                  {
                    lock (failureCountLock) failureCount++;
                    m_progressDialog.LogError($"EXCEPTION: Failed to import \"{importFile.Filename}\" for {importFile.Ticker} at resolution {importFile.Resolution} - (Exception: \"{e.Message}\", Thread id: {Task.CurrentId})");
                  }

                  //update progress after importing a file
                  m_progressDialog.Progress = m_progressDialog.Progress + 1;
                }

                m_progressDialog.LogInformation($"Ending worker thread for mass import into data provider tables \"{m_context.DataProvider}\" (Thread id: {Task.CurrentId})");
              }, m_progressDialog.CancellationTokenSource.Token)
            );

          Task.WaitAll(taskPool.ToArray());

          m_progressDialog.Complete = true;
          m_progressDialog.StatusMessage = $"Import complete - {successCount} files successfully imported, {failureCount} failed";

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;
          State = CommandState.Completed;
            
          //output status message
          m_progressDialog.LogInformation($"Mass import complete - Found {m_progressDialog.Maximum:G0} files, imported {successCount} files successfully and failed on {failureCount} files (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
        }
        catch (Exception e)
        {
          State = CommandState.Failed;
          m_progressDialog.LogError($"EXCEPTION: Mass import main thread failed - (Exception: \"{e.Message}\"");
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
      string extension = m_context.Settings.FileType.ToString().ToLower();
      string[] subDirectories = Directory.GetDirectories(m_context.Settings.Directory);

      //get the minute resolution files
      string minuteDirectory = string.Empty;
      if (m_context.Settings.ResolutionMinute)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenMinute), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenMinutes), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenM1), true) == 0)
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
            importFile.Resolution = Resolution.Minutes;
            importFiles.Push(importFile);
          }
        }
      }

      //get the hour resolution files
      string hourDirectory = string.Empty;
      if (m_context.Settings.ResolutionHour)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenHour), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenHours), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenHourly), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenH), true) == 0)
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
            importFile.Resolution = Resolution.Hours;
            importFiles.Push(importFile);
          }
        }
      }

      //get the day resolution files
      string dayDirectory = string.Empty;
      if (m_context.Settings.ResolutionDay)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenDay), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenDays), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenDaily), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenD), true) == 0)
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
            importFile.Resolution = Resolution.Days;
            importFiles.Push(importFile);
          }
        }
      }

      //get the week resolution files
      string weekDirectory = string.Empty;
      if (m_context.Settings.ResolutionWeek)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenWeek), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenWeeks), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenWeekly), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenW), true) == 0)
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
            importFile.Resolution = Resolution.Weeks;
            importFiles.Push(importFile);
          }
        }
      }

      //get the month resolution files
      string monthDirectory = string.Empty;
      if (m_context.Settings.ResolutionMonth)
      {
        foreach (string subDirectory in subDirectories)
          if (string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenMonth), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenMonths), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenMonthly), true) == 0 ||
              string.Compare(subDirectory, Path.Combine(m_context.Settings.Directory, IMassImportInstrumentData.TokenM), true) == 0)
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
            importFile.Resolution = Resolution.Months;
            importFiles.Push(importFile);
          }
        }
      }

      m_progressDialog.LogInformation($"Found minute resolution directory \"{minuteDirectory}\" (Thread id: {Task.CurrentId})");
      m_progressDialog.LogInformation($"Found hour resolution directory \"{hourDirectory}\" (Thread id: {Task.CurrentId})");
      m_progressDialog.LogInformation($"Found day resolution directory \"{dayDirectory}\" (Thread id: {Task.CurrentId})");
      m_progressDialog.LogInformation($"Found week resolution directory \"{weekDirectory}\" (Thread id: {Task.CurrentId})");
      m_progressDialog.LogInformation($"Found month resolution directory \"{monthDirectory}\" (Thread id: {Task.CurrentId})");

      return reverseStack(importFiles);
    }

    private Stack<ImportFile> scanImportFiles()
    {
      Stack<ImportFile> importFiles = new Stack<ImportFile>();
      string extension = m_context.Settings.FileType.ToString().ToLower();

      //get the minute resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] minuteFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenMinute}.{extension}");
      if (minuteFiles.Length == 0) minuteFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenMinute}.{extension}", SearchOption.TopDirectoryOnly);    
      if (minuteFiles.Length == 0) minuteFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenM1}.{extension}", SearchOption.TopDirectoryOnly);    
      if (minuteFiles.Length == 0) minuteFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenM1}.{extension}", SearchOption.TopDirectoryOnly);

      //get the hour resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] hourFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenHour}.{extension}");
      if (hourFiles.Length == 0) hourFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenHour}.{extension}", SearchOption.TopDirectoryOnly);
      if (hourFiles.Length == 0) hourFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenH}.{extension}", SearchOption.TopDirectoryOnly);
      if (hourFiles.Length == 0) hourFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenH}.{extension}", SearchOption.TopDirectoryOnly);

      //get the day resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] dayFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenDay}.{extension}");
      if (dayFiles.Length == 0) dayFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenDay}.{extension}", SearchOption.TopDirectoryOnly);
      if (dayFiles.Length == 0) dayFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenD}.{extension}", SearchOption.TopDirectoryOnly);
      if (dayFiles.Length == 0) dayFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenD}.{extension}", SearchOption.TopDirectoryOnly);

      //get the week resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] weekFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenWeek}.{extension}");
      if (weekFiles.Length == 0) weekFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenWeek}.{extension}", SearchOption.TopDirectoryOnly);
      if (weekFiles.Length == 0) weekFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenW}.{extension}", SearchOption.TopDirectoryOnly);
      if (weekFiles.Length == 0) weekFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenW}.{extension}", SearchOption.TopDirectoryOnly);

      //get the month resolution files, we require consistent naming and exits as soon as we find files matching a naming pattern
      string[] monthFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenMonth}.{extension}");
      if (monthFiles.Length == 0) monthFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenMonth}.{extension}", SearchOption.TopDirectoryOnly);
      if (monthFiles.Length == 0) monthFiles = Directory.GetFiles(m_context.Settings.Directory, $"*_{IMassImportInstrumentData.TokenM}.{extension}", SearchOption.TopDirectoryOnly);
      if (monthFiles.Length == 0) monthFiles = Directory.GetFiles(m_context.Settings.Directory, $"*.{IMassImportInstrumentData.TokenM}.{extension}", SearchOption.TopDirectoryOnly);

      m_progressDialog.LogInformation($"Import file scan found the following files: Minute({minuteFiles.Length}) Hour({hourFiles.Length}) Day({dayFiles.Length}) Week({weekFiles.Length}) Month({monthFiles.Length})");

      //add the files to the import list
      if (m_context.Settings.ResolutionMinute)
        foreach (string filename in minuteFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Minutes;
          importFiles.Push(importFile);
        }

      if (m_context.Settings.ResolutionHour)
        foreach (string filename in hourFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Hours;
          importFiles.Push(importFile);
        }

      if (m_context.Settings.ResolutionDay)
        foreach (string filename in dayFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Days;
          importFiles.Push(importFile);
        }

      if (m_context.Settings.ResolutionWeek)
        foreach (string filename in weekFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Weeks;
          importFiles.Push(importFile);
        }

      if (m_context.Settings.ResolutionMonth)
        foreach (string filename in monthFiles)
        {
          ImportFile importFile = new ImportFile();
          importFile.Ticker = Path.GetFileNameWithoutExtension(filename).Split('.')[0];
          if (importFile.Ticker.Length == 0) continue;  //skip files that don't have a ticker in the filename
          importFile.Filename = filename;
          importFile.Resolution = Resolution.Months;
          importFiles.Push(importFile);
        }

      return reverseStack(importFiles);
    }

  }
}