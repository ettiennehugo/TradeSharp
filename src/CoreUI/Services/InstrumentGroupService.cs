using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for instrument group objects.
  /// </summary>
  public partial class InstrumentGroupService : ServiceBase, IInstrumentGroupService
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
    private IDatabase m_database;
    private ILoggerFactory m_loggerFactory;
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    private ITreeNodeType<Guid, InstrumentGroup>? m_selectedNode;
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> SelectedNodes { get; set; }
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> Nodes { get; internal set; }
    public ObservableCollection<InstrumentGroup> Items { get; internal set; }

    //constructors
    public InstrumentGroupService(ILoggerFactory loggerFactory, IDatabase database, IInstrumentGroupRepository instrumentGroupRepository)
    {
      m_loggerFactory = loggerFactory;
      m_database = database;
      m_instrumentGroupRepository = instrumentGroupRepository;
      m_selectedNode = null;
      SelectedNodes = new ThreadSafeObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
      Items = new ThreadSafeObservableCollection<InstrumentGroup>();
      Nodes = new ThreadSafeObservableCollection<ITreeNodeType<Guid, InstrumentGroup>>();
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    public Guid RootNodeId => InstrumentGroup.InstrumentGroupRoot;

    public event EventHandler<ITreeNodeType<Guid, InstrumentGroup>?>? SelectedNodeChanged;
    public ITreeNodeType<Guid, InstrumentGroup>? SelectedNode
    {
      get => m_selectedNode;
      set { SetProperty(ref m_selectedNode, value); SelectedNodeChanged?.Invoke(this, m_selectedNode); }
    }

    //methods
    public bool Add(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      var result = m_instrumentGroupRepository.Add(node.Item);
      Refresh(node.Item.ParentId);
      SelectedNode = getNode(node.Item.Id, Nodes);
      SelectedNodeChanged?.Invoke(this, SelectedNode);
      return result;
    }

    public bool Copy(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      InstrumentGroup clone = (InstrumentGroup)node.Item.Clone();
      clone.Id = Guid.NewGuid();
      var result = m_instrumentGroupRepository.Add(clone);

      ITreeNodeType<Guid, InstrumentGroup>? parentNode = getNode(node.ParentId, Nodes);
      SelectedNode = null;
      parentNode!.Refresh();
      SelectedNode = getNode(clone, Nodes);
      SelectedNodeChanged?.Invoke(this, SelectedNode);

      return result;
    }

    public bool Delete(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      bool result = m_instrumentGroupRepository.Delete(node.Item);

      if (node == SelectedNode)
      {
        SelectedNode = null;
        SelectedNodeChanged?.Invoke(this, null);
      }

      return result;
    }

    public void Refresh()
    {
      //need to clear nodes/items in reverse order to TRY and avoid memory corruption
      Nodes.Clear();
      Items.Clear();

      //load all the items
      var result = m_instrumentGroupRepository.GetItems();
      foreach (var item in result) Items.Add(item);

      //populate the nodes list of root nodes
      //TODO: There is a bug in this code where the Add would result in an ExceptionAccessViolation, it's some threading issue since it does not occur consistenly.
      foreach (var item in Items)
        if (item.ParentId == InstrumentGroup.InstrumentGroupRoot)
          Nodes.Add(new InstrumentGroupNodeType(this, item));

      SelectedNode = Nodes.FirstOrDefault(x => x.ParentId == InstrumentGroup.InstrumentGroupRoot); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      if (SelectedNode != null) SelectedNodeChanged?.Invoke(this, SelectedNode);
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

    public void Refresh(Guid parentId)
    {
      Items.Clear();
      var result = m_instrumentGroupRepository.GetItems();
      foreach (var item in result) Items.Add(item);
      var parentNode = getNode(parentId, Nodes);
      if (parentNode != null) parentNode.Refresh();
      SelectedNode = parentNode;
    }

    public void Refresh(ITreeNodeType<Guid, InstrumentGroup> parentNode)
    {
      Items.Clear();
      var result = m_instrumentGroupRepository.GetItems();
      foreach (var item in result) Items.Add(item);
      parentNode.Refresh();
    }

    public bool Update(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      return m_instrumentGroupRepository.Update(node.Item);
    }

    public ImportResult Import(ImportSettings importSettings)
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
      return result;
    }

    public ExportResult Export(string filename)
    {
      IDictionary<Guid, Tuple<InstrumentGroup, bool>> leafNodes = getLeafInstrumentGroups();
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        return exportCSV(filename, leafNodes);
      else if (extension == extensionJSON)
        return exportJSON(filename, leafNodes);

      //should never be reached if all supported file types are handled
      ExportResult result = new();
      StackFrame? stackFrame = new StackTrace(true).GetFrame(0);
      result.Severity = IDialogService.StatusMessageSeverity.Error;
      result.StatusMessage = $"Invalid program state: Unable to handle file type \"{filename}\" for export. ({stackFrame!.GetFileName()}:{stackFrame!.GetFileLineNumber()})";
      return result;
    }

    /// <summary>
    /// Search through the given set of nodes/sub-tree for the node with the given instrument group Id.
    /// </summary>
    private ITreeNodeType<Guid, InstrumentGroup>? getNode(Guid instrumentGroupId, IEnumerable<ITreeNodeType<Guid, InstrumentGroup>> nodes)
    {
      //search current level of nodes
      foreach (ITreeNodeType<Guid, InstrumentGroup> instrumentGroupNode in nodes)
        if (instrumentGroupNode.Item.Id == instrumentGroupId) return instrumentGroupNode;

      //search children of current level
      foreach (ITreeNodeType<Guid, InstrumentGroup> instrumentGroupNode in nodes)
      {
        ITreeNodeType<Guid, InstrumentGroup>? node = getNode(instrumentGroupId, instrumentGroupNode.Children);
        if (node != null) return node;
      }

      return null;  //given instrument in not on this sub-tree of nodes
    }

    /// <summary>
    /// Search through the given set of nodes/sub-tree for the given node.
    /// </summary>
    private ITreeNodeType<Guid, InstrumentGroup>? getNode(InstrumentGroup instrumentGroup, IEnumerable<ITreeNodeType<Guid, InstrumentGroup>> nodes)
    {
      foreach (ITreeNodeType<Guid, InstrumentGroup> instrumentGroupNode in nodes)
        if (instrumentGroupNode.Item == instrumentGroup) return instrumentGroupNode;

      //search children of current level
      foreach (ITreeNodeType<Guid, InstrumentGroup> instrumentGroupNode in nodes)
      {
        ITreeNodeType<Guid, InstrumentGroup>? node = getNode(instrumentGroup, instrumentGroupNode.Children);
        if (node != null) return node;
      }

      return null;
    }

    /// <summary>
    /// Imports the instrument definitions from the given file, format must be one of the following:
    ///     Header line - must follow the pattern "Name, Description[, Tag], Name, Description[, Tag], etc. [, Ticker] " - field names in header is used to assign the field values for the instruments
    ///       Group 1 Name, Group 1 Description, Group 1 Tag, Group 1-1 Name, Group 1-1 Description, Group 1-1 Tag, Group 1-1-1 Name, Group 1-1-1, Group 1-1-1 Tag, AAPL
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    ///   - Only instrument group names are imported and is used for the description as well.
    /// </summary>
    private ImportResult importCSV(ImportSettings importSettings)
    {
      ImportResult result = new();
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;
      ILogger logger = m_loggerFactory.CreateLogger($"Importing instrument groups - \"{importSettings.Filename}\"");

      Dictionary<string, InstrumentGroup> definedInstrumentGroups = new Dictionary<string, InstrumentGroup>();
      Dictionary<string, InstrumentGroupRecord> fileInstrumentGroups = new Dictionary<string, InstrumentGroupRecord>();
      List<InstrumentGroupRecord> currentLineRecords = new List<InstrumentGroupRecord>();

      using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        //construct the set of instrument groups to be imported
        if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
        {
          int lineNo = 1; //header row is on line 0
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
            IList<InstrumentGroup> persistedInstrumentGroups = m_instrumentGroupRepository.GetItems();
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
                switch (importSettings.ReplaceBehavior)
                {
                  case ImportReplaceBehavior.Skip:
                    logger.LogWarning($"Skipping - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag}");
                    result.Severity = IDialogService.StatusMessageSeverity.Warning;   //warn user of skipped items
                    skippedCount++;
                    break;
                  case ImportReplaceBehavior.Replace:
                    //will update the name, description and tag and remove instrument associations
                    logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                    m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, new List<Guid>()));
                    replacedCount++;
                    break;
                  case ImportReplaceBehavior.Update:
                    //will update the name and description and keep all the associated instruments                  
                    logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                    m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, definedInstrumentGroup.Instruments));
                    updatedCount++;
                    break;
                }
              }
              else if (fileInstrumentGroup.Value.Ticker.Length == 0)
              {
                logger.LogInformation($"Creating - {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                m_instrumentGroupRepository.Add(new InstrumentGroup(fileInstrumentGroup.Value.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, fileInstrumentGroup.Value.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, new List<Guid>()));
                createdCount++;
              }
            }

            //create/update any instrument associations based on tickers given
            IList<Instrument> instruments = m_database.GetInstruments();
            foreach (KeyValuePair<string, InstrumentGroupRecord> fileInstrumentGroup in fileInstrumentGroups)
              if (fileInstrumentGroup.Value.Ticker.Length != 0)
              {
                Instrument? instrument = instruments.FirstOrDefault(x => x.Ticker.ToLower() == fileInstrumentGroup.Value.Ticker.ToLower());
                if (instrument != null)
                  m_database.CreateInstrumentGroupInstrument(fileInstrumentGroup.Value.Id, instrument.Id);
                else
                {
                  result.Severity = IDialogService.StatusMessageSeverity.Error;
                  result.StatusMessage = $"Instrument with ticker \"{fileInstrumentGroup.Value.Ticker}\" not defined, association with instrument group \"{fileInstrumentGroup.Value.Name}\" not created - define instruments first before importing instrument groups.";
                  logger.LogError(result.StatusMessage);
                }
              }
          }
        }
      }

      if (result.Severity == IDialogService.StatusMessageSeverity.Success)
        result.StatusMessage = $"";
      else
        //when some warning/error occurs add the counts
        result.StatusMessage += $" Import counts: Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}) - from \"{importSettings.Filename}\".";

      RaiseRefreshEvent();
      return result;
    }

    /// <summary>
    /// Returns the set of defined leaf instrument groups, that would be groups that have one or more instruments or groups that have no children.
    /// </summary>
    private IDictionary<Guid, Tuple<InstrumentGroup, bool>> getLeafInstrumentGroups()
    {
      SortedDictionary<Guid, Tuple<InstrumentGroup, bool>> result = new SortedDictionary<Guid, Tuple<InstrumentGroup, bool>>();
      IList<InstrumentGroup> instrumentGroups = m_instrumentGroupRepository.GetItems();

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
    private ExportResult exportCSV(string filename, IDictionary<Guid, Tuple<InstrumentGroup, bool>> instrumentGroups)
    {
      ExportResult result = new();
      long exportCount = 0;

      using (StreamWriter file = File.CreateText(filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        ILogger logger = m_loggerFactory.CreateLogger($"Exporting instrument groups to \"{filename}\"");
        IList<Instrument> instrumentsList = m_database.GetInstruments();

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
                result.Severity = IDialogService.StatusMessageSeverity.Error;
                result.StatusMessage = $"Ticker output are suppressed for CSV output since instrument group \"{lastInstrumentGroup.Name}, {lastInstrumentGroup.Tag}\" define instruments in a non-leaf node - rather use JSON output for this kind of configuration.";
                logger.LogError(result.StatusMessage);
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
                result.Severity = IDialogService.StatusMessageSeverity.Error;
                result.StatusMessage = $"Ticker output are suppressed for CSV output since instrument group \"{lastInstrumentGroup.Name}, {lastInstrumentGroup.Tag}\" define instruments in a non-leaf node - rather use JSON output for this kind of configuration.";
                logger.LogError(result.StatusMessage);
                includeTicker = false;
                break;
              }
            }
          }
        }

        exportCount = instrumentGroupsOutput.Count;

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
              {
                result.Severity = IDialogService.StatusMessageSeverity.Error;
                result.StatusMessage = $"Failed to find instrument \"{instrumentId.ToString()}\" associated with instrument group \"{lastInstrumentGroup.Name}, {lastInstrumentGroup.Tag}\".";
                logger.LogError(result.StatusMessage);
              }
          }
        }
      }

      if (result.Severity == IDialogService.StatusMessageSeverity.Success)
        result.StatusMessage = "Export success: ";
      else
        result.StatusMessage = "Export with warning/error: " + result.StatusMessage;
      result.StatusMessage += $" Exported {exportCount} groups to \"{filename}\"";

      return result;
    }

    private struct ImportCounts
    {
      public ImportCounts()
      {
        Skipped = 0;
        Updated = 0;
        Replaced = 0;
        Created = 0;
      }

      public long Skipped;
      public long Updated;
      public long Replaced;
      public long Created;
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
    private ImportResult importJSON(ImportSettings importSettings)
    {
      ImportResult result = new();
      ImportCounts counts = new();

      using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible
        ILogger logger = m_loggerFactory.CreateLogger($"Importing \"{importSettings.Filename}\"");

        if (documentNode != null)
        {
          Dictionary<Guid, InstrumentGroup> definedInstrumentGroups = new Dictionary<Guid, InstrumentGroup>();
          IList<InstrumentGroup> instrumentGroups = m_instrumentGroupRepository.GetItems();
          foreach (InstrumentGroup instrumentGroup in instrumentGroups) definedInstrumentGroups.Add(instrumentGroup.Id, instrumentGroup);

          Dictionary<string, Instrument> definedInstruments = new Dictionary<string, Instrument>();
          IList<Instrument> instruments = m_database.GetInstruments();
          foreach (Instrument instrument in instruments) definedInstruments.Add(instrument.Ticker, instrument);

          JsonArray rootNodes = documentNode.AsArray();
          result.Severity = IDialogService.StatusMessageSeverity.Success;
          foreach (JsonObject? node in rootNodes) if (node != null) result = importJsonNode(node!, importSettings.ReplaceBehavior, definedInstrumentGroups, definedInstruments, logger, result, counts);
        }
        else
        {
          result.Severity = IDialogService.StatusMessageSeverity.Error;
          result.StatusMessage = $"Failed to parse file \"{importSettings.Filename}\" as a JSON file.";
          logger.LogError(result.StatusMessage);
        }
      }

      if (result.Severity == IDialogService.StatusMessageSeverity.Success)
        result.StatusMessage = "Import success: ";
      else
        result.StatusMessage = "Import with warning/error: " + result.StatusMessage;
      result.StatusMessage += $" Skipped({counts.Skipped}), Replaced({counts.Replaced}), Updated({counts.Updated}), Created({counts.Created}) - from \"{importSettings.Filename}\".";

      RaiseRefreshEvent();
      return result;
    }

    private ImportResult importJsonNode(JsonObject node, ImportReplaceBehavior importReplaceBehavior, Dictionary<Guid, InstrumentGroup> definedInstrumentGroups, Dictionary<string, Instrument> definedInstruments, ILogger logger, ImportResult result, ImportCounts counts)
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
            counts.Skipped++;
            break;
          case ImportReplaceBehavior.Replace:
            //replacing name, description, tag and all defined instruments
            logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {name}, {description}, {tag}");
            m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, attributes, tag, parentId, name, description, instrumentList));
            counts.Replaced++;
            break;
          case ImportReplaceBehavior.Update:
            //updating name, description, tag and merge in defined instruments
            logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {name}, {description}, {tag}");

            foreach (Guid instrumentId in definedInstrumentGroup.Instruments)
              if (!instrumentList.Contains(instrumentId)) instrumentList.Add(instrumentId);

            m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, attributes, tag, parentId, name, description, instrumentList));
            counts.Updated++;
            break;
        }
      }
      else
      {
        m_instrumentGroupRepository.Add(new InstrumentGroup(id, attributes, tag, parentId, name, description, instrumentList));
        counts.Created++;
      }

      JsonArray? children = node.ContainsKey(tokenJsonChildren) ? node[tokenJsonChildren]!.AsArray() : null;
      if (children != null)
        foreach (JsonObject? childNode in children!)
          if (childNode != null)
            result = importJsonNode(childNode, importReplaceBehavior, definedInstrumentGroups, definedInstruments, logger, result, counts);

      return result;
    }

    private ExportResult exportJSON(string filename, IDictionary<Guid, Tuple<InstrumentGroup, bool>> instrumentGroups)
    {
      ExportResult result = new();
      int exportCount = 0;

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
            rootNodes.Add(writeJsonNode(InstrumentGroup.InstrumentGroupRoot, instrumentGroupTuple.Value.Item1, instrumentGroups, ref exportCount));
            break;
          }
      }
      else
      {
        //with no selected node we export from all nodes that are root node
        foreach (KeyValuePair<Guid, Tuple<InstrumentGroup, bool>> instrumentGroupTuple in instrumentGroups)
          if (instrumentGroupTuple.Value.Item1.ParentId == InstrumentGroup.InstrumentGroupRoot) rootNodes.Add(writeJsonNode(InstrumentGroup.InstrumentGroupRoot, instrumentGroupTuple.Value.Item1, instrumentGroups, ref exportCount));
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
          if (rootNodeIndex < rootNodeCount - 1) file.WriteLine(",");
        }
        file.WriteLine("");
        file.WriteLine("]");
      }

      if (result.Severity == IDialogService.StatusMessageSeverity.Success) result.StatusMessage = $"Exported {exportCount} groups to \"{filename}\"";
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
