using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for instrument group objects.
  /// </summary>
  public partial class InstrumentGroupService : ObservableObject, ITreeItemsService<Guid, InstrumentGroup>
  {
    //constants
    private const string extensionCSV = ".csv";
    private const string extensionJSON = ".json";
    private const string tokenCsvName = "name";
    private const string tokenCsvDescription = "description";
    private const string tokenCsvTag = "tag";
    private const string tokenCsvTicker = "ticker";
    private const string tokenCsvAttributes = "attributes";
    private const string tokenJsonId = "Id";
    private const string tokenJsonParentId = "ParentId";
    private const string tokenJsonName = "Name";
    private const string tokenJsonDescription = "Description";
    private const string tokenJsonTag = "Tag";
    private const string tokenJsonAttributes = "Attributes";
    private const string tokenJsonInstruments = "Instruments";
    private const string tokenJsonChildren = "Children";

    //enums


    //types
    private class InstrumentGroupRecord
    {
      public InstrumentGroupRecord(Guid parentId, Guid id, string name, string description, string tag, string ticker, Attributes attributes)
      {
        ParentId = parentId;
        Id = id;
        Name = name;
        Description = description;
        Tag = tag;
        Ticker = ticker;
        Attributes = attributes;
      }

      public Guid ParentId;
      public Guid Id;
      public string Name;
      public string Description;
      public string Tag;
      public string Ticker;
      public Attributes Attributes;
    }

    //attributes
    private IDataStoreService m_dataStoreService;
    private ILoggerFactory m_loggerFactory;
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    private IDialogService m_dialogService;
    [ObservableProperty] private ITreeNodeType<Guid, InstrumentGroup>? m_selectedNode;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_selectedNodes;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_nodes;
    [ObservableProperty] private ObservableCollection<InstrumentGroup> m_items;

    //constructors
    public InstrumentGroupService(ILoggerFactory loggerFactory, IDataStoreService dataStoreService, IDialogService dialogService, IInstrumentGroupRepository instrumentGroupRepository)
    {
      m_loggerFactory = loggerFactory;
      m_dataStoreService = dataStoreService;
      m_instrumentGroupRepository = instrumentGroupRepository;
      m_dialogService = dialogService;
      m_selectedNodes = new ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
      m_items = new ObservableCollection<InstrumentGroup>();
      m_nodes = new ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    public Guid RootNodeId => InstrumentGroup.InstrumentGroupRoot;

    public event EventHandler<InstrumentGroup>? SelectedNodeChanged;

    //methods
    public async Task<ITreeNodeType<Guid, InstrumentGroup>> AddAsync(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      await m_instrumentGroupRepository.AddAsync(node.Item);
      await RefreshAsync(node.Item.ParentId);
      SelectedNode = getNode(node.Item.Id);
      SelectedNodeChanged?.Invoke(this, SelectedNode!.Item);
      return node;
    }

    public async Task<ITreeNodeType<Guid, InstrumentGroup>> CopyAsync(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      InstrumentGroup clone = (InstrumentGroup)node.Item.Clone();
      clone.Id = Guid.NewGuid();
      var result = await m_instrumentGroupRepository.AddAsync(clone);

      ITreeNodeType<Guid, InstrumentGroup>? parentNode = getNode(node.ParentId);
      SelectedNode = null;
      await parentNode!.RefreshAsync();
      SelectedNode = getNode(result);

      SelectedNodeChanged?.Invoke(this, SelectedNode!.Item);

      return SelectedNode!;
    }

    public async Task<bool> DeleteAsync(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      bool result = await m_instrumentGroupRepository.DeleteAsync(node.Item);
      if (node == SelectedNode)
      {
        SelectedNode = null;
        SelectedNodeChanged?.Invoke(this, null);
      }
      return result;
    }

    public async Task RefreshAsync()
    {
      //load all the items
      Items.Clear();
      var result = await m_instrumentGroupRepository.GetItemsAsync();
      foreach (var item in result) Items.Add(item);

      //populate the nodes list of root nodes
      Nodes.Clear();
      foreach (var item in Items)
        if (item.ParentId == InstrumentGroup.InstrumentGroupRoot) Nodes.Add(new InstrumentGroupNodeType(this, item));

      SelectedNode = Nodes.FirstOrDefault(x => x.ParentId == InstrumentGroup.InstrumentGroupRoot); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      if (SelectedNode != null) SelectedNodeChanged?.Invoke(this, SelectedNode.Item);
    }

    private void removeNodes(Guid parentId)
    {
      var nodesToRemove = Nodes.Where(x => x.ParentId == parentId).ToList();
      foreach (var node in nodesToRemove)
      {
        removeNodes(node.Id);
        Nodes.Remove(node);
      }
    }

    public async Task RefreshAsync(Guid parentId)
    {
      Items.Clear();
      var result = await m_instrumentGroupRepository.GetItemsAsync();
      foreach (var item in result) Items.Add(item);
      removeNodes(parentId);
      var parentNode = getNode(parentId);
      if (parentNode != null) await parentNode.RefreshAsync();
      SelectedNode = parentNode;
    }

    public async Task RefreshAsync(ITreeNodeType<Guid, InstrumentGroup> parentNode)
    {
      Items.Clear();
      var result = await m_instrumentGroupRepository.GetItemsAsync();
      foreach (var item in result) Items.Add(item);
      await parentNode.RefreshAsync();
    }

    public Task<ITreeNodeType<Guid, InstrumentGroup>> UpdateAsync(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      m_instrumentGroupRepository.UpdateAsync(node.Item);
      return Task.FromResult(node);
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

    public async Task<int> ExportAsync(string filename)
    {
      int result = 0;

      IDictionary<Guid, Tuple<InstrumentGroup, bool>> leafNodes = await getLeafInstrumentGroups();
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        result = exportCSV(filename, leafNodes);
      else if (extension == extensionJSON)
        result = exportJSON(filename, leafNodes);

      return result;
    }

    private ITreeNodeType<Guid, InstrumentGroup>? getNode(Guid instrumentGroupId)
    {
      foreach (ITreeNodeType<Guid, InstrumentGroup> instrumentGroupNode in Nodes)
        if (instrumentGroupNode.Item.Id == instrumentGroupId) return instrumentGroupNode;

      return null;
    }

    private ITreeNodeType<Guid, InstrumentGroup>? getNode(InstrumentGroup instrumentGroup)
    {
      foreach (ITreeNodeType<Guid, InstrumentGroup> instrumentGroupNode in Nodes)
        if (instrumentGroupNode.Item == instrumentGroup) return instrumentGroupNode;

      return null;
    }

    /// <summary>
    /// Imports the instrument definitions from the given file, format must be one of the following:
    ///     Header line - must follow the pattern "Name, Description[, Tag], Name, Description[, Tag], etc. [, Ticker] " - field names in header is used to assign the field values for the instruments
    ///       Group 1 Name, Group 1 Description, Group 1 Tag, Group 1-1 Name, Group 1-1 Description, Group 1-1 Tag, Group 1-1-1 Name, Group 1-1-1, Group 1-1-1 Tag, AAPL
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    ///   - Only instrument group names are imported and is used for the description as well.
    /// </summary>
    private async Task<ImportReplaceResult> importCSV(ImportSettings importSettings)
    {
      ImportReplaceResult result = new ImportReplaceResult();
      ILogger logger = m_loggerFactory.CreateLogger($"Importing instrument groups - \"{importSettings.Filename}\"");

      Dictionary<string, InstrumentGroup> definedInstrumentGroups = new Dictionary<string, InstrumentGroup>();
      Dictionary<string, InstrumentGroupRecord> fileInstrumentGroups = new Dictionary<string, InstrumentGroupRecord>();
      List<InstrumentGroupRecord> currentLineRecords = new List<InstrumentGroupRecord>();

      using (var reader = new StreamReader(importSettings.Filename))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        //construct the set of instrument groups to be imported
        if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
        {
          int lineNo = 0;
          bool parseError = false;

          while (csv.Read() && !parseError)
          {
            string name = "";
            string description = "";
            string tag = "";
            string ticker = "";
            Attributes attributes = InstrumentGroup.DefaultAttributeSet; 

            lineNo++;

            for (int columnIndex = csv.HeaderRecord.Count() - 1; columnIndex >= 0; columnIndex--)
            {
              string? columnValue = null;
              if (csv.TryGetField(columnIndex, out columnValue))
              {
                if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvName)
                {
                  name = columnValue!;
                  if (description == "") description = name;
                  if (tag == "") tag = name;

                  //create new group to be added for this line
                  currentLineRecords.Add(new InstrumentGroupRecord(SelectedNode != null ? SelectedNode.Id : InstrumentGroup.InstrumentGroupRoot, Guid.NewGuid(), name, description, tag, ticker, attributes));

                  //reset field values
                  name = "";
                  description = "";
                  tag = "";
                  ticker = "";
                }
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvDescription)
                  description = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTag)
                  tag = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvTicker)
                  ticker = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenCsvAttributes)
                  attributes = (Attributes)Enum.Parse(typeof(Attributes), columnValue!);

                //resolve the parent id's and add the objects as required - we use the tag as the key since it should be a unique key value used for the instrument groups
                if (columnIndex == 0)
                {
                  Guid previousId = SelectedNode != null ? SelectedNode.Id : InstrumentGroup.InstrumentGroupRoot;
                  currentLineRecords.Reverse();
                  foreach (InstrumentGroupRecord instrumentGroupRecord in currentLineRecords)
                  {
                    //keep existing id and parent if record already exists
                    if (fileInstrumentGroups.TryGetValue(instrumentGroupRecord.Tag, out InstrumentGroupRecord? existingRecord))
                    {
                      instrumentGroupRecord.ParentId = existingRecord.ParentId;
                      instrumentGroupRecord.Id = existingRecord.Id;
                    }
                    else
                    {
                      instrumentGroupRecord.ParentId = previousId;
                      fileInstrumentGroups.Add(instrumentGroupRecord.Tag, instrumentGroupRecord);
                    }

                    previousId = instrumentGroupRecord.Id;
                  }

                  currentLineRecords.Clear();
                }
              }
            }
          }

          if (!parseError)
          {
            //assume success at this point
            result.Severity = IDialogService.StatusMessageSeverity.Success;

            //get the set of instrument groups already defined and update the parentId and Id keys for groups loaded from the file
            IEnumerable<InstrumentGroup> persistedInstrumentGroups = await m_instrumentGroupRepository.GetItemsAsync();
            foreach (InstrumentGroup instrumentGroup in persistedInstrumentGroups)
            {
              string tag = instrumentGroup.Tag.Length != 0 ? instrumentGroup.Tag : instrumentGroup.Name;
              definedInstrumentGroups.Add(tag, instrumentGroup);

              //update any instrument groups defined in the file with the existing parentId and Id keys defined for the instrument group
              if (fileInstrumentGroups.TryGetValue(tag, out InstrumentGroupRecord? instrumentGroupRecord))
              {
                //update any children to use the persisted instrument group Id and parent Id
                foreach (KeyValuePair<string, InstrumentGroupRecord> child in fileInstrumentGroups)
                  if (child.Value.ParentId == instrumentGroupRecord.Id) child.Value.ParentId = instrumentGroup.Id;

                //update the instrument group as well
                instrumentGroupRecord.ParentId = instrumentGroup.ParentId;
                instrumentGroupRecord.Id = instrumentGroup.Id;
              }
            }

            //update/create instrument groups in the repository
            foreach (KeyValuePair<string, InstrumentGroupRecord> fileInstrumentGroup in fileInstrumentGroups)
            {
              if (definedInstrumentGroups.TryGetValue(fileInstrumentGroup.Key, out InstrumentGroup? definedInstrumentGroup))
              {
                switch (importSettings.ImportReplaceBehavior)
                {
                  case ImportReplaceBehavior.Skip:
                    logger.LogWarning($"Skipping - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag}");
                    result.Severity = IDialogService.StatusMessageSeverity.Warning;   //warn user of skipped items
                    result.Skipped++;
                    break;
                  case ImportReplaceBehavior.Replace:
                    //will update the name, description and tag and remove instrument associations
                    logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                    await m_instrumentGroupRepository.UpdateAsync(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, new List<Guid>()));
                    result.Replaced++;
                    break;
                  case ImportReplaceBehavior.Update:
                    //will update the name and description and keep all the associated instruments                  
                    logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                    await m_instrumentGroupRepository.UpdateAsync(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, definedInstrumentGroup.Instruments));
                    result.Updated++;
                    break;
                }
              }
              else if (fileInstrumentGroup.Value.Ticker.Length == 0)
              {
                logger.LogInformation($"Creating - {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                await m_instrumentGroupRepository.AddAsync(new InstrumentGroup(fileInstrumentGroup.Value.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, fileInstrumentGroup.Value.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, new List<Guid>()));
                result.Created++;
              }
            }

            //create/update any instrument associations based on tickers given
            IList<Instrument> instruments = m_dataStoreService.GetInstruments();
            foreach (KeyValuePair<string, InstrumentGroupRecord> fileInstrumentGroup in fileInstrumentGroups)
              if (fileInstrumentGroup.Value.Ticker.Length != 0)
              {
                Instrument? instrument = instruments.FirstOrDefault(x => x.Ticker.ToLower() == fileInstrumentGroup.Value.Ticker.ToLower());
                if (instrument != null)
                  m_dataStoreService.CreateInstrumentGroupInstrument(fileInstrumentGroup.Value.Id, instrument.Id);
                else
                  logger.LogError($"Instrument with ticker \"{fileInstrumentGroup.Value.Ticker}\" not defined, association with instrument group \"{fileInstrumentGroup.Value.Name}\" not created - define instruments first before importing instrument groups.");
              }
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Returns the set of defined leaf instrument groups, that would be groups that have one or more instruments or groups that have no children.
    /// </summary>
    private async Task<IDictionary<Guid, Tuple<InstrumentGroup, bool>>> getLeafInstrumentGroups()
    {
      SortedDictionary<Guid, Tuple<InstrumentGroup, bool>> result = new SortedDictionary<Guid, Tuple<InstrumentGroup, bool>>();
      IEnumerable<InstrumentGroup> instrumentGroups = await m_instrumentGroupRepository.GetItemsAsync();

      InstrumentGroup? child = null;
      foreach (InstrumentGroup instrumentGroup in instrumentGroups)
      {
        child = null;
        if (instrumentGroup.Instruments.Count > 0)
          result.Add(instrumentGroup.Id, new Tuple<InstrumentGroup, bool>(instrumentGroup, true));
        else
        {
          foreach (InstrumentGroup otherInstrumentGroup in instrumentGroups)
            if (otherInstrumentGroup.ParentId == instrumentGroup.Id)
            {
              child = otherInstrumentGroup;
              break;
            }
        }

        if (child == null)
          result.Add(instrumentGroup.Id, new Tuple<InstrumentGroup, bool>(instrumentGroup, true));    //add leaf instrument group
        else
          result.Add(instrumentGroup.Id, new Tuple<InstrumentGroup, bool>(instrumentGroup, false));   //add non-leaf parent instrument group
      }

      return result;
    }

    /// <summary>
    /// Exports the instrument group data into the given file, each row would include a single instrument group definition with all it's associated
    /// parents and then followed by and instrument tickers associated with it. Example CSV format would be:
    ///     Parent Name, Parent Description, Parent Tag, Child Name, Child Description, Child Tag, Child Name, Child Description, Child Tag
    ///     Parent Name, Parent Description, Parent Tag, Child Name, Child Description, Child Tag, Child Name, Child Description, Child Tag, Ticker1
    ///     Parent Name, Parent Description, Parent Tag, Child Name, Child Description, Child Tag, Child Name, Child Description, Child Tag, Ticker2
    ///  
    ///                                                            ...................
    /// 
    ///     Parent Name, Parent Description, Parent Tag, Child Name, Child Description, Child Tag, Child Name, Child Description, Child Tag, TickerN
    /// </summary>
    private int exportCSV(string filename, IDictionary<Guid, Tuple<InstrumentGroup, bool>> instrumentGroups)
    {
      int result = 0;

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        ILogger logger = m_loggerFactory.CreateLogger($"Exporting instrument groups to \"{filename}\"");
        IList<Instrument> instrumentsList = m_dataStoreService.GetInstruments();

        SortedDictionary<Guid, Instrument> instruments = new SortedDictionary<Guid, Instrument>();
        foreach (Instrument instrument in instrumentsList) instruments.Add(instrument.Id, instrument);

        //compute the number of instrument groups to be added to each line in the file
        int longestBranch = 0;
        bool includeTicker = false;
        Guid exportRootId = SelectedNode != null ? SelectedNode.Id : InstrumentGroup.InstrumentGroupRoot;
        List<List<InstrumentGroup>> instrumentGroupsPerLine = new List<List<InstrumentGroup>>();
        foreach (KeyValuePair<Guid, Tuple<InstrumentGroup, bool>> instrumentGroupTuple in instrumentGroups)
          if (instrumentGroupTuple.Value.Item2)  //only process leaf nodes
          {
            List<InstrumentGroup> instrumentGroupPerLine = new List<InstrumentGroup>();
            InstrumentGroup? instrumentGroup = instrumentGroupTuple.Value.Item1;
            instrumentGroupPerLine.Add(instrumentGroup);

            while (instrumentGroup != null && instrumentGroup.ParentId != InstrumentGroup.InstrumentGroupRoot)
            {
              if (instrumentGroups.TryGetValue(instrumentGroup.ParentId, out Tuple<InstrumentGroup, bool>? parentInstrumentGroup))
              {
                instrumentGroup = parentInstrumentGroup.Item1;
                instrumentGroupPerLine.Add(instrumentGroup);
              }
              else
                instrumentGroup = null;
            }

            instrumentGroupPerLine.Reverse();

            //if we're adding only lines associated with a specific parent node check that the line contains that specific parent node otherwise
            //we can just add it if it's associated with the root node
            if (exportRootId == InstrumentGroup.InstrumentGroupRoot)
            {
              if (instrumentGroupPerLine.Count > longestBranch) longestBranch = instrumentGroupPerLine.Count;
              instrumentGroupsPerLine.Add(instrumentGroupPerLine);
            }
            else
            {
              bool includeLine = false;
              int branchNodeCount = 0;
              for (int i = instrumentGroupPerLine.Count - 1; i >= 0; i--)
              {
                InstrumentGroup instrumentGroupInLine = instrumentGroupPerLine[i];
                branchNodeCount++;
                if (instrumentGroupInLine.Id == exportRootId)
                {
                  includeLine = true;
                  break;
                }
              }

              if (includeLine)
              {
                if (branchNodeCount > longestBranch) longestBranch = branchNodeCount;
                instrumentGroupsPerLine.Add(instrumentGroupPerLine);
              }
            }
          }

        //construct and output the header line
        //NOTE: When tickers are included this algorithm will not work if instruments are included in non-leaf node instrument groups. This is typically not the case for instrument group classifications like GDIC by MSCI.
        //      If you want to use instruments included in the non-leaf nodes rather use JSON as the CSV output will not output the tickers.
        SortedSet<string> instrumentGroupsOutput = new SortedSet<string>();
        foreach (IList<InstrumentGroup> instrumentGroupPerLine in instrumentGroupsPerLine)
        {
          foreach (InstrumentGroup instrumentGroup in instrumentGroupPerLine) instrumentGroupsOutput.Add(instrumentGroup.Tag);    //build set of the instrument groups that are going to be output

          InstrumentGroup lastInstrumentGroup = instrumentGroupPerLine[instrumentGroupPerLine.Count - 1];

          if (exportRootId == InstrumentGroup.InstrumentGroupRoot)
          {
            if (lastInstrumentGroup.Instruments.Count > 0)
            {
              includeTicker = true;
              if (instrumentGroupPerLine.Count < longestBranch)
              {
                logger.LogError($"Ticker output are suppressed for CSV output since instrument group \"{lastInstrumentGroup.Name}, {lastInstrumentGroup.Tag}\" define instruments in a non-leaf node - rather use JSON output for this kind of configuration.");
                includeTicker = false;
                break;
              }
            }
          }
          else
          {
            int branchNodeCount = 0;
            for (int i = instrumentGroupPerLine.Count - 1; i >= 0; i--) //count instrument groups to be output in reverse order from the leaf node to 
            {
              InstrumentGroup instrumentGroupInLine = instrumentGroupPerLine[i];
              branchNodeCount++;
              if (instrumentGroupInLine.Id == exportRootId) break;
            }

            if (lastInstrumentGroup.Instruments.Count > 0)
            {
              includeTicker = true;
              if (branchNodeCount < longestBranch)
              {
                logger.LogError($"Ticker output are suppressed for CSV output since instrument group \"{lastInstrumentGroup.Name}, {lastInstrumentGroup.Tag}\" define instruments in a non-leaf node - rather use JSON output for this kind of configuration.");
                includeTicker = false;
                break;
              }
            }
          }
        }

        result = instrumentGroupsOutput.Count;

        string headerLine = "";
        for (int i = 0; i < longestBranch; i++)
        {
          if (headerLine.Length > 0) headerLine += ", ";
          headerLine += $"{tokenCsvName}, {tokenCsvDescription}, {tokenCsvTag}, {tokenCsvAttributes}";
        }

        if (includeTicker) headerLine += $", {tokenCsvTicker}";
        file.WriteLine(headerLine);

        //output the determined set of instrument groups and their associated instruments
        foreach (IList<InstrumentGroup> instrumentGroupPerLine in instrumentGroupsPerLine)
        {
          string instrumentGroupDefinitions = "";
          bool outputInstrumentGroups = false;  //need to keep track whether we need to output instrument groups if we're outputting up to a non-root node
          foreach (InstrumentGroup instrumentGroup in instrumentGroupPerLine)
          {
            if (instrumentGroup.Id == exportRootId) outputInstrumentGroups = true;

            if (outputInstrumentGroups)
            {
              if (instrumentGroupDefinitions.Length > 0) instrumentGroupDefinitions += ", ";
              instrumentGroupDefinitions += instrumentGroup.Name;
              instrumentGroupDefinitions += ", ";
              instrumentGroupDefinitions += instrumentGroup.Description;
              instrumentGroupDefinitions += ", ";
              instrumentGroupDefinitions += instrumentGroup.Tag;
              instrumentGroupDefinitions += ", ";
              int attributeSet = (int)instrumentGroup.AttributeSet;
              instrumentGroupDefinitions += attributeSet.ToString();
            }
          }

          file.WriteLine(instrumentGroupDefinitions);

          if (includeTicker)
          {
            InstrumentGroup lastInstrumentGroup = instrumentGroupPerLine[instrumentGroupPerLine.Count - 1];
            foreach (Guid instrumentId in lastInstrumentGroup.Instruments)
              if (instruments.TryGetValue(instrumentId, out Instrument? instrument))
              {
                file.WriteLine("{0}, {1}", instrumentGroupDefinitions, instrument.Ticker);
              }
              else
                logger.LogError($"Failed to find instrument \"{instrumentId.ToString()}\" associated with instrument group \"{lastInstrumentGroup.Name}, {lastInstrumentGroup.Tag}\".");
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Imports the instrument group definitions from a given file, format must be the following:
    ///   {
    ///     "name" : "Instrument Group Name",
    ///     "description" : "Instrument Group Description",
    ///     "tag" : "Instrument Group Tag Value",
    ///     "attributes" : 5, //see Attributes enum which is a bit encoded enum 
    ///     "children" : [
    ///       
    ///       /// records similar in format with name, description, tag, children (tag is optional)
    /// 
    ///     ],
    ///     "instruments" : [ ticker1, ... , tickerN ]
    ///   }
    /// </summary>
    private async Task<ImportReplaceResult> importJSON(ImportSettings importSettings)
    {
      ImportReplaceResult result = new ImportReplaceResult();

      using (StreamReader file = new StreamReader(importSettings.Filename))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd());
        if (documentNode != null)
        {
          ILogger logger = m_loggerFactory.CreateLogger($"Importing \"{importSettings.Filename}\"");
          Dictionary<Guid, InstrumentGroup> definedInstrumentGroups = new Dictionary<Guid, InstrumentGroup>();
          IEnumerable<InstrumentGroup> instrumentGroups = await m_instrumentGroupRepository.GetItemsAsync();
          foreach (InstrumentGroup instrumentGroup in instrumentGroups) definedInstrumentGroups.Add(instrumentGroup.Id, instrumentGroup);

          Dictionary<string, Instrument> definedInstruments = new Dictionary<string, Instrument>();
          IList<Instrument> instruments = m_dataStoreService.GetInstruments();
          foreach (Instrument instrument in instruments) definedInstruments.Add(instrument.Ticker, instrument);

          JsonArray rootNodes = documentNode.AsArray();
          result.Severity = IDialogService.StatusMessageSeverity.Success;
          foreach (JsonObject? node in rootNodes) if (node != null) result = await importJsonNode(node!, importSettings.ImportReplaceBehavior, definedInstrumentGroups, definedInstruments, logger, result);
        }
      }

      return result;
    }

    private async Task<ImportReplaceResult> importJsonNode(JsonObject node, ImportReplaceBehavior importReplaceBehavior, Dictionary<Guid, InstrumentGroup> definedInstrumentGroups, Dictionary<string, Instrument> definedInstruments, ILogger logger, ImportReplaceResult result)
    {
      Guid id = (Guid)(node[tokenJsonId]!.AsValue().Deserialize(typeof(Guid)))!;
      Guid parentId = (Guid)(node[tokenJsonParentId]!.AsValue().Deserialize(typeof(Guid)))!;
      Guid importParentId = SelectedNode != null ? SelectedNode.Id : InstrumentGroup.InstrumentGroupRoot; //determine which node would be the root node for all the imported nodes, the root nodes in the import data should be associated with the root node Id used for instrument groups
      parentId = parentId == InstrumentGroup.InstrumentGroupRoot ? importParentId : parentId;   //adjust the root parents to the selected node Id if required
      string name = (string)(node[tokenJsonName]!.AsValue().Deserialize(typeof(string)))!;
      string description = (string?)(node[tokenJsonDescription]!.AsValue().Deserialize(typeof(string))) ?? name;
      string tag = (string?)(node[tokenJsonTag]!.AsValue().Deserialize(typeof(string))) ?? name;
      string? attributesStr = (string?)(node[tokenJsonAttributes]!.AsValue().Deserialize(typeof(string)));
      Attributes attributes = attributesStr != null ? (Attributes)Enum.Parse(typeof(Attributes), attributesStr!) : InstrumentGroup.DefaultAttributeSet;

      JsonArray? instruments = node.ContainsKey(tokenJsonInstruments) ? node[tokenJsonInstruments]!.AsArray() : null;
      List<Guid> instrumentList = new List<Guid>();
      if (instruments != null)
        foreach (JsonObject? instrumentNode in instruments!)
          if (instrumentNode != null)
          {
            string instrumentTicker = (string)instrumentNode.AsValue().Deserialize(typeof(string))!;
            if (definedInstruments.TryGetValue(instrumentTicker, out Instrument? value))
              instrumentList.Add(value.Id);
            else
              logger.LogError($"Failed to find instrument \"{instrumentTicker}\" for instrument group \"{name}, {tag}\"");
          }

      if (definedInstrumentGroups.TryGetValue(id, out InstrumentGroup? definedInstrumentGroup))
      {
        switch (importReplaceBehavior)
        {
          case ImportReplaceBehavior.Skip:
            logger.LogWarning($"Skipping - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag}");
            result.Severity = IDialogService.StatusMessageSeverity.Warning;   //warn user of skipped items
            result.Skipped++;
            break;
          case ImportReplaceBehavior.Replace:
            //replacing name, description, tag and all defined instruments
            logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {name}, {description}, {tag}");
            await m_instrumentGroupRepository.UpdateAsync(new InstrumentGroup(definedInstrumentGroup.Id, attributes, tag, parentId, name, description, instrumentList));
            result.Replaced++;
            break;
          case ImportReplaceBehavior.Update:
            //updating name, description, tag and merge in defined instruments
            logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {name}, {description}, {tag}");

            foreach (Guid instrumentId in definedInstrumentGroup.Instruments)
              if (!instrumentList.Contains(instrumentId)) instrumentList.Add(instrumentId);

            await m_instrumentGroupRepository.UpdateAsync(new InstrumentGroup(definedInstrumentGroup.Id, attributes, tag, parentId, name, description, instrumentList));
            result.Updated++;
            break;
        }
      }
      else
      {
        await m_instrumentGroupRepository.AddAsync(new InstrumentGroup(id, attributes, tag, parentId, name, description, instrumentList));
        result.Created++;
      }

      JsonArray? children = node.ContainsKey(tokenJsonChildren) ? node[tokenJsonChildren]!.AsArray() : null;
      if (children != null)
        foreach (JsonObject? childNode in children!)
          if (childNode != null)
            result = await importJsonNode(childNode, importReplaceBehavior, definedInstrumentGroups, definedInstruments, logger, result);

      return result;
    }

    private int exportJSON(string filename, IDictionary<Guid, Tuple<InstrumentGroup, bool>> instrumentGroups)
    {
      int result = 0;

      //determine the root node(s) based on whether we have a selected node or not
      //NOTE: For exporting the nodes we adjust the parent Id's to the root node Id which again would be adjusted to the import to the selected node Id or it would be kept the root node Id if
      //      there is no selected Id.
      List<JsonObject> rootNodes = new List<JsonObject>();
      if (SelectedNode != null)
      {
        //when we have a selected node we export from that node on with it's children
        foreach (KeyValuePair<Guid, Tuple<InstrumentGroup, bool>> instrumentGroupTuple in instrumentGroups)
          if (instrumentGroupTuple.Value.Item1.Id == SelectedNode.Id)
          {
            rootNodes.Add(writeJsonNode(InstrumentGroup.InstrumentGroupRoot, instrumentGroupTuple.Value.Item1, instrumentGroups, ref result));
            break;
          }
      }
      else
      {
        //with no selected node we export from all nodes that are root node
        foreach (KeyValuePair<Guid, Tuple<InstrumentGroup, bool>> instrumentGroupTuple in instrumentGroups)
          if (instrumentGroupTuple.Value.Item1.ParentId == InstrumentGroup.InstrumentGroupRoot) rootNodes.Add(writeJsonNode(InstrumentGroup.InstrumentGroupRoot, instrumentGroupTuple.Value.Item1, instrumentGroups, ref result));
      }

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        int rootNodeCount = rootNodes.Count;
        int rootNodeIndex = 0;
        file.WriteLine("[");
        foreach (JsonObject rootNode in rootNodes)
        {
          rootNodeIndex++;
          file.Write(rootNode.ToJsonString(options));
          if (rootNodeIndex < rootNodeCount) file.WriteLine(",");
        }
        file.WriteLine("");
        file.WriteLine("]");
      }

      return result;
    }

    private JsonObject writeJsonNode(Guid adjustedParentNodeId, InstrumentGroup instrumentGroup, IDictionary<Guid, Tuple<InstrumentGroup, bool>> instrumentGroups, ref int resultCount)
    {
      JsonObject node = new JsonObject
      {
        [tokenJsonId] = instrumentGroup.Id,
        [tokenJsonParentId] = adjustedParentNodeId,
        [tokenJsonName] = instrumentGroup.Name,
        [tokenJsonDescription] = instrumentGroup.Description,
        [tokenJsonTag] = instrumentGroup.Tag,
        [tokenJsonAttributes] = ((int)instrumentGroup.AttributeSet).ToString(),   //need to first cast to an integer otherwise it renders the tokens/words of the attribute set
        [tokenJsonInstruments] = new JsonArray(),
        [tokenJsonChildren] = new JsonArray()
      };

      JsonArray children = node[tokenJsonChildren]!.AsArray();
      foreach (KeyValuePair<Guid, Tuple<InstrumentGroup, bool>> instrumentGroupTuple in instrumentGroups)
        //NOTE: For the child nodes we keep the "adjusted parent Id" normal for any layers of nodes beyond the selected parent node.
        if (instrumentGroupTuple.Value.Item1.ParentId == instrumentGroup.Id)
          children.Add(writeJsonNode(instrumentGroupTuple.Value.Item1.ParentId, instrumentGroupTuple.Value.Item1, instrumentGroups, ref resultCount));

      if (instrumentGroup.Instruments.Count > 0)
      {
        JsonArray instruments = node[tokenJsonInstruments]!.AsArray();
        foreach (Guid instrumentId in instrumentGroup.Instruments) instruments.Add(instrumentId.ToString());
      }

      resultCount++;

      return node;
    }
  }
}
