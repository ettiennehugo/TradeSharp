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
  public partial class InstrumentService : ServiceBase, IInstrumentService
  {
    //constants
    /// <summary>
    /// Supported filter fields for the instrument service.
    /// </summary>
    public const string FilterTicker = "Ticker";
    public const string FilterName = "Name";
    public const string FilterDescription = "Description";

    public int DefaultPageSize = 1000;

    // Tokens used by the import/export functionality.
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
    public InstrumentService(ILoggerFactory loggerFactory, IDatabase database, IInstrumentRepository instrumentRepository, IDialogService dialogService): base(dialogService)
    {
      m_loggerFactory = loggerFactory;
      m_instrumentRepository = instrumentRepository;
      m_database = database;
      m_selectedItem = null;
      Items = new ObservableCollection<Instrument>();
    }

    //finalizers


    //interface implementations
    public bool Add(Instrument item)
    {
      var result = m_instrumentRepository.Add(item);
      Utilities.SortedInsert(item, Items);
      SelectedItem = item;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Copy(Instrument item)
    {
      Instrument clone = (Instrument)item.Clone();
      clone.Id = Guid.NewGuid();
      var result = m_instrumentRepository.Add(clone);
      Utilities.SortedInsert(clone, Items);
      SelectedItem = clone;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Delete(Instrument item)
    {
      bool result = m_instrumentRepository.Delete(item);

      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }

      return result;
    }

    public void Refresh()
    {


      //TODO: When incremental loading is implemented this will need to just raise an exception since it does NOT support loading ALL items all at once.
      

      var result = m_instrumentRepository.GetItems();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public bool Update(Instrument item)
    {
      var result = m_instrumentRepository.Update(item);
      Utilities.UpdateItem(item, Items);
      return result;
    }

    public void Import(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        importCSV(importSettings);
      else if (extension == extensionJSON)
        importJSON(importSettings);
    }

    public void Export(string filename)
    {
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        exportCSV(filename);
      else if (extension == extensionJSON)
        exportJSON(filename);
    }

    public int GetCount()
    {
      return m_instrumentRepository.GetCount();
    }

    public int GetCount(InstrumentType instrumentType)
    {
      return m_instrumentRepository.GetCount(instrumentType);
    }

    public int GetCount(string tickerFilter, string nameFilter, string descriptionFilter)
    {
      return m_instrumentRepository.GetCount(tickerFilter, nameFilter, descriptionFilter);
    }

    public int GetCount(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter)
    {
      return m_instrumentRepository.GetCount(instrumentType, tickerFilter, nameFilter, descriptionFilter);
    }

    public IList<Instrument> GetItems(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      return m_instrumentRepository.GetItems(tickerFilter, nameFilter, descriptionFilter, offset, count);
    }

    public IList<Instrument> GetItems(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      return m_instrumentRepository.GetItems(instrumentType, tickerFilter, nameFilter, descriptionFilter, offset, count);
    }

    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }

    public event EventHandler<Instrument?>? SelectedItemChanged;
    public Instrument? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public IList<Instrument> Items { get; set; }

    //methods
    private void importJSON(ImportSettings importSettings)
    {
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;

      string statusMessage = $"Importing instruments from \"{importSettings.Filename}\"";
      ILogger logger = m_loggerFactory.CreateLogger(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      bool noErrors = true;
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
                    logger.LogWarning($"No secondary Exchange with Guid \"{secondaryExchangeId.ToString()}\" at index {index} for instrument \"{ticker}\" found, discarding it.");
                }
                else
                {
                  statusMessage = $"Failed to parse secondary Exchange Guid at index {index} for instrument \"{ticker}\".";
                  logger.LogError(statusMessage);
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
                  skippedCount++;
                  break;
                case ImportReplaceBehavior.Replace:
                  //replacing name, description, tag and all defined instruments
                  logger.LogInformation($"Replacing - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                  m_instrumentRepository.Update(new Instrument(definedInstrument.Id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                  replacedCount++;
                  break;
                case ImportReplaceBehavior.Update:
                  //updating name, description, tag and merge in defined instruments
                  logger.LogInformation($"Updating - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                  m_instrumentRepository.Update(new Instrument(definedInstrument.Id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
                  updatedCount++;
                  break;
              }
            }
            else
            {
              m_instrumentRepository.Add(new Instrument(id, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId!.Value, secondaryExchanges));
              createdCount++;
            }
          }
        }
        else
        {
          statusMessage = $"Failed to parse file \"{importSettings.Filename}\" as a JSON file.";
          logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          noErrors = false;
        }
      }

      if (noErrors) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Import success: Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}) - from \"{importSettings.Filename}\"");
      RaiseRefreshEvent();  //notify view model of changes
    }

    //methods
    /// <summary>
    /// Imports the instruments from a CSV file with the following columns:
    ///   Type, Ticker, Name, Description, Exchange, Inception Date, Tag, Attributes
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    /// </summary>
    private void importCSV(ImportSettings importSettings)
    {
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;
      string statusMessage = $"Importing instruments from \"{importSettings.Filename}\"";
      ILogger logger = m_loggerFactory.CreateLogger(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

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
              logger.LogWarning($"Failed to find exchange \"{exchange}\" for instrument \"{ticker}\", defaulting to global exchange.");

            fileInstruments.Add(new Instrument((Guid)id!, attributes, tag, type, ticker, name, description, inceptionDate, exchangeId, new List<Guid>()));
          }

          long instrumentsProcessed = 0;
          foreach (Instrument fileInstrument in fileInstruments)
          {
            if (definedInstruments.TryGetValue(fileInstrument.Ticker, out Instrument? definedInstrument))
            {
              switch (importSettings.ReplaceBehavior)
              {
                case ImportReplaceBehavior.Skip:
                  logger.LogWarning($"Skipping - {fileInstrument.Ticker}");
                  skippedCount++;
                  break;
                case ImportReplaceBehavior.Replace:
                  logger.LogInformation($"Replacing - {fileInstrument.Ticker}");
                  fileInstrument.Id = definedInstrument.Id; //make sure we replace the existing instrument
                  m_instrumentRepository.Update(fileInstrument);
                  replacedCount++;
                  break;
                case ImportReplaceBehavior.Update:
                  logger.LogInformation($"Updating - {fileInstrument.Ticker}");
                  fileInstrument.Id = definedInstrument.Id; //make sure we replace the existing instrument
                  fileInstrument.SecondaryExchangeIds = definedInstrument.SecondaryExchangeIds;
                  m_instrumentRepository.Update(fileInstrument);
                  updatedCount++;
                  break;
              }
            }
            else
            {
              logger.LogInformation($"Creating - {fileInstrument.Ticker}");
              m_instrumentRepository.Add(fileInstrument);
              createdCount++;
            }

            instrumentsProcessed++;
          }
        }
      }

      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Import from \"{importSettings.Filename}\" complete - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
      RaiseRefreshEvent();  //notify view model of changes
    }

    private void exportJSON(string filename)
    {
      long exportCount = 0;
      string statusMessage = $"Importing instruments fto \"{filename}\"";
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

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

      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Exported {exportCount} instruments to \"{filename}\".");
    }

    private void exportCSV(string filename)
    {
      long exportCount = 0;
      string statusMessage = $"Importing instruments fto \"{filename}\"";
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

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

      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Exported {exportCount} instruments to \"{filename}\".");
    }
  }
}
