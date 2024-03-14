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
    object m_attemptedInstrumentCountLock;
    int m_attemptedInstrumentCount;
    object m_successCountLock;
    int m_successCount;
    object m_failureCountLock;
    int m_failureCount;

    //constructors
    public MassCopyInstrumentDataService(ILogger<MassExportInstrumentDataService> logger, IDialogService dialogService, IInstrumentService instrumentService, IDatabase database) : base(dialogService)
    {
      Settings = new MassCopySettings();
      m_logger = logger;
      IsRunning = false;
      DataProvider = string.Empty;
      m_instrumentService = instrumentService;
      m_database = database;
      m_attemptedInstrumentCountLock = new object();
      m_attemptedInstrumentCount = 0;
      m_successCountLock = new object();
      m_successCount = 0;
      m_failureCountLock = new object();
      m_failureCount = 0;
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

          //reinitialize instance variables
          m_attemptedInstrumentCount = 0;
          m_successCountLock = new object();
          m_successCount = 0;
          m_failureCountLock = new object();
          m_failureCount = 0;

          //construct the list of instruments to copy
          //NOTE:
          // * We need to start from lower timeframes and work our way up to higher timeframes to ensure that we have the lower timeframes to build the higher timeframes from.
          // * We only add instruments that are going to be copied OR instruments that have data on the database to copy to the longer timeframes.
          if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Starting mass copy of instrument data for {m_instrumentService.Items.Count} instruments");

          Stack<CopyInstrument> minuteToHourList = new Stack<CopyInstrument>();
          Stack<CopyInstrument> hourToDayList = new Stack<CopyInstrument>();
          Stack<CopyInstrument> dayToWeekList = new Stack<CopyInstrument>();
          Stack<CopyInstrument> weekToMonthList = new Stack<CopyInstrument>();

          if (Settings.ResolutionHour)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (m_database.GetDataCount(DataProvider, instrument.Ticker, Resolution.Minute, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Minute;
                copyInstrument.Instrument = instrument;
                minuteToHourList.Push(copyInstrument);
              }

          if (Settings.ResolutionDay)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (minuteToHourList.FirstOrDefault(x => x.Instrument == instrument) != null ||    //going to construct the hourly bars from the minute bars
                  m_database.GetDataCount(DataProvider, instrument.Ticker, Resolution.Hour, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Hour;
                copyInstrument.Instrument = instrument;
                hourToDayList.Push(copyInstrument);
              }

          if (Settings.ResolutionWeek)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (hourToDayList.FirstOrDefault(x => x.Instrument == instrument) != null ||    //going to construct the daily bars from the hourly bars
                  m_database.GetDataCount(DataProvider, instrument.Ticker, Resolution.Day, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Day;
                copyInstrument.Instrument = instrument;
                dayToWeekList.Push(copyInstrument);
              }

          if (Settings.ResolutionMonth)
            foreach (Instrument instrument in m_instrumentService.Items)
              if (dayToWeekList.FirstOrDefault(x => x.Instrument == instrument) != null ||   //going to construct the weekly bars from the daily bars
                  m_database.GetDataCount(DataProvider, instrument.Ticker, Resolution.Week, Settings.FromDateTime, Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Week;
                copyInstrument.Instrument = instrument;
                weekToMonthList.Push(copyInstrument);
              }

          minuteToHourList = reverseStack(minuteToHourList);
          hourToDayList = reverseStack(hourToDayList);
          dayToWeekList = reverseStack(dayToWeekList);
          weekToMonthList = reverseStack(weekToMonthList);

          //output status message and return if there are no instruments to copy
          int totalToCopyCount = minuteToHourList.Count + hourToDayList.Count + dayToWeekList.Count + weekToMonthList.Count;
          if (totalToCopyCount == 0)
          {
            IsRunning = false;
            if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation("No instruments found to copy for the given settings");
            m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Copy", "No instruments found to copy for the given settings");
            return;
          }

          //setup progress dialog
          progressDialog.Minimum = 0;
          progressDialog.Maximum = totalToCopyCount;
          progressDialog.Progress = 0;
          progressDialog.StatusMessage = $"Found {totalToCopyCount} instrument/resolution combinations with data to copy";
          progressDialog.ShowAsync();

          //copy all the instrument data according to the defined data resolutions
          //NOTE: We wait to for the copletion of one resolution before starting the next resolution to ensure that the lower timeframes are available to construct the higher timeframes
          if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Starting mass copy for \"{DataProvider}\" of instrument data, constructed {totalToCopyCount} instrument/resolution pairs (look at attempted count for actual number of instruments copied)");

          copyInstrumentData(Resolution.Minute, minuteToHourList, progressDialog);
          copyInstrumentData(Resolution.Hour, hourToDayList, progressDialog);
          copyInstrumentData(Resolution.Day, dayToWeekList, progressDialog);
          copyInstrumentData(Resolution.Week, weekToMonthList, progressDialog);

          progressDialog.Complete = true;
          progressDialog.StatusMessage = $"Copy complete - {m_successCount} instrument/resolutions successfully copied, {m_failureCount} failed";

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;
          IsRunning = false;

          //output status message
          if (Debugging.MassInstrumentDataExport) m_logger.LogInformation($"Mass Copy Complete - Attempted {m_attemptedInstrumentCount} instruments, copied {m_successCount} instruments successfully and failed on {m_failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "Mass Copy Complete", $"Attempted {m_attemptedInstrumentCount} instruments, copied {m_successCount} instruments successfully and failed on {m_failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
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

    private void copyInstrumentData(Resolution fromResolution, Stack<CopyInstrument> list, IProgressDialog progressDialog)
    {
      if (list.Count == 0) return;

      List<Task> taskPool = new List<Task>();
      for (int i = 0; i < Settings.ThreadCount; i++)
        taskPool.Add(
          Task.Run(() =>
          {
            if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Started worker thread for copy instrument data for data provider \"{DataProvider}\" from resolution {fromResolution} to resolution {fromResolution + 1} (Thread id: {Task.CurrentId})");

            IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
            instrumentBarDataService.DataProvider = DataProvider;
            instrumentBarDataService.MassOperation = true;

            while (list.Count > 0 && !progressDialog.CancellationTokenSource.IsCancellationRequested)
            {
              //pop an instrument/resolution pair off the stack to porcess
              CopyInstrument? copyInstrument = null;
              lock (list)
                if (list.Count > 0) copyInstrument = list.Pop();
              if (copyInstrument == null) continue; //failed to find a copy/resolution entry, all instruments processed

              if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Copying instrument data for \"{copyInstrument!.Instrument.Ticker}\" to resolution \"{copyInstrument!.Resolution}\"");

              instrumentBarDataService.Resolution = copyInstrument!.Resolution;
              instrumentBarDataService.Instrument = copyInstrument!.Instrument;

              try
              {
                lock (m_attemptedInstrumentCountLock) m_attemptedInstrumentCount++;
                instrumentBarDataService.Copy(copyInstrument!.Resolution, Settings.FromDateTime, Settings.ToDateTime);
                lock (m_successCountLock) m_successCount++;
              }
              catch (Exception e)
              {
                lock (m_failureCountLock) m_failureCount++;
                if (Debugging.MassInstrumentDataCopy) m_logger.LogError($"EXCEPTION: Failed to copy instrument data for \"{copyInstrument.Instrument.Ticker}\" at resolution \"{copyInstrument.Resolution}\" (Exception: \"{e.Message}\")");
              }

              lock (m_attemptedInstrumentCountLock) m_attemptedInstrumentCount++;

              //update progress after copying instrument data
              progressDialog.Progress = progressDialog.Progress + 1;
            }

            if (Debugging.MassInstrumentDataCopy) m_logger.LogInformation($"Ending worker thread for copy of instrument data for data provider \"{DataProvider}\" from resolution {fromResolution} to resolution {fromResolution + 1} (Thread id: {Task.CurrentId})");
          }, progressDialog.CancellationTokenSource.Token));

      //wait for tasks to finish copying data for this resolution
      Task.WaitAll(taskPool.ToArray());
    }
  }
}
