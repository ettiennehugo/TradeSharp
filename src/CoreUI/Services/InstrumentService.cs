using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using Microsoft.Extensions.Logging;
using CsvHelper;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;

namespace TradeSharp.CoreUI.Services
{
  public partial class InstrumentService : ObservableObject, IListItemsService<Instrument>
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
    private const string tokenJsonType = "Type";
    private const string tokenJsonTicker = "Ticker";
    private const string tokenJsonName = "Name";
    private const string tokenJsonDescription = "Description";
    private const string tokenJsonTag = "Tag";
    private const string tokenJsonExchange = "Exchange";
    private const string tokenJsonInceptionDate = "Inception Date";
    private const string tokenJsonAttributes = "Attributes";

    //enums


    //types


    //attributes
    private ILoggerFactory m_loggerFactory;
    private IInstrumentRepository m_instrumentRepository;
    private IDataStoreService m_dataStoreService;
    private IDialogService m_dialogService;
    [ObservableProperty] private Instrument? m_selectedItem;
    [ObservableProperty] private ObservableCollection<Instrument> m_items;

    //constructors
    public InstrumentService(ILoggerFactory loggerFactory, IDataStoreService dataStoreService, IDialogService dialogService, IInstrumentRepository instrumentRepository)
    {
      m_loggerFactory = loggerFactory;
      m_instrumentRepository = instrumentRepository;
      m_dataStoreService = dataStoreService;
      m_dialogService = dialogService;
      m_items = new ObservableCollection<Instrument>();
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
    public event EventHandler<Instrument>? SelectedItemChanged;

    private Task<ImportReplaceResult> importJSON(ImportSettings importSettings)
    {
      throw new NotImplementedException();
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

      using (var reader = new StreamReader(importSettings.Filename))
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

            fileInstruments.Add(new Instrument(Guid.NewGuid(), attributes, tag, type, ticker, name, description, inceptionDate, new List<Guid>(), exchangeId, new List<Guid>()));
            //await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Normal, 0, reader.BaseStream.Length, reader.BaseStream.Position);
          }

          if (fileInstruments.Count > 0)
          {
            result.Severity = IDialogService.StatusMessageSeverity.Success;
            //await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Importing instruments from \"{importSettings.Filename}\"");
            //await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Normal, 0, fileInstruments.Count, 0);
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

    private int exportJSON(string filename)
    {
      throw new NotImplementedException();
    }

    private async Task<long> exportCSV(string filename)
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

      return result;
    }
  }
}
