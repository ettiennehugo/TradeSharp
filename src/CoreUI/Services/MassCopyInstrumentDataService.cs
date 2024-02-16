using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  public partial class MassCopyInstrumentDataService : ServiceBase, IMassCopyInstrumentDataService
  {
    //constants


    //enums


    //types
    private class CopyInstrument
    {
      public Resolution Resolution { get; set; }
      public Instrument Instrument { get; set; }
    }

    //attributes
    IDatabase m_database;
    IInstrumentService m_instrumentService;
    ILogger<MassExportInstrumentDataService> m_logger;
    ILogger? m_taskLogger;

    //constructors
    public MassCopyInstrumentDataService(ILogger<MassExportInstrumentDataService> logger, IDialogService dialogService, IInstrumentService instrumentService, IDatabase database) : base(dialogService)
    {
      Settings = new MassCopySettings();
      m_logger = logger;
      IsRunning = false;
      DataProvider = string.Empty;
      m_instrumentService = instrumentService;
      m_database = database;
    }

    //finalizers


    //interface implementations


    //properties
    public string DataProvider { get; set; }
    public ILogger? Logger { get => m_taskLogger; set => m_taskLogger = value; }
    public MassCopySettings Settings { get; set; }
    [ObservableProperty] public bool m_isRunning;

    //methods
    public Task StartAsync(IProgressDialog progressDialog)
    {
      if (DataProvider == string.Empty)
      {
        if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation("Failed to start mass copy, no data provider was set");
        return Task.CompletedTask;
      }

      if (IsRunning)
      {
        if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation("Mass copy already running, returning from mass export");
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        try 
        {
          IsRunning = true;
          Stopwatch stopwatch = new Stopwatch();
          stopwatch.Start();

          //construct the list of instruments to copy
          //NOTE: We need to start from lower timeframes and work our way up to higher timeframes to ensure that we have the lower timeframes to build the higher timeframes from.
          if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Starting mass copy of instrument data for {m_instrumentService.Items.Count} instruments");

          Stack<CopyInstrument> instrumentList = new Stack<CopyInstrument>();
          if (Settings.ResolutionHour)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (m_database.GetDataCount(DataProvider, instrument.Id, instrument.Ticker, Resolution.Minute, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Minute;
                copyInstrument.Instrument = instrument;
                instrumentList.Push(copyInstrument);
              }

          if (Settings.ResolutionDay)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (m_database.GetDataCount(DataProvider, instrument.Id, instrument.Ticker, Resolution.Hour, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Hour;
                copyInstrument.Instrument = instrument;
                instrumentList.Push(copyInstrument);
              }

          if (Settings.ResolutionWeek)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (m_database.GetDataCount(DataProvider, instrument.Id, instrument.Ticker, Resolution.Day, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Day;
                copyInstrument.Instrument = instrument;
                instrumentList.Push(copyInstrument);
              }

          if (Settings.ResolutionMonth)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (m_database.GetDataCount(DataProvider, instrument.Id, instrument.Ticker, Resolution.Week, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Week;
                copyInstrument.Instrument = instrument;
                instrumentList.Push(copyInstrument);
              }

          instrumentList = reverseStack(instrumentList);
          
          //output status message and return if there are no instruments to copy
          if (instrumentList.Count == 0)
          {
            IsRunning = false;
            if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation("No instruments found to copy for the given settings");
            m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Copy", "No instruments found to copy for the given settings");
            return;
          }

          //setup progress dialog
          progressDialog.Minimum = 0;
          progressDialog.Maximum = instrumentList!.Count;
          progressDialog.Progress = 0;
          progressDialog.StatusMessage = $"Found {instrumentList.Count} instrument/resolution combinations with data to copy";
          progressDialog.ShowAsync();

          //copy all the instrument data according to the defined data resolutions
          object attemptedInstrumentCountLock = new object();
          int attemptedInstrumentCount = 0;
          object successCountLock = new object();
          int successCount = 0;
          object failureCountLock = new object();
          int failureCount = 0;

          if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Starting mass copy for \"{DataProvider}\" of instrument data, constructed {instrumentList.Count} instrument/resolution pairs (look at attempted count for actual number of instruments copied)");

          List<Task> taskPool = new List<Task>();
          for (int i = 0; i < Settings.ThreadCount; i++)
            taskPool.Add(
            Task.Run(() =>
            {
              if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Started worker thread for copy instrument data for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");

              IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
              instrumentBarDataService.DataProvider = DataProvider;
              instrumentBarDataService.MassOperation = true;

              while (instrumentList.Count > 0 && !progressDialog.CancellationTokenSource.IsCancellationRequested)
              {
                //pop and instrument/resolution pair off the stack to porcess
                CopyInstrument? copyInstrument = null;
                lock (instrumentList)
                  if (instrumentList.Count > 0) copyInstrument = instrumentList.Pop();
                progressDialog.Progress = progressDialog.Progress + 1;

                if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Copying instrument data for \"{copyInstrument!.Instrument.Ticker}\" to resolution \"{copyInstrument!.Resolution}\"");

                instrumentBarDataService.Resolution = copyInstrument!.Resolution;
                instrumentBarDataService.Instrument = copyInstrument!.Instrument;

                try
                {
                  lock (attemptedInstrumentCountLock) attemptedInstrumentCount++;
                  instrumentBarDataService.Copy(copyInstrument!.Resolution, Settings.FromDateTime, Settings.ToDateTime);
                  lock (successCountLock) successCount++;
                }
                catch (Exception e)
                {
                  lock (failureCountLock) failureCount++;
                  if (Debugging.MassInstrumentDataCopy) m_logger.LogError($"EXCEPTION: Failed to copy instrument data for \"{copyInstrument.Instrument.Ticker}\" at resolution \"{copyInstrument.Resolution}\" (Exception: \"{e.Message}\")");
                }

                lock (attemptedInstrumentCountLock) attemptedInstrumentCount++;
              }

              if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Ending worker thread for copy of instrument data for data provider \"{DataProvider}\" (Thread id: {Task.CurrentId})");
            }, progressDialog.CancellationTokenSource.Token));

          //wait for tasks to finish exporting data for this resolution
          Task.WaitAll(taskPool.ToArray());

          progressDialog.Complete = true;
          progressDialog.StatusMessage = $"Copy complete - {successCount} instrument/resolutions successfully copied, {failureCount} failed";

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;
          IsRunning = false;

          //output status message
          if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Mass Copy Complete - Attempted {attemptedInstrumentCount} instruments, copied {successCount} instruments successfully and failed on {failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Copy Complete", $"Attempted {attemptedInstrumentCount} instruments, copied {successCount} instruments successfully and failed on {failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
        }
        catch (Exception e)
        {
          IsRunning = false;
          if (Debugging.MassInstrumentDataCopy) m_logger.LogError($"EXCEPTION: Mass copy main thread failed - (Exception: \"{e.Message}\")");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "Mass Copy Failed", $"Mass copy main thread failed - (Exception: \"{e.Message}\"");
        }
      });
    }

    private Stack<CopyInstrument> reverseStack(Stack<CopyInstrument> input)
    {
      Stack<CopyInstrument> output = new Stack<CopyInstrument>();
      while (input.Count > 0) output.Push(input.Pop());
      return output;
    }
  }
}
