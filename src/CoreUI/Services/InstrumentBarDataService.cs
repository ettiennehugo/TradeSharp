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
    private const string tokenCsvOpen = "open";
    private const string tokenCsvHigh = "high";
    private const string tokenCsvLow = "low";
    private const string tokenCsvClose = "close";
    private const string tokenCsvVolume = "volume";
    private const string tokenCsvSynthetic = "synthetic";
    private const string tokenJsonDateTime = "datetime";
    private const string tokenJsonOpen = "open";
    private const string tokenJsonHigh = "high";
    private const string tokenJsonLow = "low";
    private const string tokenJsonClose = "close";
    private const string tokenJsonVolume = "Volume";
    private const string tokenJsonSynthetic = "synthetic";

    //enums


    //types


    //attributes
    private IInstrumentBarDataRepository m_repository;
    private IBarData? m_selectedItem;
    private ILoggerFactory m_loggerFactory;

    //constructors
    public InstrumentBarDataService(ILoggerFactory loggerFactory)
    {
      m_loggerFactory = loggerFactory;
      m_repository = Ioc.Default.GetRequiredService<IInstrumentBarDataRepository>();  //need to do this to get a unique transient repository instance associated with this specific service
      DataProvider = string.Empty;
      Instrument = null;
      Resolution = Resolution.Day;
      m_selectedItem = null;
      Items = new ObservableCollection<IBarData>();
    }

    //finalizers


    //interface implementations
    public async Task<IBarData> AddAsync(IBarData item)
    {
      var result = await m_repository.AddAsync(item);
      Utilities.SortedInsert(item, Items);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public Task<IBarData> CopyAsync(IBarData item) => throw new NotImplementedException();  //TODO: Need to figure out how this would occur, maybe override method to support copy to different resolutions and PriceTypes.

    public async Task<bool> DeleteAsync(IBarData item)
    {
      bool result = await m_repository.DeleteAsync(item);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }

      Items.Remove(item);

      return result;
    }

    public Task<long> ExportAsync(string filename)
    {
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        return exportCSV(filename);
      else if (extension == extensionJSON)
        return exportJSON(filename);
      long result = 0;
      return Task.FromResult(result); //should never be reached
    }

    public Task<ImportReplaceResult> ImportAsync(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        return importCSV(importSettings);
      else if (extension == extensionJSON)
        return importJSON(importSettings);
      return Task.FromResult(new ImportReplaceResult());  //should never be reached
    }

    public async Task RefreshAsync()
    {
      var result = await m_repository.GetItemsAsync();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public async Task<IBarData> UpdateAsync(IBarData item)
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
    private Task<ImportReplaceResult> importCSV(ImportSettings importSettings)
    {
      return Task.Run(() =>
      {
        ImportReplaceResult result = new ImportReplaceResult();
        ILogger logger = m_loggerFactory.CreateLogger($"Importing instruments - \"{importSettings.Filename}\"");

        using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          //read the header record
          if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
          {
            int lineNo = 1; //header row is on line 0
            bool parseError = false;

            while (csv.Read() && !parseError)
            {
              DateTime dateTime = DateTime.Now;
              double open = 0.0;
              double high = 0.0;
              double low = 0.0;
              double close = 0.0;
              long volume = 0;
              bool synthetic = false;

              lineNo++;
              try
              {
                for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
                {
                  string? columnValue = null;
                  if (csv.TryGetField(columnIndex, out columnValue))
                  {
                    if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDateTime)
                      dateTime = DateTime.Parse(columnValue!);
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

                BarData barData = new BarData(Resolution, dateTime, open, high, low, close, volume, synthetic);
                m_repository.UpdateAsync(barData);
                result.Updated++; //we do not check for create since it would mean we need to search through all the data constantly
              }
              catch (Exception e) 
              {
                parseError = true;
                logger.LogError($"Failed to parse bar on line {lineNo} with exception \"{e.Message}\".");
              }
            }
          }
        }

        return result;
      });
    }

    private Task<ImportReplaceResult> importJSON(ImportSettings importSettings)
    {
      return Task.Run(() =>
      {
        ImportReplaceResult result = new ImportReplaceResult();
        result.Severity = IDialogService.StatusMessageSeverity.Success; //per default assume success
        ILogger logger = m_loggerFactory.CreateLogger($"Importing \"{importSettings.Filename}\"");

        using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
        {
          JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible

          if (documentNode != null)
          {
            JsonArray barDataArray = documentNode.AsArray();
            foreach (JsonObject? barDataJson in barDataArray)
            {
              DateTime dateTime = barDataJson![tokenJsonDateTime]!.AsValue().Deserialize<DateTime>();
              double open = barDataJson![tokenJsonOpen]!.AsValue().Deserialize<double>();
              double high = barDataJson![tokenJsonHigh]!.AsValue().Deserialize<double>();
              double low = barDataJson![tokenJsonLow]!.AsValue().Deserialize<double>();
              double close = barDataJson![tokenJsonClose]!.AsValue().Deserialize<double>();
              long volume = barDataJson![tokenJsonVolume]!.AsValue().Deserialize<long>();
              bool synthetic = barDataJson![tokenJsonSynthetic]!.AsValue().Deserialize<bool>();
              BarData barData = new BarData(Resolution, dateTime, open, high, low, close, volume, synthetic);
              m_repository.UpdateAsync(barData);
              result.Updated++; //we do not check for create since it would mean we need to search through all the data constantly
            }
          }
        }

        return result;
      });
    }

    private Task<long> exportCSV(string filename)
    {
      return Task.Run(() =>
      {
        long result = 0;
        using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
        {
          file.WriteLine($"{tokenCsvDateTime},{tokenCsvOpen},{tokenCsvHigh},{tokenCsvLow},{tokenCsvClose},{tokenCsvVolume},{tokenCsvSynthetic}");

          foreach (IBarData barData in Items)
          {
            string barDataStr = barData.DateTime.ToString();
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
            result++;
          }
        }
        return result;
      });
    }

    private Task<long> exportJSON(string filename)
    {
      return Task.Run(() =>
      {
        long result = 0;
        using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
        {
          int barDataIndex = 0;
          int barDataCount = Items.Count;
          JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

          file.WriteLine("[");
          foreach (IBarData barData in Items)
          {
            JsonObject barDataJson = new JsonObject
            {
              [tokenJsonDateTime] = barData.DateTime.ToString(),
              [tokenJsonOpen] = barData.Open,
              [tokenJsonHigh] = barData.High,
              [tokenJsonLow] = barData.Low,
              [tokenJsonClose] = barData.Close,
              [tokenJsonVolume] = barData.Volume,
              [tokenJsonSynthetic] = barData.Synthetic,
            };

            file.Write(barDataJson.ToJsonString(options));
            result++;
            if (barDataIndex < barDataCount - 1) file.WriteLine(",");
          }
          file.WriteLine("");
          file.WriteLine("]");
        }

        return result;
      });
    }
  }
}
