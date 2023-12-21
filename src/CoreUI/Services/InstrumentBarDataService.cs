using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Repositories;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using CsvHelper;
using System.Globalization;
using System.Xml.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for instrument bar data objects.
  /// </summary>
  public partial class InstrumentBarDataService : ObservableObject, IInstrumentBarDataService
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
    private const string tokenCsvSynthetic = "synthetic";
    private const string tokenJsonDateTime = "datetime";
    private const string tokenJsonDate = "date";
    private const string tokenJsonTime = "time";
    private const string tokenJsonOpen = "open";
    private const string tokenJsonHigh = "high";
    private const string tokenJsonLow = "low";
    private const string tokenJsonClose = "close";
    private const string tokenJsonVolume = "volume";
    private const string tokenJsonSynthetic = "synthetic";

    //enums


    //types


    //attributes
    private IInstrumentBarDataRepository m_repository;
    private IBarData? m_selectedItem;
    private ILoggerFactory m_loggerFactory;
    private IDataStoreService m_dataStoreService;

    //constructors
    public InstrumentBarDataService(ILoggerFactory loggerFactory, IDataStoreService dataStoreService)
    {
      m_loggerFactory = loggerFactory;
      m_dataStoreService = dataStoreService;
      m_repository = Ioc.Default.GetRequiredService<IInstrumentBarDataRepository>();  //need to do this to get a unique transient repository instance associated with this specific service
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      m_selectedItem = null;
      Items = new ObservableCollection<IBarData>();
    }

    //finalizers


    //interface implementations
    public Task<IBarData> AddAsync(IBarData item)
    {
      return Task.Run(async () =>
      {
        var result = await m_repository.AddAsync(item);
        Utilities.SortedInsert(item, Items);
        SelectedItem = result;
        SelectedItemChanged?.Invoke(this, SelectedItem);
        return result;
      });
    }

    public Task<IBarData> CopyAsync(IBarData item) => throw new NotImplementedException();  //TODO: Need to figure out how this would occur, maybe override method to support copy to different resolutions and PriceTypes.

    public Task<bool> DeleteAsync(IBarData item)
    {
      return Task.Run(async () =>
      {
        bool result = await m_repository.DeleteAsync(item);
        if (item == SelectedItem)
        {
          SelectedItemChanged?.Invoke(this, SelectedItem);
          SelectedItem = null;
        }

        Items.Remove(item);

        return result;
      });
    }

    public Task<ExportResult> ExportAsync(string filename)
    {
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        return exportCSV(filename);
      else if (extension == extensionJSON)
        return exportJSON(filename);

      //should never be reached if all supported file types are handled
      ExportResult result = new();
      StackFrame? stackFrame = new StackTrace(true).GetFrame(0);
      result.Severity = IDialogService.StatusMessageSeverity.Error;
      result.StatusMessage = $"Invalid program state: Unable to handle file type \"{filename}\" for export. ({stackFrame!.GetFileName()}:{stackFrame!.GetFileLineNumber()})";
      return Task.FromResult(result);
    }

    public Task<ImportResult> ImportAsync(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        return importCSV(importSettings);
      else if (extension == extensionJSON)
        return importJSON(importSettings);

      //should never be reached if all supported file types are handled
      ImportResult result = new();
      StackFrame? stackFrame = new StackTrace(true).GetFrame(0);
      result.Severity = IDialogService.StatusMessageSeverity.Error;
      result.StatusMessage = $"Invalid program state: Unable to handle file type \"{importSettings.Filename}\" for import. ({stackFrame!.GetFileName()}:{stackFrame!.GetFileLineNumber()})";
      return Task.FromResult(result);
    }

    public Task RefreshAsync()
    {
      return Task.Run(async () =>
      {
        var result = await m_repository.GetItemsAsync();
        Items.Clear();
        SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
        foreach (var item in result) Items.Add(item);
        if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
      });
    }

    public Task<IBarData> UpdateAsync(IBarData item)
    {
      return Task.Run(async () =>
      {
        IBarData barData = await m_repository.UpdateAsync(item);

        //the bar editor does not allow modification of the DateTime and Synthetic settings
        for (int i = 0; i < Items.Count(); i++)
          if (barData.Equals(item))
          {
            Items.RemoveAt(i);
            Items.Insert(i, barData);
            return barData;
          }

        return barData;
      });
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

    public ObservableCollection<IBarData> Items { get; set; }

    //methods
    /// <summary>
    /// Import the bar data from a csv file that has the following formats:
    ///   datetime, open, high, low, close, volume[, id, synthetic]
    /// OR
    ///   date, time, open, high, low, close, volume[, id, synthetic]
    /// If synthetic field is not present the default synthetic setting is used from the ImportSettings structure.
    /// Fields can be in any order but the above values are required.
    /// </summary>
    private Task<ImportResult> importCSV(ImportSettings importSettings)
    {
      return Task.Run(() =>
      {
        ImportResult result = new ImportResult();
        ILogger logger = m_loggerFactory.CreateLogger($"Importing instruments - \"{importSettings.Filename}\"");
        long barsUpdated = 0;

        //make sure we can retrieve the primary exhange for the instrument if we are importing date/time in the Exchange timezone
        Exchange? exchange = null;
        if (importSettings.DateTimeTimeZone == ImportDataDateTimeTimeZone.Exchange)
        {
          exchange = m_dataStoreService.GetExchange(Instrument!.PrimaryExchangeId);
          if (exchange == null)
          {
            logger.LogError($"Failed to find exchange with id \"{Instrument!.PrimaryExchangeId.ToString()}\" can not import data based on Exchange.");
            result.Severity = IDialogService.StatusMessageSeverity.Error;
            return result;
          }
        }

        //try to import the data from the file
        using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          //read the header record
          result.Severity = IDialogService.StatusMessageSeverity.Success;
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
            //synthetic field not required, will default it if not found
            for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
            {
              if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDateTime)
                dateTimeFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDate)
                dateFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTime)
                timeFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvOpen)
                openFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvHigh)
                highFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvLow)
                lowFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvClose)
                closeFound = true;
              else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvVolume)
                volumeFound = true;
            }

            if (!dateTimeFound && !(dateFound && timeFound))
            {
              result.StatusMessage = "DateTime or (Date and Time) fields are required to import bar data.";
              result.Severity = IDialogService.StatusMessageSeverity.Error;
              logger.LogError(result.StatusMessage);
            }

            if (!openFound || !highFound || !lowFound || !closeFound || !volumeFound)
            {
              result.StatusMessage = "Open, high, low, close and volume fields are required to import bar data.";
              result.Severity = IDialogService.StatusMessageSeverity.Error;
              logger.LogError(result.StatusMessage);
            }

            //parse the file data, first load the bars into a cache and then mass update the database (mass update is faster)
            List<IBarData> bars = new List<IBarData>();
            while (csv.Read() && result.Severity != IDialogService.StatusMessageSeverity.Error)
            {
              DateTime? dateTime = null;
              DateOnly? date = null;
              TimeOnly? time = null;
              double open = 0.0;
              double high = 0.0;
              double low = 0.0;
              double close = 0.0;
              long volume = 0;
              bool synthetic = importSettings.DefaultPriceDataType == ImportDefaultPriceDataType.Synthetic; //default synthetic bar settings

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
                    else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvSynthetic)
                      synthetic = bool.Parse(columnValue!);
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

                bars.Add(new BarData(Resolution, (DateTime)dateTime!, open, high, low, close, volume, synthetic));                
                barsUpdated++; //we do not check for create since it would mean we need to search through all the data constantly
              }
              catch (Exception e)
              {
                result.Severity = IDialogService.StatusMessageSeverity.Error;
                result.StatusMessage = $"Failed to parse bar on line {lineNo} with exception \"{e.Message}\".";
                logger.LogError(result.StatusMessage);
                bars.Clear();
              }
            }

            if (bars.Count != 0) m_repository.UpdateAsync(bars);
          }
          else
          {
            result.Severity = IDialogService.StatusMessageSeverity.Error;
            result.StatusMessage = $"Unable to parse header.";
            logger.LogError(result.StatusMessage);
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Created/updated {barsUpdated} bars from \"{importSettings.Filename}\".";
        return result;
      });
    }

    private Task<ImportResult> importJSON(ImportSettings importSettings)
    {
      return Task.Run(() =>
      {
        ImportResult result = new ImportResult();
        long barIndex = 0;
        long barsUpdated = 0;
        ILogger logger = m_loggerFactory.CreateLogger($"Importing \"{importSettings.Filename}\"");

        //make sure we can retrieve the primary exhange for the instrument if we are importing date/time in the Exchange timezone
        Exchange? exchange = null;
        if (importSettings.DateTimeTimeZone == ImportDataDateTimeTimeZone.Exchange)
        {
          exchange = m_dataStoreService.GetExchange(Instrument!.PrimaryExchangeId);
          if (exchange == null)
          {
            result.Severity = IDialogService.StatusMessageSeverity.Error;
            result.StatusMessage = $"Failed to find exchange with id \"{Instrument!.PrimaryExchangeId.ToString()}\" can not import data based on Exchange.";
            logger.LogError(result.StatusMessage);
            return result;
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
                result.Severity = IDialogService.StatusMessageSeverity.Error;
                result.StatusMessage = $"Bar at index {barIndex} does not contain a valid date/time specification.";  //JSON library contains zero information on where it found this data so the best we can do is use an index.
                logger.LogError(result.StatusMessage);
                continue; //skip to next bar definition
              }

              double open = barDataJson![tokenJsonOpen]!.AsValue().Deserialize<double>();
              double high = barDataJson![tokenJsonHigh]!.AsValue().Deserialize<double>();
              double low = barDataJson![tokenJsonLow]!.AsValue().Deserialize<double>();
              double close = barDataJson![tokenJsonClose]!.AsValue().Deserialize<double>();
              long volume = barDataJson![tokenJsonVolume]!.AsValue().Deserialize<long>();
              bool synthetic = barDataJson.ContainsKey(tokenJsonSynthetic) ? barDataJson![tokenJsonSynthetic]!.AsValue().Deserialize<bool>() : importSettings.DefaultPriceDataType == ImportDefaultPriceDataType.Synthetic;
              bars.Add(new BarData(Resolution, dateTime, open, high, low, close, volume, synthetic));
              barsUpdated++; //we do not check for create since it would mean we need to search through all the data constantly
            }

            if (bars.Count > 0) m_repository.UpdateAsync(bars);
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Created/updated {barsUpdated} bars from \"{importSettings.Filename}\".";
        return result;
      });
    }

    //NOTE: Export always writes out the data in the Exchange time-zone, so the import settings structure defaults to Exchange time-zone.
    private Task<ExportResult> exportCSV(string filename)
    {
      return Task.Run<ExportResult>(() =>
      {
        ExportResult result = new();
        long exportCount = 0;
        ILogger logger = m_loggerFactory.CreateLogger($"Exporting instruments - \"{filename}\"");

        Exchange? exchange = m_dataStoreService.GetExchange(Instrument!.PrimaryExchangeId);

        if (exchange == null)
        {
          result.Severity = IDialogService.StatusMessageSeverity.Error;
          result.StatusMessage = $"Date/time exported without UTC conversion, failed to find primary exchange \"{Instrument!.PrimaryExchangeId.ToString()}\" associated with instrument \"{Instrument!.Ticker}\".";
          logger.LogWarning(result.StatusMessage);
        }

        using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
        {
          file.WriteLine($"{tokenCsvDateTime},{tokenCsvOpen},{tokenCsvHigh},{tokenCsvLow},{tokenCsvClose},{tokenCsvVolume},{tokenCsvSynthetic}");

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
            barDataStr += ", ";
            barDataStr += barData.Synthetic.ToString();
            file.WriteLine(barDataStr);
            exportCount++;
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Exported {exportCount} bars to \"{filename}\".";
        return result;
      });
    }

    //NOTE: Export always writes out the data in the Exchange time-zone, so the import defaults to Exchange time-zone.
    private Task<ExportResult> exportJSON(string filename)
    {
      return Task.Run<ExportResult>(() =>
      {
        ExportResult result = new();
        long exportCount = 0;

        ILogger logger = m_loggerFactory.CreateLogger($"Exporting instruments - \"{filename}\"");

        Exchange? exchange = m_dataStoreService.GetExchange(Instrument!.PrimaryExchangeId);

        if (exchange == null)
        {
          result.Severity = IDialogService.StatusMessageSeverity.Error;
          result.StatusMessage = $"Date/time exported without UTC conversion, failed to find primary exchange \"{Instrument!.PrimaryExchangeId.ToString()}\" associated with instrument \"{Instrument!.Ticker}\".";
          logger.LogWarning(result.StatusMessage);
        }
        else
        {
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
                [tokenJsonSynthetic] = barData.Synthetic,
              };

              file.Write(barDataJson.ToJsonString(options));
              exportCount++;
              if (barDataIndex < barDataCount - 1) file.WriteLine(",");
            }
            file.WriteLine("");
            file.WriteLine("]");
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Exported {exportCount} bars to \"{filename}\".";
        return result;
      });
    }
  }
}
