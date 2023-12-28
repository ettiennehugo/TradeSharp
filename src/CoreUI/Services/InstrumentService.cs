using System.Diagnostics;
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
    private const string tokenCsvId = "id";
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
    private IDatabase m_database;
    private Instrument? m_selectedItem;

    //constructors
    public InstrumentService(ILoggerFactory loggerFactory, IDatabase database, IDialogService dialogService, IInstrumentRepository instrumentRepository)
    {
      m_loggerFactory = loggerFactory;
      m_instrumentRepository = instrumentRepository;
      m_database = database;
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
    private Task<ImportResult> importJSON(ImportSettings importSettings)
    {
      return Task.Run(() =>
      {
        ImportResult result = new ImportResult();
        long skippedCount = 0;
        long updatedCount = 0;
        long replacedCount = 0;
        long createdCount = 0;
        result.Severity = IDialogService.StatusMessageSeverity.Success; //per default assume success
        ILogger logger = m_loggerFactory.CreateLogger($"Importing \"{importSettings.Filename}\"");

        using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
        {
          JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible

          if (documentNode != null)
          {
            SortedDictionary<string, Exchange> definedExchanges = new SortedDictionary<string, Exchange>();
            foreach (Exchange exchange in m_database.GetExchanges()) definedExchanges.Add(exchange.Id.ToString(), exchange);
            SortedDictionary<Guid, Instrument> definedInstruments = new SortedDictionary<Guid, Instrument>();
            foreach (Instrument instrument in m_database.GetInstruments()) definedInstruments.Add(instrument.Id, instrument);

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
                      result.Severity = IDialogService.StatusMessageSeverity.Warning;
                      logger.LogWarning($"No secondary Exchange with Guid \"{secondaryExchangeId.ToString()}\" at index {index} for instrument \"{ticker}\" found, discarding it.");
                    }
                  }
                  else
                  {
                    result.Severity = IDialogService.StatusMessageSeverity.Error;
                    result.StatusMessage = $"Failed to parse secondary Exchange Guid at index {index} for instrument \"{ticker}\".";
                    logger.LogError(result.StatusMessage);
                  }
                  index++;
                }
              }

              if (definedInstruments.TryGetValue(id, out Instrument? definedInstrument))
              {
                switch (importSettings.ReplaceBehavior)
                {
                  case ImportReplaceBehavior.Skip:
                    logger.LogWarning($"Skipping - {name}, {description}, {tag}");
                    result.Severity = IDialogService.StatusMessageSeverity.Warning;   //warn user of skipped items
                    skippedCount++;
                    break;
                  case ImportReplaceBehavior.Replace:
                    //replacing name, description, tag and all defined instruments
                    logger.LogInformation($"Replacing - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                    m_instrumentRepository.UpdateAsync(new Instrument(definedInstrument.Id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                    replacedCount++;
                    break;
                  case ImportReplaceBehavior.Update:
                    //updating name, description, tag and merge in defined instruments
                    logger.LogInformation($"Updating - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                    m_instrumentRepository.UpdateAsync(new Instrument(definedInstrument.Id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                    updatedCount++;
                    break;
                }
              }
              else
              {
                m_instrumentRepository.AddAsync(new Instrument(id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                createdCount++;
              }
            }
          }
          else
          {
            result.StatusMessage = $"Failed to parse file \"{importSettings.Filename}\" as a JSON file.";
            result.Severity = IDialogService.StatusMessageSeverity.Error;
            logger.LogError(result.StatusMessage);
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Import success: Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}) - from \"{importSettings.Filename}\"";
        return result;
      });
    }

    //methods
    /// <summary>
    /// Imports the instruments from a CSV file with the following columns:
    ///   Type, Ticker, Name, Description, Exchange, Inception Date, Tag, Attributes
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    /// </summary>
    private Task<ImportResult> importCSV(ImportSettings importSettings)
    {
      return Task.Run(() =>
      {
        ImportResult result = new ImportResult();
        long skippedCount = 0;
        long updatedCount = 0;
        long replacedCount = 0;
        long createdCount = 0;
        ILogger logger = m_loggerFactory.CreateLogger($"Importing instruments - \"{importSettings.Filename}\"");

        using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          //read the header record
          if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
          {
            int lineNo = 1; //header row is on line 0
            bool parseError = false;
            IList<Exchange> exchanges = m_database.GetExchanges();
            SortedDictionary<string, Instrument> definedInstruments = new SortedDictionary<string, Instrument>();
            foreach (Instrument instrument in m_database.GetInstruments()) definedInstruments.Add(instrument.Ticker.ToUpper(), instrument);
            List<Instrument> fileInstruments = new List<Instrument>();

            while (csv.Read() && !parseError)
            {
              InstrumentType type = InstrumentType.None;
              Guid? id = null; 
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
                  else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvId)
                    id = Guid.Parse(columnValue!);
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

              if (id == null) id = Guid.NewGuid();    //generate new instrument Id if no Id present in the CSV file
              if (description == "") description = name;
              if (tag == "") tag = ticker;

              //try to find the exchange for the instrument matching different potential attributes of the exchanges
              Exchange? primaryExchange = null;
              foreach (Exchange definedExchange in exchanges)
              {
                if (Guid.TryParse(exchange, out Guid primaryExchangeId))
                {
                  if (primaryExchangeId == definedExchange.Id)
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
              {
                result.Severity = IDialogService.StatusMessageSeverity.Warning;
                result.StatusMessage = $"Failed to find exchange \"{exchange}\" for instrument \"{ticker}\", defaulting to global exchange.";
                logger.LogWarning(result.StatusMessage);
              }

              fileInstruments.Add(new Instrument((Guid)id!, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId, new List<Guid>()));
            }

            if (fileInstruments.Count > 0) result.Severity = IDialogService.StatusMessageSeverity.Success;

            long instrumentsProcessed = 0;
            foreach (Instrument fileInstrument in fileInstruments)
            {
              if (definedInstruments.TryGetValue(fileInstrument.Ticker, out Instrument? definedInstrument))
              {
                switch (importSettings.ReplaceBehavior)
                {
                  case ImportReplaceBehavior.Skip:
                    logger.LogWarning($"Skipping - {fileInstrument.Ticker}");
                    result.Severity = IDialogService.StatusMessageSeverity.Warning;
                    skippedCount++;
                    break;
                  case ImportReplaceBehavior.Replace:
                    logger.LogInformation($"Replacing - {fileInstrument.Ticker}");
                    fileInstrument.Id = definedInstrument.Id; //make sure we replace the existing instrument
                    m_instrumentRepository.UpdateAsync(fileInstrument);
                    replacedCount++;
                    break;
                  case ImportReplaceBehavior.Update:
                    logger.LogInformation($"Updating - {fileInstrument.Ticker}");
                    fileInstrument.Id = definedInstrument.Id; //make sure we replace the existing instrument
                    fileInstrument.SecondaryExchangeIds = definedInstrument.SecondaryExchangeIds;
                    m_instrumentRepository.UpdateAsync(fileInstrument);
                    updatedCount++;
                    break;
                }
              }
              else
              {
                logger.LogInformation($"Creating - {fileInstrument.Ticker}");
                m_instrumentRepository.AddAsync(fileInstrument);
                createdCount++;
              }

              instrumentsProcessed++;
            }
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success)
          result.StatusMessage = "Import success: ";
        else
          result.StatusMessage = "Import with warning/error: " + result.StatusMessage;
        result.StatusMessage += $"Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}) - from \"{importSettings.Filename}\".";

        return result;
      });
    }

    private Task<ExportResult> exportJSON(string filename)
    {
      return Task.Run<ExportResult>(() =>
      {
        ExportResult result = new();
        long exportCount = 0;
        using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
        {
          IList<Instrument> instruments = m_database.GetInstruments();
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
            exportCount++;
            if (instrumentIndex < instrumentCount - 1) file.WriteLine(",");
          }
          file.WriteLine("");
          file.WriteLine("]");
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Exported {exportCount} instruments to \"{filename}\"";
        return result;
      });
    }

    private Task<ExportResult> exportCSV(string filename)
    {
      return Task.Run<ExportResult>(() =>
      {
        ExportResult result = new();
        long exportCount = 0;

        using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
        {
          IList<Instrument> instruments = m_database.GetInstruments();

          file.WriteLine($"{tokenCsvType},{tokenCsvId},{tokenCsvTicker},{tokenCsvName},{tokenCsvDescription},{tokenCsvExchange},{tokenCsvInceptionDate},{tokenCsvTag},{tokenCsvAttributes}");

          foreach (Instrument instrument in instruments)
          {
            string instrumentDefinition = ((int)instrument.Type).ToString();
            instrumentDefinition += ", ";
            instrumentDefinition += instrument.Id.ToString();
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
            exportCount++;
          }
        }

        if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Exported {exportCount} instruments to \"{filename}\"";
        return Task.FromResult(result);
      });
    }
  }
}
