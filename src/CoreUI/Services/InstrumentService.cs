﻿using System.Collections.ObjectModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using Microsoft.Extensions.Logging;
using CsvHelper;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Events;

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

    // Tokens used by the import/export functionality.
    // NOTE: When multiple tokens are supported we use the first one as the primary token and the others as secondary tokens.
    private const string extensionCSV = ".csv";
    private const string extensionJSON = ".json";
    private const string tokenCsvType = "type";
    private const string tokenCsvTicker = "ticker";
    private const string tokenCsvAlternateTickers = "alternatetickers";
    private const string tokenCsvName = "name";
    private const string tokenCsvDescription = "description";
    private const string tokenCsvExchange = "exchange";
    private const string tokenCsvInceptionDate1 = "inception date";
    private const string tokenCsvInceptionDate2 = "inceptiondate";
    private const string tokenCsvPriceDecimals1 = "price decimals";
    private const string tokenCsvPriceDecimals2 = "pricedecimals";
    private const string tokenCsvMinimumMovement1 = "minimum movement";
    private const string tokenCsvMinimumMovement2 = "minimummovement";
    private const string tokenCsvBigPointValue1 = "big point value";
    private const string tokenCsvBigPointValue2 = "bigpointvalue";
    private const string tokenCsvBigPointValue3 = "bpv";
    private const string tokenCsvMarketCap1 = "marketcap";
    private const string tokenCsvMarketCap2 = "market cap";
    private const string tokenCsvMarketCap3 = "mrktcap";
    private const string tokenCsvMarketCap4 = "mrkt cap";
    private const string tokenCsvTag = "tag";
    private const string tokenCsvSecondaryExchanges1 = "secondaryexchanges";
    private const string tokenCsvSecondaryExchanges2 = "secondary exchanges";
    private const string tokenCsvAttributes = "attributes";
    private const string tokenJsonType = "Type";
    private const string tokenJsonTicker = "Ticker";
    private const string tokenJsonAlternateTickers = "AlternateTickers";
    private const string tokenJsonName = "Name";
    private const string tokenJsonDescription = "Description";
    private const string tokenJsonTag = "Tag";
    private const string tokenJsonExchange = "Exchange";
    private const string tokenJsonInceptionDate1 = "Inception Date";
    private const string tokenJsonInceptionDate2 = "InceptionDate";
    private const string tokenJsonPriceDecimals1 = "price decimals";
    private const string tokenJsonPriceDecimals2 = "pricedecimals";
    private const string tokenJsonMinimumMovement1 = "minimum movement";
    private const string tokenJsonMinimumMovement2 = "minimummovement";
    private const string tokenJsonBigPointValue1 = "big point value";
    private const string tokenJsonBigPointValue2 = "bigpointvalue";
    private const string tokenJsonBigPointValue3 = "bpv";
    private const string tokenJsonMarketCap1 = "marketcap";
    private const string tokenJsonMarketCap2 = "market cap";
    private const string tokenJsonMarketCap3 = "mrkt cap";
    private const string tokenJsonMarketCap4 = "mrkt cap";
    private const string tokenJsonAttributes = "Attributes";
    private const string tokenJsonSecondaryExchanges = "SecondaryExchanges";

    //enums


    //types


    //attributes
    private ILogger<InstrumentService> m_logger;
    private IInstrumentRepository m_instrumentRepository;
    private IInstrumentCacheService m_instrumentCacheService;
    private IExchangeService m_exchangeService;
    private Instrument? m_selectedItem;

    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }

    public event EventHandler<Instrument?>? SelectedItemChanged;
    public Instrument? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public IList<Instrument> Items { get; set; }
    public IInstrumentCacheService Cache { get => m_instrumentCacheService; }

    //constructors
    public InstrumentService(ILogger<InstrumentService> logger, IExchangeService exchangeService, IInstrumentRepository instrumentRepository, IInstrumentCacheService instrumentCacheService, IDialogService dialogService) : base(dialogService)
    {
      m_logger = logger;
      m_instrumentRepository = instrumentRepository;
      m_instrumentCacheService = instrumentCacheService;
      m_instrumentCacheService.Refreshed += onCacheRefresh;
      m_exchangeService = exchangeService;
      m_selectedItem = null;
      Items = new ObservableCollection<Instrument>();
    }

    //finalizers
    ~InstrumentService()
    {
      m_instrumentCacheService.Refreshed -= onCacheRefresh;
    }

    //interface implementations
    public bool Add(Instrument item)
    {
      var result = m_instrumentRepository.Add(item);
      m_instrumentCacheService.Add(item);
      TradeSharp.Common.Utilities.SortedInsert(item, Items);
      SelectedItem = item;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Copy(Instrument item)
    {
      Instrument clone = (Instrument)item.Clone();
      clone.Id = Guid.NewGuid();
      var result = m_instrumentRepository.Add(clone);
      m_instrumentCacheService.Add(clone);
      TradeSharp.Common.Utilities.SortedInsert(clone, Items);
      SelectedItem = clone;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Delete(Instrument item)
    {
      bool result = m_instrumentRepository.Delete(item);
      m_instrumentCacheService.Delete(item);

      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }

      return result;
    }

    public void Refresh()
    {
      //need to lock the refresh in case the cache is being updated by another thread
      lock (this)
      {
        LoadedState = Common.LoadedState.Loading;
        Items.Clear();
        SelectedItem = m_instrumentCacheService.Items.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null      
        foreach (var item in m_instrumentCacheService.Items) Items.Add(item);
        if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
        LoadedState = Items.Count > 0 ? Common.LoadedState.Loaded : Common.LoadedState.NotLoaded;
        if (LoadedState == Common.LoadedState.Loaded) raiseRefreshEvent();
      }
    }

    public bool Update(Instrument item)
    {
      var result = m_instrumentRepository.Update(item);
      m_instrumentCacheService.Update(item);
      for (int i = 0; i < Items.Count(); i++)
        if (item.Equals(Items[i]))
        {
          Items[i].Update(item);
          return result;
        }

      return result;
    }

    public override void Import(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        importCSV(importSettings);
      else if (extension == extensionJSON)
        importJson(importSettings);
      //NOTE: We do not update the lookup for ticker to id mappings here as the refresh should be called by the view model after the import is complete.
    }

    public override void Export(ExportSettings exportSettings)
    {
      string extension = Path.GetExtension(exportSettings.Filename).ToLower();
      if (extension == extensionCSV)
        exportCsv(exportSettings);
      else if (extension == extensionJSON)
        exportJson(exportSettings);
    }

    public int GetCount()
    {
      return m_instrumentCacheService.Items.Count;
    }

    public int GetCount(InstrumentType instrumentType)
    {
      return m_instrumentCacheService.Items.Select((instrument, index) => instrument.Type == instrumentType).Count();
    }

    public int GetCount(string tickerFilter, string nameFilter, string descriptionFilter)
    {
      return m_instrumentCacheService.Items.Select((instrument, index) => (tickerFilter.Length > 0 && instrument.Ticker == tickerFilter) || (nameFilter.Length > 0 && instrument.Name.Contains(nameFilter)) || (descriptionFilter.Length > 0 && instrument.Description.Contains(descriptionFilter))).Count();
    }

    public int GetCount(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter)
    {
      return m_instrumentCacheService.Items.Select((instrument, index) => instrument.Type == instrumentType && ((tickerFilter.Length > 0 && instrument.Ticker == tickerFilter) || (nameFilter.Length > 0 && instrument.Name.Contains(nameFilter)) || (descriptionFilter.Length > 0 && instrument.Description.Contains(descriptionFilter)))).Count();
    }

    public Instrument? GetItem(string ticker)
    {
      return m_instrumentCacheService.Items.FirstOrDefault(x => x.Ticker == ticker || x.AlternateTickers.Contains(ticker));
    }

    public IList<Instrument> GetItems(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      return m_instrumentCacheService.Items.Where(instrument => (tickerFilter.Length > 0 && (instrument.Ticker == tickerFilter || instrument.AlternateTickers.Contains(tickerFilter))) || (nameFilter.Length > 0 && instrument.Name.Contains(nameFilter)) || (descriptionFilter.Length > 0 && instrument.Description.Contains(descriptionFilter))).Skip(offset).Take(count).ToList();
    }

    public IList<Instrument> GetItems(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      return m_instrumentCacheService.Items.Where(instrument => instrument.Type == instrumentType && ((tickerFilter.Length > 0 && (instrument.Ticker == tickerFilter || instrument.AlternateTickers.Contains(tickerFilter))) || (nameFilter.Length > 0 && instrument.Name.Contains(nameFilter)) || (descriptionFilter.Length > 0 && instrument.Description.Contains(descriptionFilter)))).Skip(offset).Take(count).ToList();
    }

    private void onCacheRefresh(object sender, RefreshEventArgs args)
    {
      Refresh();
    }

    ///Method is defined public for testing purposes.
    public void importJson(ImportSettings importSettings)
    {
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;

      string statusMessage = $"Importing instruments from \"{importSettings.Filename}\"";
      if (Debugging.ImportExport) m_logger.LogInformation(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      bool noErrors = true;
      IFileSystemService fileSystemService = (IFileSystemService)IApplication.Current.Services.GetService(typeof(IFileSystemService))!;
      using (StreamReader file = fileSystemService.OpenFile(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible

        if (documentNode != null)
        {
          Exchange globalExchange = m_exchangeService.Items.First(x => x.Id == Exchange.InternationalId);    //global exchange must always exist
          JsonArray fileInstrumentsJson = documentNode.AsArray();
          foreach (JsonObject? fileInstrumentJson in fileInstrumentsJson)
          {
            try
            {
              string ticker = (string)(fileInstrumentJson![tokenJsonTicker]!.AsValue().Deserialize(typeof(string)))!;
              ticker = ticker.Trim().ToUpper();
              string name = (string?)(fileInstrumentJson![tokenJsonName]!.AsValue().Deserialize(typeof(string))) ?? ticker;
              string description = (string?)(fileInstrumentJson![tokenJsonDescription]!.AsValue().Deserialize(typeof(string))) ?? name;
              InstrumentType type = InstrumentType.None;
              string? typeStr = (string?)(fileInstrumentJson![tokenJsonType]!.AsValue().Deserialize(typeof(string)));
              if ((typeStr == null || !Enum.TryParse(typeStr, out type)) && Debugging.ImportExport) m_logger.LogError($"Failed to parse instrument type for instrument \"{ticker}\", defaulting to None.");
              string tag = fileInstrumentJson![tokenJsonTag]!.ToJsonString() ?? TagValue.EmptyJson;
              string? exchangeStr = (string?)(fileInstrumentJson![tokenJsonExchange]!.AsValue().Deserialize(typeof(string)));
              DateTime inceptionDate = Constants.DefaultMinimumDateTime;
              string? inceptionDateStr = fileInstrumentJson.ContainsKey(tokenJsonInceptionDate1) ? (string?)(fileInstrumentJson![tokenJsonInceptionDate1]!.AsValue().Deserialize(typeof(string))) : null;
              if (inceptionDateStr == null) inceptionDateStr = fileInstrumentJson.ContainsKey(tokenJsonInceptionDate2) ? (string?)(fileInstrumentJson![tokenJsonInceptionDate2]!.AsValue().Deserialize(typeof(string))) : null;
              if ((inceptionDateStr == null || !DateTime.TryParse(inceptionDateStr, null, DateTimeStyles.None, out inceptionDate)) && Debugging.ImportExport) m_logger.LogError($"Failed to parse inception date for instrument \"{ticker}\", defaulting to default minimum date.");
              int priceDecimals = fileInstrumentJson.ContainsKey(tokenJsonPriceDecimals1) ? (int?)(fileInstrumentJson![tokenJsonPriceDecimals1]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              if (priceDecimals == -1) priceDecimals = fileInstrumentJson.ContainsKey(tokenJsonPriceDecimals2) ? (int?)(fileInstrumentJson![tokenJsonPriceDecimals2]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              int minimumMovement = fileInstrumentJson.ContainsKey(tokenJsonMinimumMovement1) ? (int?)(fileInstrumentJson![tokenJsonMinimumMovement1]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              if (minimumMovement == -1) minimumMovement = fileInstrumentJson.ContainsKey(tokenJsonMinimumMovement2) ? (int?)(fileInstrumentJson![tokenJsonMinimumMovement2]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              int bigPointValue = fileInstrumentJson.ContainsKey(tokenJsonBigPointValue1) ? (int?)(fileInstrumentJson![tokenJsonBigPointValue1]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              if (bigPointValue == -1) bigPointValue = fileInstrumentJson.ContainsKey(tokenJsonBigPointValue2) ? (int?)(fileInstrumentJson![tokenJsonBigPointValue2]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              if (bigPointValue == -1) bigPointValue = fileInstrumentJson.ContainsKey(tokenJsonBigPointValue3) ? (int?)(fileInstrumentJson![tokenJsonBigPointValue3]!.AsValue().Deserialize(typeof(int))) ?? -1 : -1;
              double marketCap = fileInstrumentJson.ContainsKey(tokenJsonMarketCap1) ? (double?)(fileInstrumentJson[tokenJsonMarketCap1]!.AsValue().Deserialize(typeof(double))) ?? 0 : 0;
              if (marketCap == 0) marketCap = fileInstrumentJson.ContainsKey(tokenJsonMarketCap2) ? (double?)(fileInstrumentJson[tokenJsonMarketCap2]!.AsValue().Deserialize(typeof(double))) ?? 0 : 0;
              if (marketCap == 0) marketCap = fileInstrumentJson.ContainsKey(tokenJsonMarketCap3) ? (double?)(fileInstrumentJson[tokenJsonMarketCap3]!.AsValue().Deserialize(typeof(double))) ?? 0 : 0;
              if (marketCap == 0) marketCap = fileInstrumentJson.ContainsKey(tokenJsonMarketCap4) ? (double?)(fileInstrumentJson[tokenJsonMarketCap4]!.AsValue().Deserialize(typeof(double))) ?? 0 : 0;

              //attribute can be parsed a string or integer or integer string value ("3") - we support conversion of all three cases
              Attributes attributes = InstrumentGroup.DefaultAttributes;
              JsonNode? attributesNode = fileInstrumentJson.ContainsKey(tokenJsonAttributes) ? fileInstrumentJson[tokenJsonAttributes] : null;
              if (attributesNode != null)
              {
                if (attributesNode.AsValue().TryGetValue(out string? attributesStr))
                {
                  if (!Enum.TryParse(attributesStr!, out attributes) && Debugging.ImportExport) m_logger.LogError($"Failed to parse attributes \"{attributesStr}\" for instrument \"{ticker}\", defaulting to default attribute set.");
                }
                else if (attributesNode.AsValue().TryGetValue(out int attributesInt))
                  attributes = (Attributes)attributesInt;
              }

              JsonArray? alternateTickerJson = fileInstrumentJson!.ContainsKey(tokenJsonAlternateTickers) ? fileInstrumentJson[tokenJsonAlternateTickers]!.AsArray() : null;
              List<string> alternateTickers = new List<string>();
              if (alternateTickerJson != null)
                foreach (JsonNode? alternateTicker in alternateTickerJson)
                {
                  string? alternateTickerStr = (string?)(alternateTicker!.AsValue().Deserialize(typeof(string)));
                  if (alternateTickerStr != null) alternateTickers.Add(alternateTickerStr.Trim().ToUpper());
                }

              //setup the primary exchange
              Exchange? primaryExchange = null;
              if (exchangeStr != null)
              {
                if (Guid.TryParse(exchangeStr, out Guid exchangeId))
                  primaryExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == exchangeId);
                else
                {
                  primaryExchange = m_exchangeService.Items.FirstOrDefault(e => e.Name.ToLower() == exchangeStr!.ToLower() || e.TagStr.Contains(exchangeStr));

                  if (primaryExchange == null)
                  {
                    foreach (var exchangeItem in m_exchangeService.Items)
                    {
                      foreach (var alternateName in exchangeItem.AlternateNames)
                        if (alternateName.ToLower() == exchangeStr.ToLower())
                        {
                          primaryExchange = exchangeItem;
                          break;
                        }

                      if (primaryExchange != null) break;
                    }
                  }
                }
              }

              if (primaryExchange == null)
              {
                if (Debugging.ImportExport) m_logger.LogError($"Failed to parse or find exchange \"{exchangeStr}\" for instrument \"{ticker}\", defaulting to global exchange.");
                primaryExchange = globalExchange;
              }

              //NOTE: We do not set the primary exchange price decimals, minimum movement and big point value here, as these are set by the exchange itself.

              //process the secondary exchanges
              JsonArray? secondaryExchangesJson = fileInstrumentJson!.ContainsKey(tokenJsonSecondaryExchanges) ? fileInstrumentJson[tokenJsonSecondaryExchanges]!.AsArray() : null;
              List<Guid> secondaryExchanges = new List<Guid>();
              if (secondaryExchangesJson != null)
              {
                int index = 0;
                foreach (JsonNode? secondaryExchangeJson in secondaryExchangesJson)
                {
                  string secondaryExchangeStr = (string)(secondaryExchangeJson!.AsValue().Deserialize(typeof(string)))!;
                  if (secondaryExchangeStr != null)
                  {
                    secondaryExchangeStr = secondaryExchangeStr.Trim();
                    //first try to process the secondary exchange using it's Id
                    if (Guid.TryParse(secondaryExchangeStr, out Guid secondaryExchangeId))
                      secondaryExchanges.Add(secondaryExchangeId);
                    else
                    {
                      //try to match the secondary exchange using it's name or tag
                      Exchange? definedExchange = m_exchangeService.Items.FirstOrDefault(e => e.Name.ToLower() == secondaryExchangeStr.ToLower() || e.TagStr.Contains(secondaryExchangeStr));

                      if (definedExchange == null)
                      {
                        foreach (var exchangeItem in m_exchangeService.Items)
                        {
                          foreach (var alternateName in exchangeItem.AlternateNames)
                            if (alternateName.ToLower() == secondaryExchangeStr.ToLower())
                            {
                              definedExchange = exchangeItem;
                              break;
                            }

                          if (definedExchange != null) break;
                        }
                      }

                      if (definedExchange != null)
                        secondaryExchanges.Add(definedExchange.Id);
                      else
                        if (Debugging.ImportExport) m_logger.LogWarning($"No secondary Exchange with Guid/Name \"{secondaryExchangeStr}\" at index {index} for instrument \"{ticker}\" found, discarding it.");
                    }
                  }
                  else
                    if (Debugging.ImportExport) m_logger.LogWarning($"Failed to parse secondary exchange at index {index} for instrument \"{ticker}\" found, discarding it.");

                  index++;
                }
              }

              //create or update the instrument as required
              Instrument? fileInstrument = null;
              if (type == InstrumentType.Stock)
              {
                var stock = new Stock(ticker, attributes, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, primaryExchange.Id, secondaryExchanges, string.Empty);
                stock.MarketCap = marketCap;
                fileInstrument = stock;
              }
              else
                fileInstrument = new Instrument(ticker, attributes, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, primaryExchange.Id, secondaryExchanges, string.Empty);

              Instrument? definedInstrument = m_instrumentCacheService.Items.FirstOrDefault(x => x.Equals(fileInstrument));
              if (definedInstrument != null)
              {
                switch (importSettings.ReplaceBehavior)
                {
                  case ImportReplaceBehavior.Skip:
                    if (Debugging.ImportExport) m_logger.LogWarning($"Skipping - {name}, {description}, {tag}");
                    skippedCount++;
                    break;
                  case ImportReplaceBehavior.Replace:
                    //replacing name, description, tag and all data from the input file
                    if (Debugging.ImportExport) m_logger.LogInformation($"Replacing - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                    m_instrumentRepository.Update(fileInstrument);
                    m_instrumentCacheService.Update(fileInstrument);
                    replacedCount++;
                    break;
                  case ImportReplaceBehavior.Update:
                    //updating name, description, tag and merge in list data into the defined instrument
                    if (Debugging.ImportExport) m_logger.LogInformation($"Updating - {definedInstrument.Name}, {definedInstrument.Description}, {definedInstrument.Tag} => {name}, {description}, {tag}");
                    alternateTickers = alternateTickers.Union(definedInstrument.AlternateTickers).ToList();
                    secondaryExchanges = secondaryExchanges.Union(definedInstrument.SecondaryExchangeIds).ToList();

                    if (type == InstrumentType.Stock)
                    {
                      var stock = new Stock(ticker, attributes, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, primaryExchange.Id, secondaryExchanges, string.Empty);
                      stock.MarketCap = marketCap;
                      fileInstrument = stock;
                    }
                    else
                      fileInstrument = new Instrument(ticker, attributes, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, primaryExchange.Id, secondaryExchanges, string.Empty);

                    m_instrumentRepository.Update(fileInstrument);
                    m_instrumentCacheService.Update(fileInstrument);
                    updatedCount++;
                    break;
                }
              }
              else
              {
                m_instrumentRepository.Add(fileInstrument);
                m_instrumentCacheService.Add(fileInstrument);
                createdCount++;
              }
            }
            catch (Exception ex)
            {
              statusMessage = $"Failed to parse instrument at index {fileInstrumentsJson.IndexOf(fileInstrumentJson)} for file \"{importSettings.Filename}\". Error: {ex.Message}";
              if (Debugging.ImportExport) m_logger.LogError(statusMessage);
              noErrors = false;
            }
          }
        }
        else
        {
          statusMessage = $"Failed to parse file \"{importSettings.Filename}\" as a JSON file.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          noErrors = false;
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Import success: Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}) - from \"{importSettings.Filename}\"");
      if (noErrors) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Import success: Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}) - from \"{importSettings.Filename}\"");
      raiseRefreshEvent();  //notify view model of changes
    }

    //methods
    /// <summary>
    /// Imports the instruments from a CSV file with the following columns:
    ///   Type, Ticker, Name, Description, Exchange, Inception Date, Tag, Attributes
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    /// </summary>
    ///Method is defined public for testing purposes.
    public void importCSV(ImportSettings importSettings)
    {
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;
      string statusMessage = $"Importing instruments from \"{importSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.ImportExport) loggerScope = m_logger.BeginScope(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      IFileSystemService fileSystemService = (IFileSystemService)IApplication.Current.Services.GetService(typeof(IFileSystemService))!;
      using (StreamReader reader = fileSystemService.OpenFile(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        //read the header record
        if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
        {
          int lineNo = 1; //header row is on line 0
          bool parseError = false;
          Exchange? globalExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == Exchange.InternationalId);
          List<Instrument> fileInstruments = new List<Instrument>();

          while (csv.Read() && !parseError)
          {
            InstrumentType type = InstrumentType.None;
            string ticker = "";
            IList<string> alternateTickers = new List<string>();
            string name = "";
            string description = "";
            string exchange = "";
            DateTime inceptionDate = DateTime.MinValue;
            string tag = "";
            Attributes attributes = Instrument.DefaultAttributes;
            int priceDecimals = -1;
            int minimumMovement = -1;
            int bigPointValue = -1;
            double marketCap = 0;
            IList<string> secondaryExchanges = new List<string>();

            lineNo++;

            for (int columnIndex = 0; columnIndex < csv.HeaderRecord.Count(); columnIndex++)
            {
              string? columnValue = null;
              if (csv.TryGetField(columnIndex, out columnValue))
              {
                string columnName = csv.HeaderRecord[columnIndex].ToLower();
                if (columnName == tokenCsvType)
                  type = (InstrumentType)Enum.Parse(typeof(InstrumentType), columnValue!);
                else if (columnName == tokenCsvTicker)
                  ticker = columnValue!.Trim().ToUpper();
                else if (columnName == tokenCsvAlternateTickers)
                {
                  alternateTickers = TradeSharp.Common.Utilities.FromCsv(columnValue!);
                  for (int i = 0; i < alternateTickers.Count; i++) alternateTickers[i] = alternateTickers[i].Trim().ToUpper();
                }
                else if (columnName == tokenCsvName)
                  name = columnValue!;
                else if (columnName == tokenCsvDescription)
                  description = columnValue!;
                else if (columnName == tokenCsvExchange)
                  exchange = columnValue!.Trim();
                else if (columnName == tokenCsvInceptionDate1 || columnName == tokenCsvInceptionDate2)
                {
                  if (DateTime.TryParse(columnValue!, null, DateTimeStyles.None, out DateTime parsedDateTime))
                    inceptionDate = parsedDateTime;  //we do not convert the date to the time-zone, the timezone is always the primary exchange timezone
                  else
                  {
                    if (Debugging.ImportExport) m_logger.LogError($"Failed to parse inception date \"{columnValue!}\" at line {lineNo}, defaulting to default minimum date.");
                    inceptionDate = Constants.DefaultMinimumDateTime;
                  }
                }
                else if (columnName == tokenCsvPriceDecimals1 || columnName == tokenCsvPriceDecimals2)
                {
                  if (!int.TryParse(columnValue!, out priceDecimals) && Debugging.ImportExport) m_logger.LogError($"Failed to price decimals \"{columnValue!}\" at line {lineNo}, using defaulting.");
                }
                else if (columnName == tokenCsvMinimumMovement1 || columnName == tokenCsvMinimumMovement2)
                {
                  if (!int.TryParse(columnValue!, out minimumMovement) && Debugging.ImportExport) m_logger.LogError($"Failed to minimum movement \"{columnValue!}\" at line {lineNo}, using defaulting.");
                }
                else if (columnName == tokenCsvBigPointValue1 || columnName == tokenCsvBigPointValue2 || columnName == tokenCsvBigPointValue3)
                {
                  if (!int.TryParse(columnValue!, out bigPointValue) && Debugging.ImportExport) m_logger.LogError($"Failed to big point value \"{columnValue!}\" at line {lineNo}, using defaulting.");
                }
                else if (columnName == tokenCsvSecondaryExchanges1 || columnName == tokenCsvSecondaryExchanges2)
                  secondaryExchanges = TradeSharp.Common.Utilities.FromCsv(columnValue!);
                else if (columnName == tokenCsvTag)
                  tag = columnValue!;
                else if (columnName == tokenCsvMarketCap1 || columnName == tokenCsvMarketCap2 || columnName == tokenCsvMarketCap3 || columnName == tokenCsvMarketCap4)
                {
                  if (!double.TryParse(columnValue!, out marketCap) && Debugging.ImportExport) m_logger.LogError($"Failed to parse market cap \"{columnValue!}\" at line {lineNo}, defaulting to 0.");
                }
                else if (columnName == tokenCsvAttributes)
                {
                  if (Enum.TryParse(typeof(Attributes), columnValue!, out object? parsedAttributes))
                    attributes = (Attributes)parsedAttributes;
                  else
                    if (Debugging.ImportExport) m_logger.LogError($"Failed to parse attributes at line {lineNo}, defaulting to default attribute set.");
                }
              }
            }

            if (description == "") description = name;
            if (tag == "") tag = ticker;

            //try to find the exchange for the instrument matching different potential attributes of the exchanges
            Exchange? primaryExchange = null;
            foreach (Exchange definedExchange in m_exchangeService.Items)
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
              else if (definedExchange.TagStr.Contains(exchange))
              {
                primaryExchange = definedExchange;
                break;
              }
              else
              {
                foreach (var alternateName in definedExchange.AlternateNames)
                  if (alternateName.ToLower() == exchange.ToLower())
                  {
                    primaryExchange = definedExchange;
                    break;
                  }

                if (primaryExchange != null) break;
              }
            }

            Guid exchangeId = Exchange.InternationalId;
            if (primaryExchange != null)
            {
              exchangeId = primaryExchange!.Id;
              priceDecimals = priceDecimals == -1 ? primaryExchange!.DefaultPriceDecimals : priceDecimals;
              minimumMovement = minimumMovement == -1 ? primaryExchange!.DefaultMinimumMovement : minimumMovement;
              bigPointValue = bigPointValue == -1 ? primaryExchange!.DefaultBigPointValue : bigPointValue;
            }
            else
            {
              if (Debugging.ImportExport) m_logger.LogWarning($"Failed to find exchange \"{exchange}\" for instrument \"{ticker}\", defaulting to global exchange.");
              if (globalExchange != null)
              {
                exchangeId = globalExchange!.Id;
                priceDecimals = priceDecimals == -1 ? globalExchange!.DefaultPriceDecimals : priceDecimals;
                minimumMovement = minimumMovement == -1 ? globalExchange!.DefaultMinimumMovement : minimumMovement;
                bigPointValue = bigPointValue == -1 ? globalExchange!.DefaultBigPointValue : bigPointValue;
              }
            }

            //parse secondary exchanges
            IList<Guid> secondaryExchangeIds = new List<Guid>();
            foreach (string secondaryExchange in secondaryExchanges)
            {
              Exchange? definedExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id.ToString() == secondaryExchange || e.Name.ToUpper() == secondaryExchange.ToUpper() || e.TagStr.Contains(secondaryExchange.ToUpper()));

              //try to find exchange based on alternate names
              if (definedExchange == null)
              {
                foreach (var exchangeItem in m_exchangeService.Items)
                {
                  foreach (var alternateName in exchangeItem.AlternateNames)
                    if (alternateName.ToUpper() == secondaryExchange.ToUpper())
                    {
                      definedExchange = exchangeItem;
                      break;
                    }

                  if (definedExchange != null) break;
                }
              }

              if (definedExchange != null)
                secondaryExchangeIds.Add(definedExchange.Id);
              else
                if (Debugging.ImportExport) m_logger.LogWarning($"No secondary Exchange with Guid/Name \"{secondaryExchange}\" for instrument \"{ticker}\" found, discarding it.");
            }

            Instrument? fileInstrument = null;
            if (type == InstrumentType.Stock)
            {
              var stock = new Stock(ticker, attributes, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, exchangeId, secondaryExchangeIds, string.Empty);
              stock.MarketCap = marketCap;
              fileInstrument = stock;
            }
            else
              fileInstrument = new Instrument(ticker, attributes, tag, type, alternateTickers, name, description, inceptionDate, priceDecimals, minimumMovement, bigPointValue, exchangeId, secondaryExchangeIds, string.Empty);

            fileInstruments.Add(fileInstrument);
          }

          long instrumentsProcessed = 0;
          foreach (Instrument fileInstrument in fileInstruments)
          {
            Instrument? definedInstrument = m_instrumentCacheService.Items.FirstOrDefault(x => x.Equals(fileInstrument));
            if (definedInstrument != null)
            {
              switch (importSettings.ReplaceBehavior)
              {
                case ImportReplaceBehavior.Skip:
                  if (Debugging.ImportExport) m_logger.LogWarning($"Skipping - {fileInstrument.Ticker}");
                  skippedCount++;
                  break;
                case ImportReplaceBehavior.Replace:
                  if (Debugging.ImportExport) m_logger.LogInformation($"Replacing - {fileInstrument.Ticker}");
                  m_instrumentRepository.Update(fileInstrument);
                  replacedCount++;
                  break;
                case ImportReplaceBehavior.Update:
                  if (Debugging.ImportExport) m_logger.LogInformation($"Updating - {fileInstrument.Ticker}");
                  fileInstrument.AlternateTickers = definedInstrument.AlternateTickers.Union(fileInstrument.AlternateTickers).ToList();
                  fileInstrument.SecondaryExchangeIds = definedInstrument.SecondaryExchangeIds.Union(definedInstrument.SecondaryExchangeIds).ToList();

                  definedInstrument.Update(fileInstrument);
                  m_instrumentRepository.Update(definedInstrument);
                  m_instrumentCacheService.Update(definedInstrument);
                  updatedCount++;
                  break;
              }
            }
            else
            {
              if (Debugging.ImportExport) m_logger.LogInformation($"Creating - {fileInstrument.Ticker}");
              m_instrumentRepository.Add(fileInstrument);
              m_instrumentCacheService.Add(fileInstrument);
              createdCount++;
            }

            instrumentsProcessed++;
          }
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Import from \"{importSettings.Filename}\" complete - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Import from \"{importSettings.Filename}\" complete - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
      raiseRefreshEvent();  //notify view model of changes
    }

    //Method is defined public for testing purposes.
    public void exportJson(ExportSettings exportSettings)
    {
      long exportCount = 0;
      string statusMessage = $"Importing instruments fto \"{exportSettings.Filename}\"";
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      if (exportSettings.ReplaceBehavior != ExportReplaceBehavior.Replace && File.Exists(exportSettings.Filename))
      {
        //we also need to check the file size, the Windows save dialog would sometimes create an empty file
        FileInfo fileInfo = new FileInfo(exportSettings.Filename);
        if (fileInfo.Length > 0)
        {
          statusMessage = $"File \"{exportSettings.Filename}\" already exists, but export settings are set to not replace it.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          return;
        }
      }

      Exchange? globalExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == Exchange.InternationalId);
      IFileSystemService fileSystemService = (IFileSystemService)IApplication.Current.Services.GetService(typeof(IFileSystemService))!;
      using (StreamWriter file = fileSystemService.CreateText(exportSettings.Filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        int instrumentIndex = 0;
        int instrumentCount = m_instrumentCacheService.Items.Count;

        file.WriteLine("[");
        foreach (Instrument instrument in m_instrumentCacheService.Items)
        {
          Exchange? exchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
          if (exchange == null)
          {
            if (Debugging.ImportExport) m_logger.LogInformation($"Failed to find primary exchange with Id {instrument.PrimaryExchangeId} for instrument \"{instrument.Ticker}\", using global exchange for instrument.");
            exchange = globalExchange;
          }

          JsonObject instrumentJson = new JsonObject
          {
            [tokenJsonType] = ((int)instrument.Type).ToString(),   //need to first cast to an integer otherwise it renders the tokens/words of the attribute set
            [tokenJsonTicker] = instrument.Ticker,
            [tokenJsonAlternateTickers] = new JsonArray(),
            [tokenJsonName] = instrument.Name,
            [tokenJsonDescription] = instrument.Description,
            [tokenJsonTag] = string.Empty,
            [tokenJsonExchange] = exchange!.Name,
            [tokenJsonInceptionDate1] = instrument.InceptionDate.ToString(),
            [tokenJsonPriceDecimals1] = instrument.PriceDecimals,
            [tokenJsonMinimumMovement1] = instrument.MinimumMovement,
            [tokenJsonBigPointValue1] = instrument.BigPointValue,
            [tokenJsonMarketCap1] = instrument is Stock stock ? stock.MarketCap : 0,
            [tokenJsonAttributes] = ((int)instrument.AttributeSet),   //need to first cast to an integer otherwise it renders the tokens/words of the attribute set
            [tokenJsonSecondaryExchanges] = new JsonArray()
          };

          //add the JSON tag data if it exists
          if (!string.IsNullOrEmpty(instrument.TagStr))
          {
            try
            {
              var tagJsonNode = JsonNode.Parse(instrument.TagStr);
              instrumentJson[tokenJsonTag] = tagJsonNode;
            }
            catch (JsonException ex)
            {
              m_logger.LogError($"Error parsing JSON for TagStr: {ex.Message}");
            }
          }

          if (instrument.AlternateTickers.Count > 0)
          {
            JsonArray alternateTickers = instrumentJson[tokenJsonAlternateTickers]!.AsArray();
            foreach (string alternateTicker in instrument.AlternateTickers)
              alternateTickers.Add(alternateTicker);
          }

          if (instrument.SecondaryExchangeIds.Count > 0)
          {
            //NOTE: We try to output the names of the secondary exchange so we can import definitions based on names rather than Ids that are used internally.
            JsonArray secondaryExchanges = instrumentJson[tokenJsonSecondaryExchanges]!.AsArray();
            foreach (Guid secondaryExchangeId in instrument.SecondaryExchangeIds)
            {
              exchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == secondaryExchangeId);
              if (exchange != null)
                secondaryExchanges.Add(exchange.Name);
              else
                if (Debugging.ImportExport) m_logger.LogInformation($"Failed to find secondary exchange with Id {secondaryExchangeId} for instrument \"{instrument.Ticker}\".");
            }
          }

          file.Write(instrumentJson.ToJsonString(JsonSerializerOptions.Default));
          exportCount++;
          if (instrumentIndex < instrumentCount - 1) file.WriteLine(",");
        }
        file.WriteLine("");
        file.WriteLine("]");
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Exported {exportCount} instruments to \"{exportSettings.Filename}\".");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Exported {exportCount} instruments to \"{exportSettings.Filename}\".");
    }

    //Method is defined public for testing purposes.
    public void exportCsv(ExportSettings exportSettings)
    {
      long exportCount = 0;
      string statusMessage = $"Importing instruments fto \"{exportSettings.Filename}\"";
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      if (exportSettings.ReplaceBehavior != ExportReplaceBehavior.Replace && File.Exists(exportSettings.Filename))
      {
        //we also need to check the file size, the Windows save dialog would sometimes create an empty file
        FileInfo fileInfo = new FileInfo(exportSettings.Filename);
        if (fileInfo.Length > 0)
        {
          statusMessage = $"File \"{exportSettings.Filename}\" already exists, but export settings are set to not replace it.";
          if (Debugging.ImportExport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", statusMessage);
          return;
        }
      }

      Exchange globalExchange = m_exchangeService.Items.First(x => x.Id == Exchange.InternationalId);    //global exchange must always exist
      IFileSystemService fileSystemService = (IFileSystemService)IApplication.Current.Services.GetService(typeof(IFileSystemService))!;
      using (StreamWriter file = fileSystemService.CreateText(exportSettings.Filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        file.WriteLine($"{tokenCsvType},{tokenCsvTicker},{tokenCsvAlternateTickers},{tokenCsvName},{tokenCsvDescription},{tokenCsvExchange},{tokenCsvInceptionDate1},{tokenCsvPriceDecimals1},{tokenCsvMinimumMovement1},{tokenCsvBigPointValue1},{tokenCsvMarketCap1},{tokenCsvTag},{tokenCsvSecondaryExchanges1},{tokenCsvAttributes}");

        List<Exchange> secondaryExchanges = new List<Exchange>();
        foreach (Instrument instrument in m_instrumentCacheService.Items)
        {
          Exchange? exchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
          if (exchange == null)
          {
            if (Debugging.ImportExport) m_logger.LogInformation($"Failed to find primary exchange with Id {instrument.PrimaryExchangeId} for instrument \"{instrument.Ticker}\", using global exchange for instrument.");
            exchange = globalExchange;
          }

          secondaryExchanges.Clear();
          foreach (var secondaryExchangeId in instrument.SecondaryExchangeIds)
          {
            var secondaryExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == secondaryExchangeId);
            if (secondaryExchange != null)
              secondaryExchanges.Add(secondaryExchange);
            else
              if (Debugging.ImportExport) m_logger.LogInformation($"Failed to find secondary exchange with Id {secondaryExchangeId} for instrument \"{instrument.Ticker}\".");
          }

          string instrumentDefinition = ((int)instrument.Type).ToString();
          instrumentDefinition += ",";
          instrumentDefinition += instrument.Ticker.ToUpper();
          instrumentDefinition += ",";
          instrumentDefinition += TradeSharp.Common.Utilities.ToCsv(instrument.AlternateTickers);
          instrumentDefinition += ",";
          instrumentDefinition += TradeSharp.Common.Utilities.MakeCsvSafe(instrument.Name);
          instrumentDefinition += ",";
          instrumentDefinition += TradeSharp.Common.Utilities.MakeCsvSafe(instrument.Description);
          instrumentDefinition += ",";
          instrumentDefinition += instrument.PrimaryExchangeId.ToString();
          instrumentDefinition += ",";
          instrumentDefinition += instrument.InceptionDate.ToString();
          instrumentDefinition += ",";
          instrumentDefinition += instrument.PriceDecimals;
          instrumentDefinition += ",";
          instrumentDefinition += instrument.MinimumMovement;
          instrumentDefinition += ",";
          instrumentDefinition += instrument.BigPointValue;
          instrumentDefinition += ",";
          instrumentDefinition += instrument is Stock stock ? stock.MarketCap : 0;
          instrumentDefinition += ",";
          instrumentDefinition += TradeSharp.Common.Utilities.MakeCsvSafe(instrument.TagStr);
          instrumentDefinition += ",";
          instrumentDefinition += Common.Utilities.secondaryExchangeNamesCsv(instrument, secondaryExchanges, m_logger);
          instrumentDefinition += ",";
          instrumentDefinition += (int)instrument.AttributeSet;
          file.WriteLine(instrumentDefinition);
          exportCount++;
        }
      }

      if (Debugging.ImportExport) m_logger.LogInformation($"Exported {exportCount} instruments to \"{exportSettings.Filename}\".");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Exported {exportCount} instruments to \"{exportSettings.Filename}\".");
    }
  }
}
