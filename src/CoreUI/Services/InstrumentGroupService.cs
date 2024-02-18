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
using TradeSharp.Common;

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
    private const string tokenCsvTickers = "tickers";
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
      public InstrumentGroupRecord(Guid parentId, Guid id, string name, string description, string tag, Attributes attributes, List<string> tickers)
      {
        ParentId = parentId;
        Id = id;
        Name = name;
        Description = description;
        Tag = tag;
        Tickers = tickers;
        Attributes = attributes;
      }

      public Guid ParentId;
      public Guid Id;
      public string Name;
      public string Description;
      public string Tag;
      public List<string> Tickers;
      public Attributes Attributes;
    }

    //attributes
    private IDatabase m_database;
    private ILogger<InstrumentGroupService> m_logger;
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    private IInstrumentService m_instrumentService;
    private ITreeNodeType<Guid, InstrumentGroup>? m_selectedNode;
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> SelectedNodes { get; set; }
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> Nodes { get; internal set; }
    public ObservableCollection<InstrumentGroup> Items { get; internal set; }

    //constructors
    public InstrumentGroupService(ILogger<InstrumentGroupService> logger, IDatabase database, IInstrumentGroupRepository instrumentGroupRepository, IInstrumentService instrumentService, IDialogService dialogService) : base(dialogService)
    {
      m_logger = logger;
      m_database = database;
      m_instrumentGroupRepository = instrumentGroupRepository;
      m_instrumentService = instrumentService;
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

    public override void Import(ImportSettings importSettings)
    {
      string extension = Path.GetExtension(importSettings.Filename).ToLower();
      if (extension == extensionCSV)
        importCSV(importSettings);
      else if (extension == extensionJSON)
        importJSON(importSettings);
    }

    public override void Export(ExportSettings exportSettings)
    {
      //only output the data if the replacing the file is allowed
      if (exportSettings.ReplaceBehavior == ExportReplaceBehavior.Skip && File.Exists(exportSettings.Filename))
      {
        if (Debugging.InstrumentGroupExport) m_logger.LogWarning($"File \"{exportSettings.Filename}\" already exists and the export settings are set to skip it.");
        return;
      }

      IDictionary<Guid, Tuple<InstrumentGroup, bool>> leafNodes = getLeafInstrumentGroups();
      string extension = Path.GetExtension(exportSettings.Filename).ToLower();
      if (extension == extensionCSV)
        //exportCSV(exportSettings, leafNodes);
        exportCSV(exportSettings);
      else if (extension == extensionJSON)
        exportJSON(exportSettings, leafNodes);
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
    ///     Header line - must follow the pattern "Name, Description[, Tag], Name, Description[, Tag], etc. [, Tickers] " - field names in header is used to assign the field values for the instruments
    ///       Group 1 Name, Group 1 Description, Group 1 Tag, Group 1-1 Name, Group 1-1 Description, Group 1-1 Tag, Group 1-1-1 Name, Group 1-1-1, Group 1-1-1 Tag [, Ticker1, Ticker2, ... , TickerN ] 
    /// Uses - https://joshclose.github.io/CsvHelper/examples/reading/get-dynamic-records/
    /// NOTES:
    ///   - Tickers must be the last column in the line and the rest of the comma separated values are parsed as tickers.
    ///   - If instrument groups are defined that contain tickers under non-leaf nodes then the JSON format should be used to import the instrument groups.
    /// </summary>
    private void importCSV(ImportSettings importSettings)
    {
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;

      string statusMessage = $"Importing instrument groups - \"{importSettings.Filename}\"";
      if (Debugging.ImportExport) m_logger.LogInformation(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

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
          while (csv.Read())
          {
            string name = "";
            string description = "";
            string tag = "";
            Attributes attributes = InstrumentGroup.DefaultAttributeSet;
            List<string> tickers = new List<string>();

            lineNo++;

            for (int columnIndex = csv.HeaderRecord.Count() - 1; columnIndex >= 0; columnIndex--)
            {
              string? columnValue = null;
              if (csv.TryGetField(columnIndex, out columnValue))
              {
                columnValue = columnValue!.Trim();
                string columnHeader = csv.HeaderRecord[columnIndex].Trim().ToLower();

                if (columnHeader == tokenCsvName)
                {
                  name = columnValue!;
                  if (description == "") description = name;
                  if (tag == "") tag = name;

                  //create new group to be added for this line
                  currentLineRecords.Add(new InstrumentGroupRecord(SelectedNode != null ? SelectedNode.Id : InstrumentGroup.InstrumentGroupRoot, Guid.NewGuid(), name, description, tag, attributes, tickers));

                  //reset field values
                  name = "";
                  description = "";
                  tag = "";
                  attributes = InstrumentGroup.DefaultAttributeSet;
                  tickers = new List<string>();
                }
                else if (columnHeader == tokenCsvDescription)
                  description = columnValue!;
                else if (columnHeader == tokenCsvTag)
                  tag = columnValue!;
                else if (columnHeader == tokenCsvTickers)
                {
                  //tickers should be the last column in the line and the rest of the comma separated values are parsed as tickers
                  if (columnIndex == csv.HeaderRecord.Count() - 1)
                  {
                    int tickerCount = csv.ColumnCount - columnIndex;
                    for (int tickerIndex = 0; tickerIndex < tickerCount; tickerIndex++)
                      if (csv.TryGetField(columnIndex + tickerIndex, out string? tickerValue)) tickers.Add(tickerValue!.Trim().ToUpper());
                  }
                  else if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Tickers not defined as last item of line {lineNo}, use JSON file format to import instrument groups with non-leaf node ticker lists");
                }
                else if (columnHeader == tokenCsvAttributes)
                  try
                  {
                    attributes = (Attributes)Enum.Parse(typeof(Attributes), columnValue!);
                  }
                  catch (Exception e)
                  {
                    if (Debugging.InstrumentGroupImport) m_logger.LogError($"Error parsing attributes for line {lineNo}, column index {columnIndex} - {e.Message}");
                  }

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
            //construct the list of instruments associated with the instrument group in the file
            List<Guid> tickerIds = new List<Guid>();
            foreach (string ticker in fileInstrumentGroup.Value.Tickers)
            {
              Instrument? instrument = m_instrumentService.Items.FirstOrDefault(x => x.Ticker == ticker);
              if (instrument != null)
                tickerIds.Add(instrument.Id);
              else
                if (Debugging.InstrumentGroupImport) m_logger.LogError($"Instrument with ticker \"{ticker}\" not defined, association with instrument group \"{fileInstrumentGroup.Value.Name}\" not created - define instruments first before importing instrument groups.");
            }

            if (definedInstrumentGroups.TryGetValue(fileInstrumentGroup.Key, out InstrumentGroup? definedInstrumentGroup))
            {
              //merge in instruments in the current group if we require update behavior
              if (importSettings.ReplaceBehavior == ImportReplaceBehavior.Update)
                foreach (Guid instrumentId in definedInstrumentGroup.Instruments)
                  if (!tickerIds.Contains(instrumentId))
                    tickerIds.Add(instrumentId);

              //update the instrument group based on the replace behavior
              switch (importSettings.ReplaceBehavior)
              {
                case ImportReplaceBehavior.Skip:
                  if (Debugging.InstrumentGroupImport) m_logger.LogWarning($"Skipping - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag}");
                  skippedCount++;
                  break;
                case ImportReplaceBehavior.Replace:
                  //will update the name, description and tag and remove instrument associations
                  if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                  m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, tickerIds));
                  replacedCount++;
                  break;
                case ImportReplaceBehavior.Update:
                  //will update the name and description and keep all the associated instruments                  
                  if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
                  m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, tickerIds));
                  updatedCount++;
                  break;
              }
            }
            else
            {
              if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Creating - {fileInstrumentGroup.Value.Name}, {fileInstrumentGroup.Value.Description}, {fileInstrumentGroup.Value.Tag}");
              m_instrumentGroupRepository.Add(new InstrumentGroup(fileInstrumentGroup.Value.Id, fileInstrumentGroup.Value.Attributes, fileInstrumentGroup.Value.Tag, fileInstrumentGroup.Value.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, tickerIds));
              createdCount++;
            }
          }
        }
      }

      if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Completed import from \"{importSettings.Filename}\" - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Completed import from \"{importSettings.Filename}\" - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
      RaiseRefreshEvent();
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
    /// Make string value safe to use as a CSV value.
    /// </summary>
    private string makeCsvSafe(string value)
    {
      if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        return "\"" + value.Replace("\"", "\"\"") + "\"";
      else
        return value;
    }

    /// <summary>
    /// Storage structure for each line to output to the CSV file, the line is constructed as the tree is traversed.
    /// </summary>
    private struct ExportCsvLine
    {
      public ExportCsvLine()
      {
        InstrumentGroupsOnLine = 0;
        Line = string.Empty;
      }

      public int InstrumentGroupsOnLine;
      public string Line;
    }

    /// <summary>
    /// Recurse down a branches for each of the leaf nodes of the tree.
    /// </summary>
    private void exportCsvForInstrumentGroupNode(ITreeNodeType<Guid, InstrumentGroup> node, ExportCsvLine currentLine, List<ExportCsvLine> exportCsvLines, ref int instrumentGroupExportCount, ref bool includeTickersInHeader)
    {
      instrumentGroupExportCount++;
      currentLine.InstrumentGroupsOnLine++;
      if (currentLine.Line.Length > 0) currentLine.Line += ",";

      //add node data to the line
      currentLine.Line += makeCsvSafe(node.Item.Name);
      currentLine.Line += ",";
      currentLine.Line += makeCsvSafe(node.Item.Description);
      currentLine.Line += ",";
      currentLine.Line += makeCsvSafe(node.Item.Tag);
      currentLine.Line += ",";
      int attributeSet = (int)node.Item.AttributeSet;
      currentLine.Line += attributeSet.ToString();

      //add instruments to the line if it's a leaf node
      //NOTE: For CSV files only leaf nodes can contain ticker associations due to the file format restrictions
      if (node.Children.Count > 0)
      {
        if (Debugging.InstrumentGroupExport && node.Item.Instruments.Count > 0) m_logger.LogInformation($"Node \"{node.Item.Id}, {node.Item.Name}, {node.Item.Tag}\" contains both tickers and instrument group children, tickers are skipped - use JSON to retain these relationships");

        foreach (ITreeNodeType<Guid, InstrumentGroup> child in node.Children)
          exportCsvForInstrumentGroupNode(child, currentLine, exportCsvLines, ref instrumentGroupExportCount, ref includeTickersInHeader);
      }
      else
      {
        //append the tickers to the end of the CSV line if needed
        if (node.Item.Instruments.Count > 0)
        {
          includeTickersInHeader = true;
          foreach (Guid instrumentId in node.Item.Instruments)
          {
            Instrument? instrument = m_instrumentService.Items.FirstOrDefault(x => x.Id == instrumentId);
            if (instrument != null)
              currentLine.Line += "," + instrument.Ticker;
            else
              if (Debugging.InstrumentGroupExport) m_logger.LogError($"Failed to find instrument \"{instrumentId.ToString()}\" associated with instrument group \"{node.Item.Name}, {node.Item.Tag}\".");
          }
        }

        //add the line to the set of lines to be output
        exportCsvLines.Add(currentLine);
      }
    }

    /// <summary>
    /// Exports the instrument group data into the given file, each row would include a single instrument group definition with all it's associated
    /// parents and then followed by and instrument tickers associated with it. Example CSV format would be:
    ///     Parent Name, Parent Description, Parent Tag, Child Name, Child Description, Child Tag, Child Name, Child Description, Child Tag
    ///     Parent Name, Parent Description, Parent Tag, Child Name, Child Description, Child Tag, Child Name, Child Description, Child Tag, Ticker1, Ticker2,  ... , TickerN
    /// NOTES:
    ///   - Tickers are only included if the instrument group is a leaf node, i.e. it has no children.
    ///   - If instrument groups are defined that contain tickers under non-leaf nodes then the JSON format should be used to export the instrument groups.
    ///   - Do NOT output spaces after the commas in the CSV file, it will cause the CSV parser to treat the space as part of the field value and lead to parsing errors.
    /// </summary>
    private void exportCSV(ExportSettings exportSettings)
    {
      try
      {
        bool includeTickersInHeader = false;
        int instrumentGroupExportCount = 0;
        List<ExportCsvLine> exportLines = new List<ExportCsvLine>();
        using (StreamWriter file = File.CreateText(exportSettings.Filename))
        {
          string statusMessage = $"Exporting instrument groups to \"{exportSettings.Filename}\"";
          if (Debugging.InstrumentGroupExport) m_logger.LogInformation(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

          //construct the list of lines to be output to the csv file, selected node data vs all root nodes
          ExportCsvLine currentLine = new ExportCsvLine();
          if (SelectedNode != null)
            exportCsvForInstrumentGroupNode(SelectedNode, currentLine, exportLines, ref instrumentGroupExportCount, ref includeTickersInHeader);
          else
            foreach (ITreeNodeType<Guid, InstrumentGroup> node in Nodes)
            {
              currentLine = new ExportCsvLine();
              exportCsvForInstrumentGroupNode(node, currentLine, exportLines, ref instrumentGroupExportCount, ref includeTickersInHeader);
            }

          //determine the longest branch of instrument groups to be output, this will determine the header for the CSV file
          int longestBranch = 0;
          foreach (ExportCsvLine exportLine in exportLines)
            if (exportLine.InstrumentGroupsOnLine > longestBranch) longestBranch = exportLine.InstrumentGroupsOnLine;
          
          string headerLine = "";
          for (int i = 0; i < longestBranch; i++)
          {
            if (headerLine.Length > 0) headerLine += ",";
            headerLine += $"{tokenCsvName},{tokenCsvDescription},{tokenCsvTag},{tokenCsvAttributes}";
          }

          if (includeTickersInHeader) headerLine += $",{tokenCsvTickers}";

          //output the data to the file
          file.WriteLine(headerLine);
          foreach (ExportCsvLine exportLine in exportLines) file.WriteLine(exportLine.Line);

          if (Debugging.InstrumentGroupExport) m_logger.LogInformation($"Exported {instrumentGroupExportCount} groups to \"{exportSettings.Filename}\" in {exportLines.Count} lines");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Exported {instrumentGroupExportCount} groups to \"{exportSettings.Filename}\" in {exportLines.Count} lines");
        }
      }
      catch (Exception e)
      {
        if (Debugging.InstrumentGroupExport) m_logger.LogError($"Failed to export instrument groups to \"{exportSettings.Filename}\" - {e.Message}");
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Failed to export instrument groups to \"{exportSettings.Filename}\" - {e.Message}");
      }
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
    private void importJSON(ImportSettings importSettings)
    {
      ImportCounts counts = new();
      bool noErrors = true;

      using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible
        string statusMessage = $"Importing instrument groups from \"{importSettings.Filename}\"";
        IDisposable? loggerScope = null;
        if (Debugging.InstrumentGroupImport) loggerScope = m_logger.BeginScope(statusMessage);
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

        if (documentNode != null)
        {
          Dictionary<Guid, InstrumentGroup> definedInstrumentGroups = new Dictionary<Guid, InstrumentGroup>();
          IList<InstrumentGroup> instrumentGroups = m_instrumentGroupRepository.GetItems();
          foreach (InstrumentGroup instrumentGroup in instrumentGroups) definedInstrumentGroups.Add(instrumentGroup.Id, instrumentGroup);

          Dictionary<string, Instrument> definedInstruments = new Dictionary<string, Instrument>();
          IList<Instrument> instruments = m_database.GetInstruments();
          foreach (Instrument instrument in instruments) definedInstruments.Add(instrument.Ticker, instrument);

          JsonArray rootNodes = documentNode.AsArray();
          foreach (JsonObject? node in rootNodes) if (node != null) importJsonNode(node!, importSettings.ReplaceBehavior, definedInstrumentGroups, definedInstruments, m_logger, counts);
        }
        else
        {
          statusMessage = $"Failed to parse file \"{importSettings.Filename}\" as a JSON file.";
          if (Debugging.InstrumentGroupImport) m_logger.LogError(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);
          noErrors = false;
        }
      }

      if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Completed instument group import from \"{importSettings.Filename}\" - Skipped({counts.Skipped}), Replaced({counts.Replaced}), Updated({counts.Updated}), Created({counts.Created}).");
      if (noErrors) m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Completed instument group import from \"{importSettings.Filename}\" - Skipped({counts.Skipped}), Replaced({counts.Replaced}), Updated({counts.Updated}), Created({counts.Created}).");
      RaiseRefreshEvent();
    }

    private void importJsonNode(JsonObject node, ImportReplaceBehavior importReplaceBehavior, Dictionary<Guid, InstrumentGroup> definedInstrumentGroups, Dictionary<string, Instrument> definedInstruments, ILogger logger, ImportCounts counts)
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
          if (childNode != null) importJsonNode(childNode, importReplaceBehavior, definedInstrumentGroups, definedInstruments, logger, counts);
    }

    private void exportJSON(ExportSettings exportSettings, IDictionary<Guid, Tuple<InstrumentGroup, bool>> instrumentGroups)
    {
      int exportCount = 0;
      string statusMessage = $"Exporting instrument groups to \"{exportSettings.Filename}\"";
      IDisposable? loggerScope = null;
      if (Debugging.InstrumentGroupExport) loggerScope = m_logger.BeginScope(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

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

      using (StreamWriter file = File.CreateText(exportSettings.Filename))   //NOTE: This will always overwrite the text file if it exists.
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

      if (Debugging.InstrumentGroupExport) m_logger.LogInformation($"Exported {exportCount} groups to \"{exportSettings.Filename}\"");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} groups to \"{exportSettings.Filename}\"");
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
