using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using Microsoft.Extensions.Logging;
using CsvHelper;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TradeSharp.CoreUI.Services
{
    public partial class InstrumentService : ObservableObject, IInstrumentService
  {
    //constants
    private const string extensionCSV = ".csv";
    private const string extensionJSON = ".json";
    private const string tokenCsvType = "type";
    private const string tokenCsvTicker = "ticker";
    private const string tokenCsvName = "name";
    private const string tokenCsvDescription = "description";
    private const string tokenCsvExchange = "exchange";
    private const string tokenCsvInceptionDate = "inception date";
    private const string tokenCsvTag = "tag";
    private const string tokenCsvAttributes = "attributes";
    private const string tokenJsonId = "Id";
    private const string tokenJsonType = "Type";
    private const string tokenJsonTicker = "Ticker";
    private const string tokenJsonName = "Name";
    private const string tokenJsonDescription = "Description";
    private const string tokenJsonTag = "Tag";
    private const string tokenJsonExchange = "Exchange";
    private const string tokenJsonInceptionDate = "Inception Date";
    private const string tokenJsonAttributes = "Attributes";
    private const string tokenJsonSecondaryExchanges = "SecondaryExchanges";

    //enums


    //types


    //attributes
    private ILoggerFactory m_loggerFactory;
    private IInstrumentRepository m_instrumentRepository;
    private IDataStoreService m_dataStoreService;
    private IDialogService m_dialogService;
    private Instrument? m_selectedItem;

    //constructors
    public InstrumentService(ILoggerFactory loggerFactory, IDataStoreService dataStoreService, IDialogService dialogService, IInstrumentRepository instrumentRepository)
    {
      m_loggerFactory = loggerFactory;
      m_instrumentRepository = instrumentRepository;
      m_dataStoreService = dataStoreService;
      m_dialogService = dialogService;
      m_selectedItem = null;
      Items = new ObservableCollection<Instrument>();
    }

    //finalizers


    //interface implementations
    public async Task<Instrument> AddAsync(Instrument item)
    {
      var result = await m_instrumentRepository.AddAsync(item);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<Instrument> CopyAsync(Instrument item)
    {
      Instrument clone = (Instrument)item.Clone();
      clone.Id = Guid.NewGuid();
      var result = await m_instrumentRepository.AddAsync(clone);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<bool> DeleteAsync(Instrument item)
    {
      bool result = await m_instrumentRepository.DeleteAsync(item);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_instrumentRepository.GetItemsAsync();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<Instrument> UpdateAsync(Instrument item)
    {
      return m_instrumentRepository.UpdateAsync(item);
    }

    public async Task<ImportReplaceResult> ImportAsync(ImportSettings importSettings)
    {
      ImportReplaceResult result = new ImportReplaceResult();

      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        result = await importCSV(importSettings);
      else if (extension == extensionJSON)
        result = await importJSON(importSettings);

      return result;
    }

    public async Task<long> ExportAsync(string filename)
    {
      long result = 0;

      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        result = await exportCSV(filename);
      else if (extension == extensionJSON)
        result = exportJSON(filename);

      return result;
    }

    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    
    public event EventHandler<Instrument?>? SelectedItemChanged;
    public Instrument? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public ObservableCollection<Instrument> Items { get; set; }

    //methods
    private async Task<ImportReplaceResult> importJSON(ImportSettings importSettings)
    {
      ImportReplaceResult result = new ImportReplaceResult();
      result.Severity = IDialogService.StatusMessageSeverity.Success; //per default assume success
      ILogger logger = m_loggerFactory.CreateLogger($"Importing \"{importSettings.Filename}\"");

      using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible

        if (documentNode != null)
        {
          SortedDictionary<string, Exchange> definedExchanges = new SortedDictionary<string, Exchange>();
          foreach (Exchange exchange in m_dataStoreService.GetExchanges()) definedExchanges.Add(exchange.Id.ToString(), exchange);
          SortedDictionary<Guid, Instrument> definedInstruments = new SortedDictionary<Guid, Instrument>();
          foreach (Instrument instrument in m_dataStoreService.GetInstruments()) definedInstruments.Add(instrument.Id, instrument);

          JsonArray fileInstrumentsJson = documentNode.AsArray();
          foreach (JsonObject? fileInstrumentJson in fileInstrumentsJson)
          {
            Guid id = (Guid)(fileInstrumentJson![tokenJsonId]!.AsValue().Deserialize(typeof(Guid)))!;
            string? typeStr = (string?)(fileInstrumentJson![tokenJsonType]!.AsValue().Deserialize(typeof(string)));
            InstrumentType type = (InstrumentType)Enum.Parse(typeof(InstrumentType), typeStr!);
            string ticker = (string)(fileInstrumentJson![tokenJsonTicker]!.AsValue().Deserialize(typeof(string)))!;
            string name = (string?)(fileInstrumentJson![tokenJsonName]!.AsValue().Deserialize(typeof(string))) ?? ticker;
            string description = (string?)(fileInstrumentJson![tokenJsonDescription]!.AsValue().Deserialize(typeof(string))) ?? name;
            string tag = (string?)(fileInstrumentJson![tokenJsonTag]!.AsValue().Deserialize(typeof(string))) ?? ticker;
            Guid? exchangeId = (Guid?)(fileInstrumentJson![tokenJsonExchange]!.AsValue().Deserialize(typeof(Guid)));
            string? inceptionDateStr = (string?)(fileInstrumentJson![tokenJsonInceptionDate]!.AsValue().Deserialize(typeof(string)))!;
            DateTime inceptionDate = inceptionDateStr != null ? DateTime.Parse(inceptionDateStr) : DateTime.MinValue;
            string? attributesStr = (string?)(fileInstrumentJson[tokenJsonAttributes]!.AsValue().Deserialize(typeof(string)));
            Attributes attributes = attributesStr != null ? (Attributes)Enum.Parse(typeof(Attributes), attributesStr!) : InstrumentGroup.DefaultAttributeSet;

            JsonArray? secondaryExchangesJson = fileInstrumentJson!.ContainsKey(tokenJsonSecondaryExchanges) ? fileInstrumentJson[tokenJsonSecondaryExchanges]!.AsArray() : null;
            List<Guid> secondaryExchanges = new List<Guid>();

            if (secondaryExchangesJson != null)
            {
              int index = 0;
              foreach (JsonObject? secondaryExchangeJson in secondaryExchangesJson)
              {
                Guid? secondaryExchangeId = (Guid?)(fileInstrumentJson![tokenJsonId]!.AsValue().Deserialize(typeof(Guid)));
                if (secondaryExchangeId != null)
                {
                  if (definedExchanges.TryGetValue(secondaryExchangeId.Value.ToString(), out Exchange? definedExchange)) 
                    secondaryExchanges.Add(secondaryExchangeId.Value);
                  else
                  {
                    logger.LogWarning($"No secondary Exchange with Guid \"{secondaryExchangeId.ToString()}\" at index {index} for instrument \"{ticker}\" found, discarding it.");
                    result.Severity = IDialogService.StatusMessageSeverity.Warning;
                  }
                }
                else
                {
                  logger.LogError($"Failed to parse secondary Exchange Guid at index {index} for instrument \"{ticker}\".");
                  result.Severity = IDialogService.StatusMessageSeverity.Error;
                }
                index++;
              }
            }

            if (definedInstruments.TryGetValue(id, out Instrument? definedInstrument))
            {
              switch (importSettings.ImportReplaceBehavior)
              {
                case ImportReplaceBehavior.Skip:
                  logger.LogWarning($"Skipping - {name}, {description}, {tag}");
                  result.Severity = IDialogService.StatusMessageSeverity.Warning;   //warn user of skipped items
                  result.Skipped++;
                  break;
                case ImportReplaceBehavior.Replace:
                  //replacing name, description, tag and all defined instruments
                  logger.LogInformation($"Replacing - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                  await m_instrumentRepository.UpdateAsync(new Instrument(definedInstrument.Id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                  
                  result.Replaced++;
                  break;
                case ImportReplaceBehavior.Update:
                  //updating name, description, tag and merge in defined instruments
                  logger.LogInformation($"Updating - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                  await m_instrumentRepository.UpdateAsync(new Instrument(definedInstrument.Id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                  result.Updated++;
                  break;
              }
            }
            else
            {
              await m_instrumentRepository.AddAsync(new Instrument(id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
              result.Created++;
            }
          }
        }
        else
        {
          logger.LogError("Failed to parse file as a JSON file.");
          result.Severity = IDialogService.StatusMessageSeverity.Error;
        }
      }

      return result;
    }

    //methods
    /// <summary>
    /// Imports the instruments from a CSV file with the following columns:
    ///   Type, Ticker, Name, Description, Exchange, Inception Date, Tag, Attributes
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    /// </summary>
    private async Task<ImportReplaceResult> importCSV(ImportSettings importSettings)
    {
      ImportReplaceResult result = new ImportReplaceResult();
      //await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Parsing instruments from \"{importSettings.Filename}\"");
      ILogger logger = m_loggerFactory.CreateLogger($"Importing instruments - \"{importSettings.Filename}\"");

      using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        //read the header record
        //await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Normal, 0, reader.BaseStream.Length, 0);
        if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
        {
          int lineNo = 0;
          bool parseError = false;
          IList<Exchange> exchanges = m_dataStoreService.GetExchanges();
          SortedDictionary<string, Instrument> definedInstruments = new SortedDictionary<string, Instrument>();
          foreach (Instrument instrument in m_dataStoreService.GetInstruments()) definedInstruments.Add(instrument.Ticker.ToUpper(), instrument);
          List<Instrument> fileInstruments = new List<Instrument>();

          while (csv.Read() && !parseError)
          {
            InstrumentType type = InstrumentType.None;
            string ticker = "";
            string name = "";
            string description = "";
            string exchange = "";
            DateTime inceptionDate = DateTime.MinValue;
            string tag = "";
            Attributes attributes = Instrument.DefaultAttributeSet;

            lineNo++;

            for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
            {
              string? columnValue = null;
              if (csv.TryGetField(columnIndex, out columnValue))
              {
                if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvType)
                  type = (InstrumentType)Enum.Parse(typeof(InstrumentType), columnValue!);
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTicker)
                  ticker = columnValue!.ToUpper();
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvName)
                  name = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDescription)
                  description = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvExchange)
                  exchange = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvInceptionDate)
                  inceptionDate = DateTime.Parse(columnValue!);
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTag)
                  tag = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvAttributes)
                  attributes = (Attributes)Enum.Parse(typeof(Attributes), columnValue!);
              }
            }

            if (description == "") description = name;
            if (tag == "") tag = ticker;

            //try to find the exchange for the instrument matching different potential attributes of the exchanges
            Exchange? primaryExchange = null;
            foreach (Exchange definedExchange in exchanges)
            {
              if (Guid.TryParse(exchange, out Guid id))
              {
                if (id == definedExchange.Id)
                {
                  primaryExchange = definedExchange;
                  break;
                }
              }
              else if (definedExchange.Name.ToLower() == exchange.ToLower())
              {
                primaryExchange = definedExchange;
                break;
              }
              else if (definedExchange.Tag.ToLower() == exchange.ToLower())
              {
                primaryExchange = definedExchange;
                break;
              }
            }

            Guid exchangeId = Exchange.InternationalId;
            if (primaryExchange != null)
              exchangeId = primaryExchange!.Id;
            else
              logger.LogWarning($"Failed to find exchange \"{exchange}\" for instrument \"{ticker}\", defaulting to global exchange.");

            fileInstruments.Add(new Instrument(Guid.NewGuid(), attributes, tag, type, ticker, name, description, inceptionDate, exchangeId, new List<Guid>()));
          }

          if (fileInstruments.Count > 0)
          {
            result.Severity = IDialogService.StatusMessageSeverity.Success;
          }

          long instrumentsProcessed = 0;
          foreach (Instrument fileInstrument in fileInstruments)
          {
            if (definedInstruments.TryGetValue(fileInstrument.Ticker, out Instrument? definedInstrument))
            {
              switch (importSettings.ImportReplaceBehavior)
              {
                case ImportReplaceBehavior.Skip:
                  logger.LogWarning($"Skipping - {fileInstrument.Ticker}");
                  result.Severity = IDialogService.StatusMessageSeverity.Warning;
                  result.Skipped++;
                  break;
                case ImportReplaceBehavior.Replace:
                  logger.LogInformation($"Replacing - {fileInstrument.Ticker}");
                  fileInstrument.Id = definedInstrument.Id; //make sure we replace the existing instrument
                  await m_instrumentRepository.UpdateAsync(fileInstrument);
                  result.Replaced++;
                  break;
                case ImportReplaceBehavior.Update:
                  logger.LogInformation($"Updating - {fileInstrument.Ticker}");
                  fileInstrument.Id = definedInstrument.Id; //make sure we replace the existing instrument
                  fileInstrument.SecondaryExchangeIds = definedInstrument.SecondaryExchangeIds;
                  await m_instrumentRepository.UpdateAsync(fileInstrument);
                  result.Updated++;
                  break;
              }
            }
            else
            {
              logger.LogInformation($"Creating - {fileInstrument.Ticker}");
              await m_instrumentRepository.AddAsync(fileInstrument);
              result.Created++;
            }

            instrumentsProcessed++;
            //await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Normal, 0, fileInstruments.Count, instrumentsProcessed);
          }
        }
      }

      return result;
    }

    private long exportJSON(string filename)
    {
      long result = 0;

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        IList<Instrument> instruments = m_dataStoreService.GetInstruments();
        int instrumentIndex = 0;
        int instrumentCount = instruments.Count;
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

        file.WriteLine("[");
        foreach (Instrument instrument in instruments)
        {
          JsonObject instrumentJson = new JsonObject
          {
            [tokenJsonId] = instrument.Id.ToString(),
            [tokenJsonType] = ((int)instrument.Type).ToString(),   //need to first cast to an integer otherwise it renders the tokens/words of the attribute set
            [tokenJsonTicker] = instrument.Ticker,
            [tokenJsonName] = instrument.Name,
            [tokenJsonDescription] = instrument.Description,
            [tokenJsonTag] = instrument.Tag,
            [tokenJsonExchange] = instrument.PrimaryExchangeId,
            [tokenJsonInceptionDate] = instrument.InceptionDate.ToString(),
            [tokenJsonAttributes] = ((int)instrument.AttributeSet).ToString(),   //need to first cast to an integer otherwise it renders the tokens/words of the attribute set
            [tokenJsonSecondaryExchanges] = new JsonArray()
          };

          if (instrument.SecondaryExchangeIds.Count > 0)
          {
            JsonArray secondaryExchanges = instrumentJson[tokenJsonSecondaryExchanges]!.AsArray();
            foreach (Guid secondaryExchange in instrument.SecondaryExchangeIds) secondaryExchanges.Add(secondaryExchange.ToString());
          }

          file.Write(instrumentJson.ToJsonString(options));
          result++;
          if (instrumentIndex < instrumentCount - 1) file.WriteLine(",");
        }
        file.WriteLine("");
        file.WriteLine("]");
      }

      return result;
    }

    private Task<long> exportCSV(string filename)
    {
      long result = 0;

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        //await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Exporting instruments to \"{filename}\"");
        IList<Instrument> instruments = m_dataStoreService.GetInstruments();
        //await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Normal, 0, instruments.Count, 0);

        file.WriteLine($"{tokenCsvType},{tokenCsvTicker},{tokenCsvName},{tokenCsvDescription},{tokenCsvExchange},{tokenCsvInceptionDate},{tokenCsvTag},{tokenCsvAttributes}");

        foreach (Instrument instrument in instruments)
        {
          string instrumentDefinition = ((int)instrument.Type).ToString();
          instrumentDefinition += ", ";
          instrumentDefinition += instrument.Ticker.ToUpper();
          instrumentDefinition += ", ";
          instrumentDefinition += instrument.Name;
          instrumentDefinition += ", ";
          instrumentDefinition += instrument.Description;
          instrumentDefinition += ", ";
          instrumentDefinition += instrument.PrimaryExchangeId.ToString();
          instrumentDefinition += ", ";
          instrumentDefinition += instrument.InceptionDate.ToString();
          instrumentDefinition += ", ";
          instrumentDefinition += instrument.Tag;
          instrumentDefinition += ", ";
          instrumentDefinition += ((int)instrument.AttributeSet).ToString();
          file.WriteLine(instrumentDefinition);
          result++;
          //await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Normal, 0, instruments.Count, result);
        }
      }

      return Task.FromResult(result);
    }
  }
}
