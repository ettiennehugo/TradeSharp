using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TradeSharp.CoreUI.Commands;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
    public partial class MassCopyInstrumentData: Command, IMassCopyInstrumentData
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
    IMassCopyInstrumentData.Context m_context;
    IDatabase m_database;
    ILogger? m_taskLogger;
    object m_attemptedInstrumentCountLock;
    int m_attemptedInstrumentCount;
    object m_successCountLock;
    int m_successCount;
    object m_failureCountLock;
    int m_failureCount;

    //properties

    //constructors
    public MassCopyInstrumentData() : base()
    {
      m_database = (IDatabase)m_serviceHost.GetService(typeof(IDatabase))!;
      m_attemptedInstrumentCountLock = new object();
      m_attemptedInstrumentCount = 0;
      m_successCountLock = new object();
      m_successCount = 0;
      m_failureCountLock = new object();
      m_failureCount = 0;
    }

    //finalizers


    //interface implementations


    //methods
    public override Task StartAsync(IProgressDialog progressDialog, object? context)
    {

      if (context is not IMassCopyInstrumentData.Context m_context)
      {
        progressDialog.LogError("Failed to start mass copy, invalid context provided");
        State = CommandState.Failed;
        return Task.CompletedTask;
      }
      
      if (m_context.Instruments.Count == 0) 
      {
        progressDialog.LogError("Failed to start mass copy, no instruments selected");
        State = CommandState.Failed;
        return Task.CompletedTask;
      }

      return Task.Run(() =>
      {
        try 
        {
          State = CommandState.Running;

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
          progressDialog.LogInformation($"Starting mass copy of instrument data for {m_context.Instruments.Count} instruments");

          Stack<CopyInstrument> minuteToHourList = new Stack<CopyInstrument>();
          Stack<CopyInstrument> hourToDayList = new Stack<CopyInstrument>();
          Stack<CopyInstrument> dayToWeekList = new Stack<CopyInstrument>();
          Stack<CopyInstrument> weekToMonthList = new Stack<CopyInstrument>();

          if (m_context.Settings.ResolutionHour)
            foreach (Instrument instrument in m_context.Instruments)
              if (m_database.GetDataCount(m_context.Settings.DataProvider, instrument.Ticker, Resolution.Minutes, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Minutes;
                copyInstrument.Instrument = instrument;
                minuteToHourList.Push(copyInstrument);
              }

          if (m_context.Settings.ResolutionDay)
            foreach (Instrument instrument in m_context.Instruments)
              if (minuteToHourList.FirstOrDefault(x => x.Instrument == instrument) != null ||    //going to construct the hourly bars from the minute bars
                  m_database.GetDataCount(m_context.Settings.DataProvider, instrument.Ticker, Resolution.Hours, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Hours;
                copyInstrument.Instrument = instrument;
                hourToDayList.Push(copyInstrument);
              }

          if (m_context.Settings.ResolutionWeek)
            foreach (Instrument instrument in m_context.Instruments)
              if (hourToDayList.FirstOrDefault(x => x.Instrument == instrument) != null ||    //going to construct the daily bars from the hourly bars
                  m_database.GetDataCount(m_context.Settings.DataProvider, instrument.Ticker, Resolution.Days, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Days;
                copyInstrument.Instrument = instrument;
                dayToWeekList.Push(copyInstrument);
              }

          if (m_context.Settings.ResolutionMonth)
            foreach (Instrument instrument in m_context.Instruments)
              if (dayToWeekList.FirstOrDefault(x => x.Instrument == instrument) != null ||   //going to construct the weekly bars from the daily bars
                  m_database.GetDataCount(m_context.Settings.DataProvider, instrument.Ticker, Resolution.Weeks, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime) > 0)
              {
                CopyInstrument copyInstrument = new CopyInstrument();
                copyInstrument.Resolution = Resolution.Weeks;
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
            State = CommandState.Completed;
            progressDialog.LogInformation("No instruments found to copy for the given m_context.Settings");
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
          progressDialog.LogInformation($"Starting mass copy for \"{m_context.DataProvider}\" of instrument data, constructed {totalToCopyCount} instrument/resolution pairs (look at attempted count for actual number of instruments copied)");

          copyInstrumentData(Resolution.Minutes, minuteToHourList, progressDialog);
          copyInstrumentData(Resolution.Hours, hourToDayList, progressDialog);
          copyInstrumentData(Resolution.Days, dayToWeekList, progressDialog);
          copyInstrumentData(Resolution.Weeks, weekToMonthList, progressDialog);

          progressDialog.Complete = true;
          progressDialog.StatusMessage = $"Copy complete - {m_successCount} instrument/resolutions successfully copied, {m_failureCount} failed";

          stopwatch.Stop();
          TimeSpan elapsed = stopwatch.Elapsed;
          State = CommandState.Completed;

          //output status message
          progressDialog.LogInformation($"Mass Copy Complete - Attempted {m_attemptedInstrumentCount} instruments, copied {m_successCount} instruments successfully and failed on {m_failureCount} instruments (Elapsed time: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3})");
        }
        catch (Exception e)
        {
          State = CommandState.Failed;
          progressDialog.LogError($"EXCEPTION: Mass copy main thread failed - (Exception: \"{e.Message}\")");
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
      for (int i = 0; i < m_context.Settings.ThreadCount; i++)
        taskPool.Add(
          Task.Run(() =>
          {
            progressDialog.LogInformation($"Started worker thread for copy instrument data for data provider \"{m_context.Settings.DataProvider}\" from resolution {fromResolution} to resolution {fromResolution + 1} (Thread id: {Task.CurrentId})");

            IInstrumentBarDataService instrumentBarDataService = (IInstrumentBarDataService)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataService))!;
            instrumentBarDataService.DataProvider = m_context.Settings.DataProvider;
            instrumentBarDataService.MassOperation = true;

            while (list.Count > 0 && !progressDialog.CancellationTokenSource.IsCancellationRequested)
            {
              //pop an instrument/resolution pair off the stack to porcess
              CopyInstrument? copyInstrument = null;
              lock (list)
                if (list.Count > 0) copyInstrument = list.Pop();
              if (copyInstrument == null) continue; //failed to find a copy/resolution entry, all instruments processed

              progressDialog.LogInformation($"Copying instrument data for \"{copyInstrument!.Instrument.Ticker}\" to resolution \"{copyInstrument!.Resolution}\"");

              instrumentBarDataService.Resolution = copyInstrument!.Resolution;
              instrumentBarDataService.Instrument = copyInstrument!.Instrument;

              try
              {
                lock (m_attemptedInstrumentCountLock) m_attemptedInstrumentCount++;
                //monthly resolution needs to be copied from daily resolution
                if (fromResolution == Resolution.Weeks)
                  instrumentBarDataService.Copy(Resolution.Days, Resolution.Months, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime);
                else
                  instrumentBarDataService.Copy(copyInstrument!.Resolution, copyInstrument!.Resolution + 1, m_context.Settings.FromDateTime, m_context.Settings.ToDateTime);
                lock (m_successCountLock) m_successCount++;
              }
              catch (Exception e)
              {
                lock (m_failureCountLock) m_failureCount++;
                progressDialog.LogError($"EXCEPTION: Failed to copy instrument data for \"{copyInstrument.Instrument.Ticker}\" at resolution \"{copyInstrument.Resolution}\" (Exception: \"{e.Message}\")");
              }

              lock (m_attemptedInstrumentCountLock) m_attemptedInstrumentCount++;

              //update progress after copying instrument data
              progressDialog.Progress = progressDialog.Progress + 1;
            }

            progressDialog.LogInformation($"Ending worker thread for copy of instrument data for data provider \"{m_context.Settings.DataProvider}\" from resolution {fromResolution} to resolution {fromResolution + 1} (Thread id: {Task.CurrentId})");
          }, progressDialog.CancellationTokenSource.Token));

      //wait for tasks to finish copying data for this resolution
      Task.WaitAll(taskPool.ToArray());
    }
  }
}
