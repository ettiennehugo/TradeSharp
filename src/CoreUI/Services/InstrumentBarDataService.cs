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
    private IBarData? m_selectedItem;
    private ILogger<InstrumentBarDataService> m_logger;
    private IDatabase m_database;

    //constructors
    public InstrumentBarDataService(IInstrumentBarDataRepository repository, ILogger<InstrumentBarDataService> logger, IDatabase database, IDialogService dialogService) : base(dialogService)
    {
      m_logger = logger;
      m_database = database;
      m_repository = repository;
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      m_selectedItem = null;
      Items = new ObservableCollection<IBarData>();
    }

    //finalizers


    //interface implementations
    public bool Add(IBarData item)
    {
      var result = m_repository.Add(item);
      Utilities.SortedInsert(item, Items);
      SelectedItem = item;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Copy(IBarData item) => throw new NotImplementedException();  //TODO: Need to figure out how this would occur, maybe override method to support copy to different resolutions and PriceTypes.

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

    public void Export(string filename)
    {
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        exportCSV(filename);
      else if (extension == extensionJSON)
        exportJSON(filename);
    }

    public void Import(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        importCSV(importSettings);
      else if (extension == extensionJSON)
        importJSON(importSettings);
    }

    public void Refresh()
    {
      var result = m_repository.GetItems();
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
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
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
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          break;
        case Resolution.Day:
          toRepository.Resolution = Resolution.Week;

          if (Debugging.Copy) m_logger.LogInformation($"Copying {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toRepository.Resolution} resolution.");
          foreach (IBarData bar in fromBarData)
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

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
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
              toBar.High = Math.Max(toBar.High, bar.High);
              toBar.Low = Math.Min(toBar.Low, bar.Low);
              toBar.Close = bar.Close;
              toBar.Volume += bar.Volume;
            }

          if (toBarData.Count > 0) toRepository.Delete(internalFromDateTime, internalToDateTime); //we replace the bars of data
          toRepository.Update(toBarData);

          if (Debugging.Copy) m_logger.LogInformation($"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Copied {fromBarData.Count} bars defined in {fromRepository.Resolution} resolution to {toBarData.Count} bars in resolution {toRepository.Resolution}.");
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
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      long barsUpdated = 0;
      bool noErrors = true;

      //make sure we can retrieve the primary exhange for the instrument if we are importing date/time in the Exchange timezone
      Exchange? exchange = null;
      if (importSettings.DateTimeTimeZone == ImportDataDateTimeTimeZone.Exchange)
      {
        exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);
        if (exchange == null)
        {
          statusMessage = $"Failed to find exchange with id \"{Instrument!.PrimaryExchangeId.ToString()}\" can not import data based on Exchange.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
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
            m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          }

          if (!openFound || !highFound || !lowFound || !closeFound || !volumeFound)
          {
            statusMessage = "Open, high, low, close and volume fields are required to import bar data.";
            if (Debugging.ImportExport) m_logger.LogError(statusMessage);
            m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
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
            long volume = 0;

            lineNo++;
            try
            {
              for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
              {
                string? columnValue = null;
                if (csv.TryGetField(columnIndex, out columnValue))
                {
                  if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDateTime)
                  {
                    switch (importSettings.DateTimeTimeZone)
                    {
                      case ImportDataDateTimeTimeZone.UTC:
                        dateTime = DateTime.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal);
                        break;
                      case ImportDataDateTimeTimeZone.Exchange:
                        //parse date as local time, convert it so exhange timezone and then convert that to UTC timezone
                        dateTime = DateTime.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal);
                        DateTimeOffset dateTimeOffset = new DateTimeOffset((DateTime)dateTime!, exchange!.TimeZone.BaseUtcOffset);
                        dateTime = dateTimeOffset.DateTime.ToUniversalTime();
                        break;
                      case ImportDataDateTimeTimeZone.Local:
                        //adjust local time to UTC time use by database layer
                        dateTime = DateTime.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal);
                        break;
                    }
                  }
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDate)
                    date = DateOnly.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTime)
                    time = TimeOnly.Parse(columnValue!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvOpen)
                    open = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvHigh)
                    high = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvLow)
                    low = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvClose)
                    close = double.Parse(columnValue!);
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvVolume)
                    volume = long.Parse(columnValue!);
                }
              }

              //adjust date/time when separate date and time fields are used and convert it to UTC for database use
              if (date != null && time != null)
                switch (importSettings.DateTimeTimeZone)
                {
                  case ImportDataDateTimeTimeZone.UTC:
                    dateTime = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, time.Value.Hour, time.Value.Minute, time.Value.Second, DateTimeKind.Utc);
                    break;
                  case ImportDataDateTimeTimeZone.Local:
                    dateTime = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, time.Value.Hour, time.Value.Minute, time.Value.Second, DateTimeKind.Local);
                    dateTime = dateTime.Value.ToUniversalTime();
                    break;
                  case ImportDataDateTimeTimeZone.Exchange:
                    //create date/time without timezone and then set it on a date/time offset to the exchange timezone before convert to UTC
                    dateTime = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, time.Value.Hour, time.Value.Minute, time.Value.Second, DateTimeKind.Unspecified);
                    DateTimeOffset dateTimeOffset = new DateTimeOffset((DateTime)dateTime!, exchange!.TimeZone.BaseUtcOffset);
                    dateTime = dateTimeOffset.DateTime.ToUniversalTime();
                    break;
                }

              if (dateTime >= importSettings.FromDateTime && dateTime <= importSettings.ToDateTime)
              {
                bars.Add(new BarData(Resolution, (DateTime)dateTime!, open, high, low, close, volume));
                barsUpdated++; //we do not check for create since it would mean we need to search through all the data constantly
              }
            }
            catch (Exception e)
            {
              statusMessage = $"Failed to parse bar on line {lineNo} with exception \"{e.Message}\".";
              if (Debugging.ImportExport) m_logger.LogError(statusMessage);
              m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
              bars.Clear();
              noErrors = false;
            }
          }

          if (bars.Count != 0) m_repository.Update(bars);
        }
        else
        {
          statusMessage = $"Unable to parse header.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          noErrors = false;
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Imported {barsUpdated} bars from \"{importSettings.Filename}\".");

      if (noErrors)
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");
      else
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");

      RaiseRefreshEvent();  //notify view model of changes
    }

    private void importJSON(ImportSettings importSettings)
    {
      long barIndex = 0;
      long barsUpdated = 0;
      string statusMessage = $"Importing bar data from \"{importSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      bool noErrors = true;

      //make sure we can retrieve the primary exhange for the instrument if we are importing date/time in the Exchange timezone
      Exchange? exchange = null;
      if (importSettings.DateTimeTimeZone == ImportDataDateTimeTimeZone.Exchange)
      {
        exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);
        if (exchange == null)
        {
          statusMessage = $"Failed to find exchange with id \"{Instrument!.PrimaryExchangeId.ToString()}\" can not import data based on Exchange.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
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
            barIndex++;
            DateTime dateTime = DateTime.Now;

            if (barDataJson!.ContainsKey(tokenJsonDateTime))
            {
              switch (importSettings.DateTimeTimeZone)
              {
                case ImportDataDateTimeTimeZone.UTC:
                  dateTime = DateTime.Parse(barDataJson![tokenJsonDateTime]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal);
                  break;
                case ImportDataDateTimeTimeZone.Exchange:
                  //parse date as local time, convert it so exhange timezone and then convert that to UTC timezone
                  dateTime = DateTime.Parse(barDataJson![tokenJsonDateTime]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal);
                  DateTimeOffset dateTimeOffset = new DateTimeOffset((DateTime)dateTime!, exchange!.TimeZone.BaseUtcOffset);
                  dateTime = dateTimeOffset.DateTime.ToUniversalTime();
                  break;
                case ImportDataDateTimeTimeZone.Local:
                  //adjust local time to UTC time use by database layer
                  dateTime = DateTime.Parse(barDataJson![tokenJsonDateTime]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal);
                  break;
              }
            }
            else if (barDataJson!.ContainsKey(tokenJsonDate) && barDataJson!.ContainsKey(tokenJsonTime))
            {
              DateOnly date = DateOnly.Parse(barDataJson![tokenJsonDate]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
              TimeOnly time = TimeOnly.Parse(barDataJson![tokenJsonTime]!.AsValue().Deserialize<string>()!, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces);
              switch (importSettings.DateTimeTimeZone)
              {
                case ImportDataDateTimeTimeZone.UTC:
                  dateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Utc);
                  break;
                case ImportDataDateTimeTimeZone.Local:
                  dateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Local);
                  dateTime = dateTime.ToUniversalTime();
                  break;
                case ImportDataDateTimeTimeZone.Exchange:
                  //create date/time without timezone and then set it on a date/time offset to the exchange timezone before convert to UTC
                  dateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Unspecified);
                  DateTimeOffset dateTimeOffset = new DateTimeOffset((DateTime)dateTime!, exchange!.TimeZone.BaseUtcOffset);
                  dateTime = dateTimeOffset.DateTime.ToUniversalTime();
                  break;
              }
            }
            else
            {
              statusMessage = $"Bar at index {barIndex} does not contain a valid date/time specification.";  //JSON library contains zero information on where it found this data so the best we can do is use an index.
              if (Debugging.ImportExport) m_logger.LogError(statusMessage);
              m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "", statusMessage);
              continue; //skip to next bar definition
            }

            //skip bars that are not within the import date/time range
            if (dateTime >= importSettings.FromDateTime && dateTime <= importSettings.ToDateTime) continue;

            double open = barDataJson![tokenJsonOpen]!.AsValue().Deserialize<double>();
            double high = barDataJson![tokenJsonHigh]!.AsValue().Deserialize<double>();
            double low = barDataJson![tokenJsonLow]!.AsValue().Deserialize<double>();
            double close = barDataJson![tokenJsonClose]!.AsValue().Deserialize<double>();
            long volume = barDataJson![tokenJsonVolume]!.AsValue().Deserialize<long>();
            bars.Add(new BarData(Resolution, dateTime, open, high, low, close, volume));
            barsUpdated++; //we do not check for create since it would mean we need to search through all the data constantly
          }

          if (bars.Count > 0) m_repository.Update(bars);
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Imported {barsUpdated} bars from \"{importSettings.Filename}\".");

      if (noErrors)
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");
      else
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Completed import of {barsUpdated} from \"{importSettings.Filename}\".");

      RaiseRefreshEvent();  //notify view model of changes
    }

    //NOTE: Export always writes out the data in the Exchange time-zone, so the import settings structure defaults to Exchange time-zone.
    private void exportCSV(string filename)
    {
      long exportCount = 0;
      string statusMessage = $"Exporting bar data to \"{filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      Exchange? exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);

      if (exchange == null)
      {
        statusMessage = $"Date/time exported without UTC conversion, failed to find primary exchange \"{Instrument!.PrimaryExchangeId.ToString()}\" associated with instrument \"{Instrument!.Ticker}\".";
        if (Debugging.ImportExport) m_logger.LogWarning(statusMessage);
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
        return;
      }

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        file.WriteLine($"{tokenCsvDateTime},{tokenCsvOpen},{tokenCsvHigh},{tokenCsvLow},{tokenCsvClose},{tokenCsvVolume}");

        foreach (IBarData barData in Items)
        {
          string barDataStr = "";

          if (exchange != null)
          {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(barData.DateTime.Ticks, exchange.TimeZone.BaseUtcOffset);
            barDataStr = dateTimeOffset.UtcDateTime.ToString();
          }
          else
            barDataStr = barData.DateTime.ToString();

          barDataStr += ", ";
          barDataStr += barData.Open.ToString();
          barDataStr += ", ";
          barDataStr += barData.High.ToString();
          barDataStr += ", ";
          barDataStr += barData.Low.ToString();
          barDataStr += ", ";
          barDataStr += barData.Close.ToString();
          barDataStr += ", ";
          barDataStr += barData.Volume.ToString();
          file.WriteLine(barDataStr);
          exportCount++;
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Exported {exportCount} bars to \"{filename}\".");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} bars to \"{filename}\".");
    }

    //NOTE: Export always writes out the data in the Exchange time-zone, so the import defaults to Exchange time-zone.
    private void exportJSON(string filename)
    {
      long exportCount = 0;
      string statusMessage = $"Exporting bar data to \"{filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);
      Exchange? exchange = m_database.GetExchange(Instrument!.PrimaryExchangeId);

      if (exchange == null)
      {
        statusMessage = $"Date/time exported without UTC conversion, failed to find primary exchange \"{Instrument!.PrimaryExchangeId.ToString()}\" associated with instrument \"{Instrument!.Ticker}\".";
        if (Debugging.ImportExport) m_logger.LogWarning(statusMessage);
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
        return;
      }

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        int barDataIndex = 0;
        int barDataCount = Items.Count;
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

        file.WriteLine("[");
        foreach (IBarData barData in Items)
        {
          string dateTimeStr = string.Empty;
          if (exchange != null)
          {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(barData.DateTime.Ticks, exchange.TimeZone.BaseUtcOffset);
            dateTimeStr = dateTimeOffset.UtcDateTime.ToString();
          }
          else
            dateTimeStr = barData.DateTime.ToString();

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

      if (Debugging.ImportExport) m_logger.LogInformation($"Exported {exportCount} bars to \"{filename}\".");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} bars to \"{filename}\".");
    }
  }
}
