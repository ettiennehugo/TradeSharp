using TradeSharp.Data;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Repositories;
using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using CsvHelper;
using System.Globalization;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for instrument bar data objects.
  /// </summary>
  public partial class InstrumentBarDataService : ServiceBase, IInstrumentBarDataService
  {
    //constants
    private const string extensionCSV = ".csv";
    private const string extensionJSON = ".json";
    private const string tokenCsvDateTime = "datetime";
    private const string tokenCsvDate = "date";
    private const string tokenCsvTime = "time";
    private const string tokenCsvOpen = "open";
    private const string tokenCsvHigh = "high";
    private const string tokenCsvLow = "low";
    private const string tokenCsvClose = "close";
    private const string tokenCsvVolume = "volume";
    private const string tokenJsonDateTime = "datetime";
    private const string tokenJsonDate = "date";
    private const string tokenJsonTime = "time";
    private const string tokenJsonOpen = "open";
    private const string tokenJsonHigh = "high";
    private const string tokenJsonLow = "low";
    private const string tokenJsonClose = "close";
    private const string tokenJsonVolume = "volume";

    //enums


    //types


    //attributes
    private IInstrumentBarDataRepository m_repository;
    private IConfigurationService m_configurationService;
    private IBarData? m_selectedItem;
    private ILogger<InstrumentBarDataService> m_logger;
    private IDatabase m_database;

    //constructors
    public InstrumentBarDataService(IInstrumentBarDataRepository repository, ILogger<InstrumentBarDataService> logger, IDatabase database, IConfigurationService configurationService, IDialogService dialogService) : base(dialogService)
    {
      m_logger = logger;
      m_database = database;
      m_repository = repository;
      m_configurationService = configurationService;
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      MassOperation = false;
      m_selectedItem = null;
      Items = new ObservableCollection<IBarData>();
    }

    //finalizers


    //interface implementations
    public bool Add(IBarData item)
    {
      var result = m_repository.Add(item);
      TradeSharp.Common.Utilities.SortedInsert(item, Items);
      SelectedItem = item;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Copy(IBarData item) => throw new NotImplementedException(); //this is mainly implemented by the IMassCopyInstrumentDataService

    public bool Delete(IBarData item)
    {
      bool result = m_repository.Delete(item);

      if (item == SelectedItem)
      {
        SelectedItem = null;
        SelectedItemChanged?.Invoke(this, SelectedItem);
      }

      Items.Remove(item);
      return result;
    }

    public int Delete(IList<IBarData> items)
    {
      int result = m_repository.Delete(items);
      foreach (IBarData item in items) Items.Remove(item);
      SelectedItem = Items.FirstOrDefault();
      return result;
    }

    public override void Export(ExportSettings exportSettings)
    {
      //only output the data if the replacing the file is allowed
      if (exportSettings.ReplaceBehavior == ExportReplaceBehavior.Skip && File.Exists(exportSettings.Filename))
      {
        string statusMessage = $"File \"{exportSettings.Filename}\" already exists and the export settings are set to skip it.";
        if (Debugging.ImportExport) m_logger.LogWarning(statusMessage);
        if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "", statusMessage);
        return;
      }

      //get the instrument exchange if Exchange time-zone is used for export data
      Exchange? exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);
      if (exportSettings.DateTimeTimeZone == ImportExportDataDateTimeTimeZone.Exchange && exchange == null)
      {
        string statusMessage = $"Date/time requested time-zone for export is Exchange, failed to find primary exchange \"{Instrument!.PrimaryExchangeId.ToString()}\" associated with instrument \"{Instrument!.Ticker}\", skipping export.";
        if (Debugging.ImportExport) m_logger.LogWarning(statusMessage);
        if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
        return;
      }

      IConfigurationService.TimeZone dbTimeZoneUsed = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];

      string extension = Path.GetExtension(exportSettings.Filename).ToLower();
      long exportCount = 0;
      if (extension == extensionCSV)
        exportCount = exportCSV(exportSettings, dbTimeZoneUsed, exchange);
      else if (extension == extensionJSON)
        exportCount = exportJSON(exportSettings, dbTimeZoneUsed, exchange);

      if (Debugging.ImportExport) m_logger.LogInformation($"Exported {exportCount} bars to \"{exportSettings.Filename}\".");
      if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} bars to \"{exportSettings.Filename}\".");
    }

    public override void Import(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        importCSV(importSettings);
      else if (extension == extensionJSON)
        importJSON(importSettings);
    }

    public void Refresh()
    {
      LoadedState = LoadedState.Loading;
      var result = m_repository.GetItems();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
      LoadedState = LoadedState.Loaded;
      raiseRefreshEvent();
    }

    public void Refresh(DateTime from, DateTime to)
    {
      var result = m_repository.GetItems(from, to);
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public int GetCount()
    {
      return m_repository.GetCount();
    }

    public int GetCount(DateTime from, DateTime to)
    {
      return m_repository.GetCount(from, to);
    }

    public IList<IBarData> GetItems(DateTime from, DateTime to)
    {
      return m_repository.GetItems(from, to);
    }

    public IList<IBarData> GetItems(int index, int count)
    {
      return m_repository.GetItems(index, count);
    }

    public IList<IBarData> GetItems(DateTime from, DateTime to, int index, int count)
    {
      return m_repository.GetItems(from, to, index, count);
    }

    /// <summary>
    /// Copy the given bar resolution to the next higher resolution, e.g. minute input parameter copies the data to the
    /// hourly timeframe.
    /// NOTES:
    ///   * The start bars' date/time is used as the date/time for the bar in the higher resolution, e.g. if you start with daily bars converting to
    ///     weekly bars for stock data the resulting weekly bars will always start on a Monday date/time.
    /// </summary>
    public void Copy(Resolution from, DateTime? fromDateTime = null, DateTime? toDateTime = null)
    {
      DateTime internalFromDateTime = fromDateTime ?? Constants.DefaultMinimumDateTime;
      DateTime internalToDateTime = toDateTime ?? Constants.DefaultMaximumDateTime;

      IInstrumentBarDataRepository fromRepository = (IInstrumentBarDataRepository)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataRepository))!;
      IInstrumentBarDataRepository toRepository = (IInstrumentBarDataRepository)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataRepository))!;
      fromRepository.DataProvider = DataProvider;
      fromRepository.Instrument = Instrument;
      fromRepository.Resolution = from;
      IList<IBarData> fromBarData = fromRepository.GetItems(internalFromDateTime, internalToDateTime);

      toRepository.DataProvider = DataProvider;
      toRepository.Instrument = Instrument;
      IList<IBarData> toBarData = new List<IBarData>();

      IBarData? toBar = null;
      switch (from)
      {
        case Resolution.Minute:
          toRepository.Resolution = Resolution.Hour;

          if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
          foreach (IBarData bar in fromBarData)
            if (toBar == null || toBar.DateTime.Hour != bar.DateTime.Hour)
            {
              toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
              toBarData.Add(toBar);
            }
            else //keep constructing bar while it's in the same to-resolution
            {
              //nothing to do for the open, that is set when the new bar is created
              toBar.DateTime = bar.DateTime;    //alwys keep the date/time to the last bar
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          break;
        case Resolution.Hour:
          toRepository.Resolution = Resolution.Day;

          if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
          foreach (IBarData bar in fromBarData)
            if (toBar == null || toBar.DateTime.Day != bar.DateTime.Day)
            {
              toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
              toBarData.Add(toBar);
            }
            else //keep constructing bar while it's in the same to-resolution
            {
              //nothing to do for the open, that is set when the new bar is created
              toBar.DateTime = bar.DateTime;    //alwys keep the date/time to the last bar
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          break;
        case Resolution.Day:
          toRepository.Resolution = Resolution.Week;

          if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
          foreach (IBarData bar in fromBarData)
            //we always end the week on a Sunday and use Monday as the start of the week
            if (toBar == null || (toBar.DateTime.DayOfWeek != DayOfWeek.Monday && bar.DateTime.DayOfWeek == DayOfWeek.Monday ))
            {
              toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
              toBarData.Add(toBar);
            }
            else //keep constructing bar while it's in the same to-resolution
            {
              //nothing to do for the open, that is set when the new bar is created
              toBar.DateTime = bar.DateTime;    //alwys keep the date/time to the last bar
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          break;
        case Resolution.Week:
          toRepository.Resolution = Resolution.Month;

          if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
          foreach (IBarData bar in fromBarData)
            if (toBar == null || toBar.DateTime.Month != bar.DateTime.Month)
            {
              toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
              toBarData.Add(toBar);
            }
            else //keep constructing bar while it's in the same to-resolution
            {
              //nothing to do for the open, that is set when the new bar is created
              toBar.DateTime = bar.DateTime;    //alwys keep the date/time to the last bar
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          break;
        case Resolution.Month:
          //nothing to do
          break;
      }
    }

    public void Copy(Resolution from, Resolution to, DateTime? fromDateTime = null, DateTime? toDateTime = null)
    {
      //make sure the from resolution is lower than the to resolution
      if (Debugging.Copy && from >= to)
      {
        string statusMessage = $"The from resolution {from} must be lower than the to resolution {to}.";
        m_logger.LogError(statusMessage);
        return;
      }

      //handle trivial cases already covered by the Copy(Resolution from) method
      if (from == Resolution.Minute && to == Resolution.Hour)
      {
        Copy(from);
        return;
      }

      if (from == Resolution.Hour && to == Resolution.Day)
      {
        Copy(from);
        return;
      }

      if (from == Resolution.Day && to == Resolution.Week)
      {
        Copy(from);
        return;
      }

      if (from == Resolution.Week && to == Resolution.Month)
      {
        Copy(from);
        return;
      }

      //handle rest of the more complex cases
      DateTime internalFromDateTime = fromDateTime ?? Constants.DefaultMinimumDateTime;
      DateTime internalToDateTime = toDateTime ?? Constants.DefaultMaximumDateTime;

      IInstrumentBarDataRepository fromRepository = (IInstrumentBarDataRepository)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataRepository))!;
      IInstrumentBarDataRepository toRepository = (IInstrumentBarDataRepository)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataRepository))!;
      fromRepository.DataProvider = DataProvider;
      fromRepository.Instrument = Instrument;
      fromRepository.Resolution = from;
      IList<IBarData> fromBarData = fromRepository.GetItems(internalFromDateTime, internalToDateTime);

      toRepository.DataProvider = DataProvider;
      toRepository.Instrument = Instrument;
      toRepository.Resolution = to;
      IList<IBarData> toBarData = new List<IBarData>();

      for (Resolution resolution = from; resolution < to; resolution++)
      {
        IBarData? toBar = null;
        switch (from)
        {
          case Resolution.Minute:
            toRepository.Resolution = Resolution.Hour;

            if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
            foreach (IBarData bar in fromBarData)
              if (bar.DateTime >= internalFromDateTime && bar.DateTime <= internalToDateTime)
              {
                if (toBar == null || toBar.DateTime.Hour != bar.DateTime.Hour)
                {
                  toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                  toBarData.Add(toBar);
                }
                else //keep constructing bar while it's in the same to-resolution
                {
                  //nothing to do for the open, that is set when the new bar is created
                  toBar.High = Math.Max(toBar.High, bar.High);
                  toBar.Low = Math.Min(toBar.Low, bar.Low);
                  toBar.Close = bar.Close;
                  toBar.Volume += bar.Volume;
                }
              }

            if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
            fromBarData = new List<IBarData>(toBarData); //copy the data to the next resolution
            break;
          case Resolution.Hour:
            toRepository.Resolution = Resolution.Day;

            if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
            foreach (IBarData bar in fromBarData)
              if (bar.DateTime >= internalFromDateTime && bar.DateTime <= internalToDateTime)
              {
                if (toBar == null || toBar.DateTime.Day != bar.DateTime.Day)
                {
                  toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                  toBarData.Add(toBar);
                }
                else //keep constructing bar while it's in the same to-resolution
                {
                  //nothing to do for the open, that is set when the new bar is created
                  toBar.High = Math.Max(toBar.High, bar.High);
                  toBar.Low = Math.Min(toBar.Low, bar.Low);
                  toBar.Close = bar.Close;
                  toBar.Volume += bar.Volume;
                }
              }

            if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
            fromBarData = new List<IBarData>(toBarData); //copy the data to the next resolution
            break;
          case Resolution.Day:
            toRepository.Resolution = Resolution.Week;

            if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
            foreach (IBarData bar in fromBarData)
              if (bar.DateTime >= internalFromDateTime && bar.DateTime <= internalToDateTime)
              {
                //ISO8601 considers Monday the first day of the week, so we use that as the first day of the week for week resolution bars.
                //NOTE: * This function only works for Gregorian dates which is fine, CultureInfo and ISOWeek returns anything and everything
                //        except just a sane interpretation of the week number based on "which week of the year is this given that 1 January
                //        is the start of the first week?".
                //      * We are good with considering the first and last weeks of the year to be "partial" weeks.
                if (toBar == null || ISOWeek.GetWeekOfYear(toBar.DateTime) != ISOWeek.GetWeekOfYear(bar.DateTime))
                {
                  toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                  toBarData.Add(toBar);
                }
                else //keep constructing bar while it's in the same to-resolution
                {
                  //nothing to do for the open, that is set when the new bar is created
                  toBar.High = Math.Max(toBar.High, bar.High);
                  toBar.Low = Math.Min(toBar.Low, bar.Low);
                  toBar.Close = bar.Close;
                  toBar.Volume += bar.Volume;
                }
              }

            if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
            fromBarData = new List<IBarData>(toBarData); //copy the data to the next resolution
            break;
          case Resolution.Week:
            toRepository.Resolution = Resolution.Month;

            if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
            foreach (IBarData bar in fromBarData)
              if (bar.DateTime >= internalFromDateTime && bar.DateTime <= internalToDateTime)
              {
                if (toBar == null || toBar.DateTime.Month != bar.DateTime.Month)
                {
                  toBar = new BarData(toRepository.Resolution, bar.DateTime, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                  toBarData.Add(toBar);
                }
                else //keep constructing bar while it's in the same to-resolution
                {
                  //nothing to do for the open, that is set when the new bar is created
                  toBar.High = Math.Max(toBar.High, bar.High);
                  toBar.Low = Math.Min(toBar.Low, bar.Low);
                  toBar.Close = bar.Close;
                  toBar.Volume += bar.Volume;
                }
              }

            if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
            fromBarData = new List<IBarData>(toBarData); //copy the data to the next resolution
            break;
          case Resolution.Month:
            //nothing to do
            break;
        }
      }

      if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
      toRepository.Update(toBarData);
    }

    public bool Update(IBarData item)
    {
      var result = m_repository.Update(item);

      //the bar editor does not allow modification of the DateTime
      for (int i = 0; i < Items.Count(); i++)
        if (item.Equals(Items[i]))
        {
          Items.RemoveAt(i);
          Items.Insert(i, item);
          return result;
        }

      return result;
    }

    //properties
    public Guid ParentId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } //not supported, instrument bar data requires a complex key

    public string DataProvider { get => m_repository.DataProvider; set => m_repository.DataProvider = value; }
    public Instrument? Instrument { get => m_repository.Instrument; set => m_repository.Instrument = value; }
    public Resolution Resolution { get => m_repository.Resolution; set => m_repository.Resolution = value; }
    public bool MassOperation { get; set; }
    public event EventHandler<IBarData?>? SelectedItemChanged;
    public IBarData? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public IList<IBarData> Items { get; set; }

    //events


    //methods

    /// <summary>
    /// Convert the input column date/time value to the output value based on the import settings.
    /// </summary>
    private DateTime convertImportDateTime(string columnValue, ImportExportDataDateTimeTimeZone importTimeZone, Exchange? exchange, out DateTime unconvertedDateTime)
    {
      DateTime result = DateTime.Now;
      unconvertedDateTime = DateTime.Now;
      switch (importTimeZone)
      {
        case ImportExportDataDateTimeTimeZone.UTC:
          result = DateTime.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal);
          unconvertedDateTime = result;
          break;
        case ImportExportDataDateTimeTimeZone.Exchange:
          //parse date as-is and convert it from the exchange timezone to the UTC
          result = DateTime.Parse(columnValue!);
          unconvertedDateTime = result;
          result = TimeZoneInfo.ConvertTimeToUtc(result, exchange!.TimeZone);
          break;
        case ImportExportDataDateTimeTimeZone.Local:
          //adjust local time to UTC time used by database layer
          unconvertedDateTime = DateTime.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal);
          result = unconvertedDateTime.ToUniversalTime();
          break;
      }

      return result;
    }

    /// <summary>
    /// Convert the input column date/time value to UTC used by the database from the time-zone in the import settings, returns the unconverted date/time to be
    /// used to filter the data.
    /// </summary>
    private DateTime convertImportDateTime(DateOnly date, TimeOnly time, ImportExportDataDateTimeTimeZone importTimeZone, Exchange? exchange, out DateTime unconvertedDateTime)
    {
      DateTime result = DateTime.Now;
      unconvertedDateTime = DateTime.Now;
      switch (importTimeZone)
      {
        case ImportExportDataDateTimeTimeZone.UTC:
          result = new DateTime(date, time);
          unconvertedDateTime = result;
          break;
        case ImportExportDataDateTimeTimeZone.Exchange:
          //parse date as local time, convert it so exhange timezone and then convert that to UTC timezone
          result = new DateTime(date, time);
          unconvertedDateTime = result;
          result = TimeZoneInfo.ConvertTimeToUtc(result, exchange!.TimeZone);
          break;
        case ImportExportDataDateTimeTimeZone.Local:
          //adjust local time to UTC time used by database layer
          unconvertedDateTime = new DateTime(date, time);
          result = unconvertedDateTime.ToUniversalTime();
          break;
      }

      return result;
    }

    /// <summary>
    /// Import the bar data from a csv file that has the following formats:
    ///   datetime, open, high, low, close, volume[, id]
    /// OR
    ///   date, time, open, high, low, close, volume[, id]
    /// Fields can be in any order but the above values are required.
    /// </summary>
    private void importCSV(ImportSettings importSettings)
    {
      string statusMessage = $"Importing bar data from \"{importSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      long barsUpdated = 0;
      bool noErrors = true;

      //make sure we can retrieve the primary exhange for the instrument if we are importing date/time in the Exchange timezone
      Exchange? exchange = null;
      if (importSettings.DateTimeTimeZone == ImportExportDataDateTimeTimeZone.Exchange)
      {
        exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);
        if (exchange == null)
        {
          statusMessage = $"Failed to find exchange with id \"{Instrument!.PrimaryExchangeId.ToString()}\" can not import data based on Exchange.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          return;
        }
      }

      //try to import the data from the file
      using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        //read the header record
        if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
        {
          int lineNo = 1; //header row is on line 0

          //validate that the header contains valid date/time specification and required fields before trying to parse the data
          bool dateTimeFound = false;
          bool dateFound = false;
          bool timeFound = false;
          bool openFound = false;
          bool highFound = false;
          bool lowFound = false;
          bool closeFound = false;
          bool volumeFound = false;

          for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
          {
            string columnName = csv.HeaderRecord[columnIndex].ToLower();
            if (columnName == tokenCsvDateTime)
              dateTimeFound = true;
            else if (columnName == tokenCsvDate)
              dateFound = true;
            else if (columnName == tokenCsvTime)
              timeFound = true;
            else if (columnName == tokenCsvOpen)
              openFound = true;
            else if (columnName == tokenCsvHigh)
              highFound = true;
            else if (columnName == tokenCsvLow)
              lowFound = true;
            else if (columnName == tokenCsvClose)
              closeFound = true;
            else if (columnName == tokenCsvVolume)
              volumeFound = true;
          }

          if (!dateTimeFound && !(dateFound && timeFound))
          {
            statusMessage = "DateTime or (Date and Time) fields are required to import bar data.";
            if (Debugging.ImportExport) m_logger.LogError(statusMessage);
            if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          }

          if (!openFound || !highFound || !lowFound || !closeFound || !volumeFound)
          {
            statusMessage = "Open, high, low, close and volume fields are required to import bar data.";
            if (Debugging.ImportExport) m_logger.LogError(statusMessage);
            if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          }

          //parse the file data, first load the bars into a cache and then mass update the database (mass update is faster)
          List<IBarData> bars = new List<IBarData>();
          while (csv.Read() && noErrors)
          {
            DateTime? dateTime = null;
            DateOnly? date = null;
            TimeOnly? time = null;
            double open = 0.0;
            double high = 0.0;
            double low = 0.0;
            double close = 0.0;
            double volume = 0.0;

            lineNo++;
            bool filterBar = false;

            try
            {
              for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
              {
                if (filterBar) continue;    //skip rest of fields if bar will be filtered anyway

                string? columnValue = null;
                if (csv.TryGetField(columnIndex, out columnValue))
                {
                  if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDateTime)
                  {
                    //convert input date/time value and filter the bar data if needed
                    dateTime = convertImportDateTime(columnValue!, importSettings.DateTimeTimeZone, exchange, out DateTime unconvertedDateTime);
                    filterBar = unconvertedDateTime < importSettings.FromDateTime && unconvertedDateTime > importSettings.ToDateTime;
                  }
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDate)
                  {
                    date = DateOnly.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
                    if (date != null && time != null)
                    {
                      //convert input date/time value and filter the bar data if needed
                      dateTime = convertImportDateTime((DateOnly)date, (TimeOnly)time, importSettings.DateTimeTimeZone, exchange, out DateTime unconvertedDateTime);
                      filterBar = unconvertedDateTime < importSettings.FromDateTime || unconvertedDateTime > importSettings.ToDateTime;
                    }
                  }
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTime)
                  {
                    time = TimeOnly.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
                    if (date != null && time != null)
                    {
                      //convert input date/time value and filter the bar data if needed
                      dateTime = convertImportDateTime((DateOnly)date, (TimeOnly)time, importSettings.DateTimeTimeZone, exchange, out DateTime unconvertedDateTime);
                      filterBar = unconvertedDateTime < importSettings.FromDateTime || unconvertedDateTime > importSettings.ToDateTime;
                    }
                  }
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvOpen)
                    open = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvHigh)
                    high = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvLow)
                    low = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvClose)
                    close = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvVolume)
                    volume = double.Parse(columnValue!);
                }
              }

              //filter the imported date/time according to 
              if (!filterBar)
              {
                bars.Add(new BarData(Resolution, (DateTime)dateTime!, open, high, low, close, volume));
                barsUpdated++; //we do not check for create since it would mean we need to search through all the data constantly
              }
            }
            catch (Exception e)
            {
              statusMessage = $"Failed to parse bar on line {lineNo} with exception \"{e.Message}\".";
              if (Debugging.ImportExport) m_logger.LogError(statusMessage);
              if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
              bars.Clear();
              noErrors = false;
            }
          }

          if (bars.Count > 0) m_repository.Update(bars);
        }
        else
        {
          statusMessage = $"Unable to parse header.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          noErrors = false;
        }
      }
      if (Debugging.ImportExport) m_logger.LogInformation($"Imported {barsUpdated} bars from \"{importSettings.Filename}\".");

      if (noErrors)
      {
        if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");
      }
      else
        if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");

      raiseRefreshEvent();  //notify view model of changes
    }

    private void importJSON(ImportSettings importSettings)
    {
      long barIndex = 0;
      long barsUpdated = 0;
      string statusMessage = $"Importing bar data from \"{importSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      bool noErrors = true;

      //make sure we can retrieve the primary exhange for the instrument if we are importing date/time in the Exchange timezone
      Exchange? exchange = null;
      if (importSettings.DateTimeTimeZone == ImportExportDataDateTimeTimeZone.Exchange)
      {
        exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);
        if (exchange == null)
        {
          statusMessage = $"Failed to find exchange with id \"{Instrument!.PrimaryExchangeId.ToString()}\" can not import data based on Exchange.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          return;
        }
      }

      //try to import the data from the file
      using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible

        if (documentNode != null)
        {
          JsonArray barDataArray = documentNode.AsArray();
          List<IBarData> bars = new List<IBarData>();
          foreach (JsonObject? barDataJson in barDataArray)
          {
            bool filterBar = false;
            barIndex++;
            DateTime dateTime = DateTime.Now;

            if (barDataJson!.ContainsKey(tokenJsonDateTime))
            {
              dateTime = convertImportDateTime(barDataJson![tokenJsonDateTime]!.AsValue().Deserialize<string>()!, importSettings.DateTimeTimeZone, exchange, out DateTime unconvertedDateTime);
              filterBar = unconvertedDateTime < importSettings.FromDateTime || unconvertedDateTime > importSettings.ToDateTime;
            }
            else if (barDataJson!.ContainsKey(tokenJsonDate) && barDataJson!.ContainsKey(tokenJsonTime))
            {
              DateOnly date = DateOnly.Parse(barDataJson![tokenJsonDate]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
              TimeOnly time = TimeOnly.Parse(barDataJson![tokenJsonTime]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
              dateTime = convertImportDateTime((DateOnly)date, (TimeOnly)time, importSettings.DateTimeTimeZone, exchange, out DateTime unconvertedDateTime);
              filterBar = unconvertedDateTime < importSettings.FromDateTime || unconvertedDateTime > importSettings.ToDateTime;
            }
            else
            {
              statusMessage = $"Bar at index {barIndex} does not contain a valid date/time specification.";  //JSON library contains zero information on where it found this data so the best we can do is use an index.
              if (Debugging.ImportExport) m_logger.LogError(statusMessage);
              if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "", statusMessage);
              continue; //skip to next bar definition
            }

            //skip bars that are not within the import date/time range
            if (filterBar) continue;

            double open = barDataJson![tokenJsonOpen]!.AsValue().Deserialize<double>();
            double high = barDataJson![tokenJsonHigh]!.AsValue().Deserialize<double>();
            double low = barDataJson![tokenJsonLow]!.AsValue().Deserialize<double>();
            double close = barDataJson![tokenJsonClose]!.AsValue().Deserialize<double>();
            double volume = barDataJson![tokenJsonVolume]!.AsValue().Deserialize<double>();

            bars.Add(new BarData(Resolution, dateTime, open, high, low, close, volume));
            barsUpdated++; //we do not check for create since it would mean we need to search through all the data constantly
          }

          if (bars.Count > 0) m_repository.Update(bars);
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Imported {barsUpdated} bars from \"{importSettings.Filename}\".");

      if (noErrors)
      {
        if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");
      }
      else
        if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");

      raiseRefreshEvent();  //notify view model of changes
    }


    /// <summary>
    /// The database can be configured to output the date/time data in different time-zones, this function converts the database timezone to the requested exchange time-zone.
    /// </summary>
    private DateTime convertExportDateTime(DateTime dateTime, IConfigurationService.TimeZone dbOutputTimeZone, ImportExportDataDateTimeTimeZone exportTimeZone, Exchange? exchange)
    {
      switch (exportTimeZone)
      {
        case ImportExportDataDateTimeTimeZone.UTC:
          return dateTime.ToUniversalTime();
        case ImportExportDataDateTimeTimeZone.Exchange:
          //convert date/time to the requested Exchange export time
          switch (dbOutputTimeZone)
          {
            case IConfigurationService.TimeZone.UTC:
              return TimeZoneInfo.ConvertTimeFromUtc(dateTime, exchange!.TimeZone);
            case IConfigurationService.TimeZone.Local:
              return TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), exchange!.TimeZone);
            case IConfigurationService.TimeZone.Exchange:
              return dateTime;
          }
          break;
        case ImportExportDataDateTimeTimeZone.Local:
          //convert date/time to the requested Local export time
          switch (dbOutputTimeZone)
          {
            case IConfigurationService.TimeZone.UTC:
              return dateTime.ToLocalTime();
            case IConfigurationService.TimeZone.Local:
              return dateTime;
            case IConfigurationService.TimeZone.Exchange:
              return dateTime.ToUniversalTime().ToLocalTime();
          }
          break;
      }

      return dateTime;  //should never reach this point, just keep the compiler happy
    }


    //NOTE: Export always writes out the data in the Exchange time-zone, so the import settings structure defaults to Exchange time-zone.
    private long exportCSV(ExportSettings exportSettings, IConfigurationService.TimeZone dbTimeZoneUsed, Exchange? exchange)
    {
      long exportCount = 0;
      string statusMessage = $"Exporting bar data to \"{exportSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      using (StreamWriter file = File.CreateText(exportSettings.Filename))
      {
        file.WriteLine($"{tokenCsvDateTime},{tokenCsvOpen},{tokenCsvHigh},{tokenCsvLow},{tokenCsvClose},{tokenCsvVolume}");

        foreach (IBarData barData in Items)
        {
          DateTime exportDateTime = convertExportDateTime(barData.DateTime, dbTimeZoneUsed, exportSettings.DateTimeTimeZone, exchange);
          if (exportDateTime < exportSettings.FromDateTime || exportDateTime > exportSettings.ToDateTime) continue; //skip bars that are not within the export date/time range
          //NOTES:
          // * We need to output the data in a very specific format to ensure that date/time parsing will work when importing the data (DateTime.Parse will fail if the format is not correct).
          //     o format = s: 2008-06-15T21:15:07.0000000 (not currently used since it's not needed).
          //     s format = s: 2008-06-15T21:15:07
          // * We do not include spaces between comma's and data since it adds additional bytes to the file.
          string barDataStr = exportDateTime.ToString("s");
          barDataStr += ",";
          barDataStr += barData.Open.ToString();
          barDataStr += ",";
          barDataStr += barData.High.ToString();
          barDataStr += ",";
          barDataStr += barData.Low.ToString();
          barDataStr += ",";
          barDataStr += barData.Close.ToString();
          barDataStr += ",";
          barDataStr += barData.Volume.ToString();
          file.WriteLine(barDataStr);
          exportCount++;
        }
      }

      return exportCount;
    }

    private long exportJSON(ExportSettings exportSettings, IConfigurationService.TimeZone dbTimeZoneUsed, Exchange? exchange)
    {
      long exportCount = 0;
      string statusMessage = $"Exporting bar data to \"{exportSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      if (!MassOperation) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      using (StreamWriter file = File.CreateText(exportSettings.Filename))
      {
        int barDataIndex = 0;
        int barDataCount = Items.Count;
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

        file.WriteLine("[");
        foreach (IBarData barData in Items)
        {
          DateTime exportDateTime = convertExportDateTime(barData.DateTime, dbTimeZoneUsed, exportSettings.DateTimeTimeZone, exchange);
          if (exportDateTime < exportSettings.FromDateTime || exportDateTime > exportSettings.ToDateTime) continue; //skip bars that are not within the export date/time range

          //NOTE: We need to output the data in a very specific format to ensure that date/time parsing will work when importing the data (DateTime.Parse will fail if the format is not correct).
          // o format = s: 2008-06-15T21:15:07.0000000 (not currently used since it's not needed).
          // s format = s: 2008-06-15T21:15:07
          string dateTimeStr = exportDateTime.ToString("s");

          JsonObject barDataJson = new JsonObject
          {
            [tokenJsonDateTime] = dateTimeStr,
            [tokenJsonOpen] = barData.Open,
            [tokenJsonHigh] = barData.High,
            [tokenJsonLow] = barData.Low,
            [tokenJsonClose] = barData.Close,
            [tokenJsonVolume] = barData.Volume,
          };

          file.Write(barDataJson.ToJsonString(options));
          exportCount++;
          if (barDataIndex < barDataCount - 1) file.WriteLine(",");
        }
        file.WriteLine("");
        file.WriteLine("]");
      }

      return exportCount;
    }
  }
}
