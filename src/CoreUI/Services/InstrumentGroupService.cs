using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private const string tokenCsvId = "id";
    private const string tokenCsvParentId = "parentid";
    private const string tokenCsvParentName = "parentname";
    private const string tokenCsvName = "name";
    private const string tokenCsvAlternateNames = "alternatenames";
    private const string tokenCsvDescription = "description";
    private const string tokenCsvUserId = "userid";
    private const string tokenCsvTag = "tag";
    private const string tokenCsvTickers = "tickers";
    private const string tokenCsvAttributes = "attributes";
    private const string tokenJsonId = "Id";
    private const string tokenJsonName = "Name";
    private const string tokenJsonAlternateNames = "AlternateNames";
    private const string tokenJsonDescription = "Description";
    private const string tokenJsonUserId = "UserId";
    private const string tokenJsonTag = "Tag";
    private const string tokenJsonAttributes = "Attributes";
    private const string tokenJsonInstruments = "Instruments";
    private const string tokenJsonChildren = "Children";

    //enums


    //types
    private class InstrumentGroupRecord : IEquatable<InstrumentGroupRecord>, IEquatable<InstrumentGroup>
    {
      public InstrumentGroupRecord(Guid parentId, Guid id, string parentName, string name, List<string> alternateNames, string description, string userId, string tag, Attributes attributes, List<string> tickers)
      {
        ParentId = parentId;
        Id = id;
        ParentName = parentName;
        Name = name;
        AlternateNames = alternateNames;
        Description = description;
        UserId = userId;
        Tag = tag;
        Tickers = tickers;
        Attributes = attributes;
      }

      public Guid ParentId;
      public Guid Id;
      public string ParentName;
      public string Name;
      public List<string> AlternateNames;
      public string Description;
      public string UserId;
      public string Tag;
      public List<string> Tickers;
      public Attributes Attributes;

      public bool Equals(InstrumentGroup? other)
      {
        if (other != null)
        {
          if (other.Id == Id) return true;
          if (other.UserId.Trim().ToUpper() == UserId.Trim().ToUpper()) return true;
          if (other.Name.Trim().ToUpper() == Name.Trim().ToUpper()) return true;
          if (other.AlternateNames.FirstOrDefault(x => AlternateNames.Contains(x)) != null) return true;
        }
        return false;
      }

      public bool Equals(InstrumentGroupRecord? other)
      {
        if (other != null)
        {
          if (other.Id == Id) return true;
          if (other.UserId.ToUpper() == UserId.ToUpper()) return true;
          if (other.Name.ToUpper() == Name.ToUpper()) return true;
          if (other.AlternateNames.FirstOrDefault(x => AlternateNames.Contains(x)) != null) return true;
        }
        return false;
      }

      public bool IsChildOf(InstrumentGroupRecord other)
      {
        if (other != null)
        {
          if (other.Id == ParentId) return true;
          string parentName = ParentName.ToUpper();
          if (other.Name.ToUpper() == parentName) return true;
          if (other.AlternateNames.FirstOrDefault(x => x.ToUpper() == parentName) != null) return true;
        }
        return false;
      }

      public bool IsChildOf(InstrumentGroup other)
      {
        if (other != null)
        {
          if (other.Id == ParentId) return true;
          string parentName = ParentName.ToUpper();
          if (other.Name.ToUpper() == parentName) return true;
          if (other.AlternateNames.FirstOrDefault(x => x.ToUpper() == parentName) != null) return true;
        }
        return false;
      }
    }

    //attributes
    private IDatabase m_database;
    private ILogger<InstrumentGroupService> m_logger;
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    private IInstrumentService m_instrumentService;

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
      m_instrumentService.RefreshEvent += onInstrumentServiceRefresh;
    }

    //finalizers
    ~InstrumentGroupService()
    {
      m_instrumentService.RefreshEvent -= onInstrumentServiceRefresh;
    }

    //interface implementations


    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    public Guid RootNodeId => InstrumentGroup.InstrumentGroupRoot;
    [ObservableProperty] ITreeNodeType<Guid, InstrumentGroup>? m_selectedNode;
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> SelectedNodes { get; set; }
    public ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> Nodes { get; internal set; }
    public ObservableCollection<InstrumentGroup> Items { get; internal set; }

    //methods
    public bool Add(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      var result = m_instrumentGroupRepository.Add(node.Item);
      if (node.ParentId == InstrumentGroup.InstrumentGroupRoot)
        Nodes.Add(node);
      else
        Refresh(node.Item.ParentId);
      SelectedNode = getNode(node.Item.Id, Nodes);
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
      return result;
    }

    public bool Delete(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      bool result = m_instrumentGroupRepository.Delete(node.Item);

      if (node == SelectedNode)
        SelectedNode = null;
      return result;
    }

    public void Refresh()
    {
      LoadedState = LoadedState.Loading;
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
          Nodes.Add(new InstrumentGroupNodeType(this, m_instrumentService, null, item, true));   //null since parent is the root node
      SelectedNode = Nodes.FirstOrDefault(x => x.ParentId == InstrumentGroup.InstrumentGroupRoot); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      LoadedState = LoadedState.Loaded;
      raiseRefreshEvent();
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
        importCsv(importSettings);
      else if (extension == extensionJSON)
        importJson(importSettings);
    }

    public override void Export(ExportSettings exportSettings)
    {
      //only output the data if the replacing the file is allowed
      if (exportSettings.ReplaceBehavior == ExportReplaceBehavior.Skip && File.Exists(exportSettings.Filename))
      {
        if (Debugging.InstrumentGroupExport) m_logger.LogWarning($"File \"{exportSettings.Filename}\" already exists and the export settings are set to skip it.");
        return;
      }

      string extension = Path.GetExtension(exportSettings.Filename).ToLower();
      if (extension == extensionCSV)
        exportCsv(exportSettings);
      else if (extension == extensionJSON)
        exportJson(exportSettings);
    }

    protected void onInstrumentServiceRefresh(object sender, EventArgs e)
    {
      //recursively process the instrument groups to update the instrument descriptions
      foreach (ITreeNodeType<Guid, InstrumentGroup> node in Nodes)
      {
        var instrumentGroupNode = (InstrumentGroupNodeType)node;
        instrumentGroupNode.RefreshInstruments();
      }
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
    /// Imports the instrument group definitions from the given file, format must be as follows (column order does not matter but be cognizant of :
    ///   [ParentId,Id,][ParentName,]Name,AlternateNames,Description,UserId,Tag,Attributes,Tickers
    /// NOTES:
    ///   - Nodes in the instrument groups needs to be defined in depth first order, i.e. the parent node must be defined before any child node referencing it in the ParentName.
    ///   - If ParentId and Id are defined then the ParentName is not required, if ParentId and Id are not defined then the ParentName is required.
    ///   - Parent name should be blank if the instrument group is a root node.
    ///   - Instrument groups need to be unique according to their name.
    ///   - Alternate names is a list other names that can be used to match the instrument group - some data providers use different names for the same instrument group.
    ///   - Tickers is a list of instrument tickers associated with the instrument group.
    /// E.g.
    ///   "",MSCI GICS,,MSCI GICS,,,""
    ///   MSCI GICS,Equities,"Stocks,Shares",Equity instruments,admin,,"AAPL,MSFT,GOOG"
    /// </summary>
    /// Method is made public to allow for testing.
    public void importCsv(ImportSettings importSettings)
    {
      long skippedCount = 0;
      long updatedCount = 0;
      long replacedCount = 0;
      long createdCount = 0;

      string statusMessage = $"Importing instrument groups - \"{importSettings.Filename}\"";
      if (Debugging.InstrumentGroupImport) m_logger.LogInformation(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);
      List<string> errorColumns = new List<string>();

      List<InstrumentGroupRecord> fileInstrumentGroups = new List<InstrumentGroupRecord>();

      using (var reader = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {

        if (csv.Read() && csv.ReadHeader() && csv.HeaderRecord != null)
        {
          int lineNo = 1; //header row is on line 0
          while (csv.Read())
          {
            Guid id = Guid.Empty;
            Guid parentId = Guid.Empty;
            string parentName = "";
            string name = "";
            List<string> alternateNames = new List<string>();
            string description = "";
            string userId = "";
            string tag = "";
            Attributes attributes = InstrumentGroup.DefaultAttributeSet;
            List<string> tickers = new List<string>();

            lineNo++;

            //parse the columns in the row
            for (int columnIndex = 0;  columnIndex < csv.HeaderRecord.Count(); columnIndex++)
            {
              string? columnValue = null;
              if (csv.TryGetField(columnIndex, out columnValue))
              {
                columnValue = columnValue!.Trim();  //always trim the column value to ensure comparisons work well
                string columnHeader = csv.HeaderRecord[columnIndex].Trim().ToLower();

                if (columnHeader == tokenCsvParentId)
                {
                  if (Guid.TryParse(columnValue, out Guid result))
                    parentId = result;
                  else
                  {
                    if (Debugging.InstrumentGroupImport) m_logger.LogError($"Error parsing parent Id {columnValue} on line {lineNo}, node will be placed under the root");
                    parentId = InstrumentGroup.InstrumentGroupRoot;
                  }
                }
                if (columnHeader == tokenCsvId)
                {
                  if (Guid.TryParse(columnValue, out Guid result))
                    id = result;
                  else
                    if (Debugging.InstrumentGroupImport) m_logger.LogError($"Error parsing Id {columnValue} on line {lineNo}");
                }
                if (columnHeader == tokenCsvParentName)
                {
                  parentName = columnValue!;
                  //set parent id to root if the parent name is blank and parentId not yet explicitly set
                  if (parentName == "" && parentId == Guid.Empty) parentId = InstrumentGroup.InstrumentGroupRoot;
                }
                else if (columnHeader == tokenCsvName)
                  name = columnValue!;
                else if (columnHeader == tokenCsvAlternateNames && columnValue!.Trim() != "")
                {
                  string[] alternateNamesArray = columnValue!.Split(',');
                  foreach (string alternateName in alternateNamesArray) alternateNames.Add(alternateName.Trim());
                }
                else if (columnHeader == tokenCsvDescription)
                  description = columnValue!;
                else if (columnHeader == tokenCsvUserId)
                  userId = columnValue!;
                else if (columnHeader == tokenCsvTag)
                  tag = columnValue!;
                else if (columnHeader == tokenCsvTickers)
                {
                  string[] tickersArray = columnValue!.Split(',');
                  foreach (string ticker in tickersArray)
                  {
                    string normalizedTicker = ticker.Trim().ToUpper();
                    if (normalizedTicker != "" && !tickers.Contains(normalizedTicker)) tickers.Add(ticker.Trim().ToUpper());   //we need to ensure the tickers are trimmed and in upper case to allow quick comparisons
                  }
                }
                else if (columnHeader == tokenCsvAttributes)
                  try
                  {
                    attributes = (Attributes)Enum.Parse(typeof(Attributes), columnValue!);
                  }
                  catch (Exception e)
                  {
                    if (Debugging.InstrumentGroupImport) m_logger.LogError($"Error parsing attributes for line {lineNo}, column index {columnIndex}, using default - {e.Message}");
                  }
                else
                  //header column not found, log error and ignore it
                  if (Debugging.InstrumentGroupImport && errorColumns.FirstOrDefault(x => x == columnHeader) == null)
                  {
                    errorColumns.Add(columnHeader);
                    m_logger.LogError($"Failed to find header column to process it {columnHeader}");
                  }
              }
            }

            fileInstrumentGroups.Add(new InstrumentGroupRecord(parentId, id, parentName, name, alternateNames, description, userId, tag, attributes, tickers));
          }
        }
        else
        {
          if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Failed to parse header in \"{importSettings.Filename}\", aborting import.");
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Failed to parse header in \"{importSettings.Filename}\", aborting import.");
        }
      }

      //create/update instrument groups if we could parse any of them
      if (fileInstrumentGroups.Count > 0)
      {
        //get the set of instrument groups already defined and update the parentId and Id keys for groups loaded from the file
        List<InstrumentGroup> definedInstrumentGroups = new List<InstrumentGroup>();
        IList<InstrumentGroup> persistedInstrumentGroups = m_instrumentGroupRepository.GetItems();
        foreach (InstrumentGroup instrumentGroup in persistedInstrumentGroups)
        {
          string tag = instrumentGroup.Tag.Length != 0 ? instrumentGroup.Tag : instrumentGroup.Name;
          definedInstrumentGroups.Add(instrumentGroup);

          //update any instrument groups defined in the file with the existing parentId and Id keys defined for the instrument group
          InstrumentGroupRecord? instrumentGroupRecord = fileInstrumentGroups.FirstOrDefault(x => x.Equals(instrumentGroup));
          if (instrumentGroupRecord != null)
          {
            //update any children to use the persisted instrument group Id and parent Id
            foreach (InstrumentGroupRecord child in fileInstrumentGroups)
              if (child.IsChildOf(instrumentGroupRecord)) child.ParentId = instrumentGroup.Id;

            //update the instrument group as well
            instrumentGroupRecord.ParentId = instrumentGroup.ParentId;
            instrumentGroupRecord.Id = instrumentGroup.Id;
          }
        }

        //create id's for groups that are new and were not assigned existing id's above
        foreach (InstrumentGroupRecord instrumentGroupRecord in fileInstrumentGroups)
          if (instrumentGroupRecord.Id == Guid.Empty)
            instrumentGroupRecord.Id = Guid.NewGuid();

        //try to find parents for groups that were not parented yet
        foreach (InstrumentGroupRecord instrumentGroupRecord in fileInstrumentGroups)
          if (instrumentGroupRecord.ParentId == Guid.Empty)
          {
            InstrumentGroupRecord? parentRecord = fileInstrumentGroups.FirstOrDefault(x => instrumentGroupRecord.IsChildOf(x));
            if (parentRecord != null)
              instrumentGroupRecord.ParentId = parentRecord.Id;
            else
            {
              if (Debugging.InstrumentGroupImport) m_logger.LogError($"Parent \"{instrumentGroupRecord.ParentName}\" not found for \"{instrumentGroupRecord.Name}\" - defaulting to root node");
              instrumentGroupRecord.ParentId = InstrumentGroup.InstrumentGroupRoot;
            }
          }

        //update/create instrument groups in the repository
        foreach (InstrumentGroupRecord fileInstrumentGroup in fileInstrumentGroups)
        {
          //validate the list of instruments associated with the instrument group in the file
          foreach (string ticker in fileInstrumentGroup.Tickers)
          {
            Instrument? instrument = m_instrumentService.Items.FirstOrDefault(x => x.Ticker == ticker || x.AlternateTickers.Contains(ticker));
            if (instrument == null && Debugging.InstrumentGroupImport)
              m_logger.LogError($"Instrument with ticker \"{ticker}\" not defined, association with instrument group \"{fileInstrumentGroup.Name}\" not created - define instruments first before importing instrument groups.");
          }

          //update the instrument group based on the replace behavior
          InstrumentGroup? definedInstrumentGroup = definedInstrumentGroups.FirstOrDefault(x => fileInstrumentGroup.Equals(x));
          if (definedInstrumentGroup != null)
          {
            //update the instrument group based on the replace behavior
            switch (importSettings.ReplaceBehavior)
            {
              case ImportReplaceBehavior.Skip:
                if (Debugging.InstrumentGroupImport) m_logger.LogWarning($"Skipping - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag}");
                skippedCount++;
                break;
              case ImportReplaceBehavior.Replace:
                //will update the name, description and tag and remove instrument associations
                if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Name}, {fileInstrumentGroup.Description}, {fileInstrumentGroup.Tag}");
                m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Attributes, fileInstrumentGroup.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Name, fileInstrumentGroup.AlternateNames, fileInstrumentGroup.Description, fileInstrumentGroup.UserId, fileInstrumentGroup.Tickers));
                replacedCount++;
                break;
              case ImportReplaceBehavior.Update:
                //will update the name and description and keep all the associated instruments                  
                if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {fileInstrumentGroup.Name}, {fileInstrumentGroup.Description}, {fileInstrumentGroup.Tag}");

                foreach (string alternateName in definedInstrumentGroup.AlternateNames)
                {
                  string upperAlternateName = alternateName.ToUpper();
                  if (fileInstrumentGroup.AlternateNames.FirstOrDefault(x => x.ToUpper() == upperAlternateName) == null) fileInstrumentGroup.AlternateNames.Add(alternateName);
                }

                foreach (string instrumentTicker in definedInstrumentGroup.Instruments)
                  if (!fileInstrumentGroup.Tickers.Contains(instrumentTicker))
                    fileInstrumentGroup.Tickers.Add(instrumentTicker);

                m_instrumentGroupRepository.Update(new InstrumentGroup(definedInstrumentGroup.Id, fileInstrumentGroup.Attributes, fileInstrumentGroup.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Name, fileInstrumentGroup.AlternateNames, fileInstrumentGroup.Description, fileInstrumentGroup.UserId, fileInstrumentGroup.Tickers));
                updatedCount++;
                break;
            }
          }
          else
          {
            if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Creating - {fileInstrumentGroup.Name}, {fileInstrumentGroup.Description}, {fileInstrumentGroup.Tag}");
            m_instrumentGroupRepository.Add(new InstrumentGroup(fileInstrumentGroup.Id, fileInstrumentGroup.Attributes, fileInstrumentGroup.Tag, fileInstrumentGroup.ParentId, fileInstrumentGroup.Name, fileInstrumentGroup.AlternateNames, fileInstrumentGroup.Description, fileInstrumentGroup.UserId, fileInstrumentGroup.Tickers));
            createdCount++;
          }
        }

        if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"Completed import from \"{importSettings.Filename}\" - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", $"Completed import from \"{importSettings.Filename}\" - Skipped({skippedCount}), Replaced({replacedCount}), Updated({updatedCount}), Created({createdCount}).");
        raiseRefreshEvent();
      }
      else
      {
        if (Debugging.InstrumentGroupImport) m_logger.LogInformation($"No data found to import from \"{importSettings.Filename}\".");
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "", $"No data found to import from \"{importSettings.Filename}\".");
      }
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
    /// Recurse down branches for each of the leaf nodes of the tree.
    /// </summary>
    private void exportCsvForInstrumentGroupNode(ITreeNodeType<Guid, InstrumentGroup> node, List<string> exportCsvLines, ref int instrumentGroupExportCount)
    {
      instrumentGroupExportCount++;

      //add node data to the line
      string line = node.Item.ParentId.ToString();
      line += ",";
      line += node.Item.Id.ToString();
      line += ",";
      line += TradeSharp.Common.Utilities.MakeCsvSafe(node.Item.Name);
      line += ",";

      if (node.Item.AlternateNames.Count > 0)
      {
        string alternateNames = "";
        foreach (string name in node.Item.AlternateNames)
        {
          if (alternateNames.Length > 0) alternateNames += ",";
          alternateNames += name;
        }
        line += TradeSharp.Common.Utilities.MakeCsvSafe(alternateNames);
      }

      line += ",";
      line += TradeSharp.Common.Utilities.MakeCsvSafe(node.Item.Description);
      line += ",";
      line += TradeSharp.Common.Utilities.MakeCsvSafe(node.Item.UserId);
      line += ",";
      line += TradeSharp.Common.Utilities.MakeCsvSafe(node.Item.Tag);
      line += ",";
      int attributeSet = (int)node.Item.AttributeSet;
      line += attributeSet.ToString();

      //append the tickers to the end of the CSV line if needed
      line += ",";
      if (node.Item.Instruments.Count > 0)
      {
        string tickers = "";
        foreach (string instrumentTicker in node.Item.Instruments)
        {
          if (tickers.Length > 0) tickers += ",";
          tickers += instrumentTicker;
        }
        line += TradeSharp.Common.Utilities.MakeCsvSafe(tickers);
      }

      //add the line to the set of lines to be output
      exportCsvLines.Add(line);

      //add instruments to the line if it's a leaf node
      //NOTE: For CSV files only leaf nodes can contain ticker associations due to the file format restrictions
      if (node.Children.Count > 0)
      {
        if (Debugging.InstrumentGroupExport && node.Item.Instruments.Count > 0) m_logger.LogInformation($"Node \"{node.Item.Id}, {node.Item.Name}, {node.Item.Tag}\" contains both tickers and instrument group children, tickers are skipped - use JSON to retain these relationships");

        foreach (ITreeNodeType<Guid, InstrumentGroup> child in node.Children)
          exportCsvForInstrumentGroupNode(child, exportCsvLines, ref instrumentGroupExportCount);
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
    private void exportCsv(ExportSettings exportSettings)
    {
      try
      {
        int instrumentGroupExportCount = 0;
        List<string> exportLines = new List<string>();
        using (StreamWriter file = File.CreateText(exportSettings.Filename))
        {
          string statusMessage = $"Exporting instrument groups to \"{exportSettings.Filename}\"";
          if (Debugging.InstrumentGroupExport) m_logger.LogInformation(statusMessage);
          m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

          foreach (ITreeNodeType<Guid, InstrumentGroup> node in Nodes)
            exportCsvForInstrumentGroupNode(node, exportLines, ref instrumentGroupExportCount);

          //output the data to the file - in the header we output the parent Id and node Id in order to keep parsing it on import simple
          file.WriteLine($"{tokenCsvParentId},{tokenCsvId},{tokenCsvName},{tokenCsvAlternateNames},{tokenCsvDescription},{tokenCsvUserId},{tokenCsvTag},{tokenCsvAttributes},{tokenCsvTickers}");
          foreach (string exportLine in exportLines) file.WriteLine(exportLine);

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
    ///     "alternateNames" : ["Another name1", "Another name2",...,"Another NameN"],
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
    private void importJson(ImportSettings importSettings)
    {
      ImportCounts counts = new();
      bool noErrors = true;

      using (StreamReader file = new StreamReader(importSettings.Filename, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read }))
      {
        JsonNode? documentNode = JsonNode.Parse(file.ReadToEnd(), new JsonNodeOptions { PropertyNameCaseInsensitive = true }, new JsonDocumentOptions { AllowTrailingCommas = true });  //try make the parsing as forgivable as possible
        string statusMessage = $"Importing instrument groups from \"{importSettings.Filename}\"";
        if (Debugging.InstrumentGroupImport) m_logger.LogInformation(statusMessage);
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

        if (documentNode != null)
        {
          List<InstrumentGroup> definedInstrumentGroups = new List<InstrumentGroup>();
          IList<InstrumentGroup> instrumentGroups = m_instrumentGroupRepository.GetItems();
          foreach (InstrumentGroup instrumentGroup in instrumentGroups) definedInstrumentGroups.Add(instrumentGroup);

          Guid parentId = SelectedNode != null ? SelectedNode.Id : InstrumentGroup.InstrumentGroupRoot; //determine which node would be the root node for all the imported nodes, the root nodes in the import data should be associated with the root node Id used for instrument groups
          JsonArray rootNodes = documentNode.AsArray();
          foreach (JsonObject? node in rootNodes) if (node != null) importJsonNode(parentId, node!, importSettings.ReplaceBehavior, definedInstrumentGroups, m_database.GetInstruments(), m_logger, ref counts);
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
      raiseRefreshEvent();
    }

    private void importJsonNode(Guid parentId, JsonObject node, ImportReplaceBehavior importReplaceBehavior, IList<InstrumentGroup> definedInstrumentGroups, IList<Instrument> definedInstruments, ILogger logger, ref ImportCounts counts)
    {
      Guid id = Guid.Empty;
      string name = "";
      string description = "";
      string userId = "";
      string tag = "";
      string? attributesStr = null;
      Attributes attributes = InstrumentGroup.DefaultAttributeSet;

      try
      {
        id = node.ContainsKey(tokenJsonId) ? (Guid)(node[tokenJsonId]!.AsValue().Deserialize(typeof(Guid)))! : Guid.NewGuid();
        name = (string)(node[tokenJsonName]!.AsValue().Deserialize(typeof(string)))!;
        description = (string?)(node[tokenJsonDescription]!.AsValue().Deserialize(typeof(string))) ?? name;
        userId = node.ContainsKey(tokenJsonUserId) ? (string?)(node[tokenJsonUserId]!.AsValue().Deserialize(typeof(string)))! : name;
        tag = node.ContainsKey(tokenJsonTag) ? (string?)(node[tokenJsonTag]!.AsValue().Deserialize(typeof(string)))! : name;
        attributesStr = node.ContainsKey(tokenJsonAttributes) ? (string?)(node[tokenJsonAttributes]!.AsValue().Deserialize(typeof(string)))! : null;
        attributes = InstrumentGroup.DefaultAttributeSet;

        if (Enum.TryParse(typeof(Attributes), attributesStr, out object? result))
          attributes = (Attributes)result;
        else
          if (Debugging.InstrumentGroupImport) m_logger.LogError($"Failed to parse attributes for instrument group \"{name}\" - defaulting to default attribute set.");

        //parse fields with list structures
        JsonArray? alternateNames = node.ContainsKey(tokenJsonAlternateNames) ? node[tokenJsonAlternateNames]!.AsArray() : null;
        JsonArray? instruments = node.ContainsKey(tokenJsonInstruments) ? node[tokenJsonInstruments]!.AsArray() : null;

        List<string> alternateNamesList = new List<string>();
        if (alternateNames != null)
          foreach (JsonNode? alternateNameNode in alternateNames!)
            if (alternateNameNode != null)
              alternateNamesList.Add((string)alternateNameNode.Deserialize(typeof(string))!);

        List<string> instrumentTickers = new List<string>();
        if (instruments != null)
          foreach (JsonNode? instrumentNode in instruments!)
            if (instrumentNode != null)
            {
              string instrumentTicker = (string)instrumentNode.Deserialize(typeof(string))!;
              Instrument? value = definedInstruments.FirstOrDefault(x => x.Ticker == instrumentTicker || x.AlternateTickers.Contains(instrumentTicker));
              if (value != null)
                instrumentTickers.Add(value.Ticker);
              else
              {
                if (Debugging.InstrumentGroupImport) m_logger.LogError($"Instrument with ticker \"{instrumentTicker}\" not defined, association with instrument group \"{name}\" not created - define instruments first before importing instrument groups.");
                logger.LogError($"Failed to find instrument \"{instrumentTicker}\" for instrument group \"{name}, {tag}\"");
              }
            }

        //check if the instrument group is already defined and update it based on the replace behavior
        InstrumentGroup fileInstrumentGroup = new InstrumentGroup(id, attributes, tag, parentId, name, alternateNamesList, description, userId, instrumentTickers);
        InstrumentGroup? definedInstrumentGroup = definedInstrumentGroups.FirstOrDefault(x => x.Equals(fileInstrumentGroup));
        if (definedInstrumentGroup != null)
        {
          id = fileInstrumentGroup.Id = definedInstrumentGroup.Id;  //id is used to import children if required

          switch (importReplaceBehavior)
          {
            case ImportReplaceBehavior.Skip:
              logger.LogWarning($"Skipping - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag}");
              counts.Skipped++;
              break;
            case ImportReplaceBehavior.Replace:
              //replacing name, description, tag and all defined instruments
              logger.LogInformation($"Replacing - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {name}, {description}, {tag}");
              m_instrumentGroupRepository.Update(fileInstrumentGroup);
              counts.Replaced++;
              break;
            case ImportReplaceBehavior.Update:
              //updating name, description, tag and merge in defined alternate names and instruments
              logger.LogInformation($"Updating - {definedInstrumentGroup.Name}, {definedInstrumentGroup.Description}, {definedInstrumentGroup.Tag} => {name}, {description}, {tag}");

              foreach (string alternateName in definedInstrumentGroup.AlternateNames)
              {
                string upperAlternateName = alternateName.ToUpper();
                if (alternateNamesList.FirstOrDefault(x => x.ToUpper() == upperAlternateName) == null) alternateNamesList.Add(alternateName);
              }

              foreach (string instrumentTicker in definedInstrumentGroup.Instruments)
                if (!instrumentTickers.Contains(instrumentTicker)) instrumentTickers.Add(instrumentTicker);
              fileInstrumentGroup.Instruments = instrumentTickers;

              m_instrumentGroupRepository.Update(fileInstrumentGroup);
              counts.Updated++;
              break;
          }
        }
        else
        {
          m_instrumentGroupRepository.Add(new InstrumentGroup(id, attributes, tag, parentId, name, alternateNamesList, description, userId, instrumentTickers));
          counts.Created++;
        }

        //parse the child nodes for this node
        JsonArray? children = node.ContainsKey(tokenJsonChildren) ? node[tokenJsonChildren]!.AsArray() : null;
        if (children != null)
          foreach (JsonObject? child in children!)
            if (child != null) importJsonNode(id, child, importReplaceBehavior, definedInstrumentGroups, definedInstruments, logger, ref counts);
      }
      catch (Exception e)
      {
        if (Debugging.InstrumentGroupImport) m_logger.LogError($"Failed to import instrument group node {name} - {description} - {e.Message}");
        logger.LogError($"Failed to import instrument group node {name} - {description} - {e.Message}");
      }
    }

    private void exportJson(ExportSettings exportSettings)
    {
      int exportCount = 0;
      string statusMessage = $"Exporting instrument groups to \"{exportSettings.Filename}\"";
      if (Debugging.InstrumentGroupExport) m_logger.LogInformation(statusMessage);
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", statusMessage);

      //determine the root node(s) based on whether we have a selected node or not
      //NOTE: For exporting the nodes we adjust the parent Id's to the root node Id which again would be adjusted to the import to the selected node Id or it would be kept the root node Id if
      //      there is no selected Id.
      List<JsonObject> rootNodes = new List<JsonObject>();
      if (SelectedNode != null)
      {
        //when we have a selected node we export from that node on with it's children
        foreach (var instrumentGroupNode in Nodes)
          if (instrumentGroupNode.Item.Id == SelectedNode.Id)
          {
            rootNodes.Add(writeJsonNode(InstrumentGroup.InstrumentGroupRoot, instrumentGroupNode.Item, ref exportCount));
            break;
          }
      }
      else
      {
        //with no selected node we export from all root nodes
        foreach (InstrumentGroup instrumentGroup in Items)
          if (instrumentGroup.ParentId == InstrumentGroup.InstrumentGroupRoot) rootNodes.Add(writeJsonNode(InstrumentGroup.InstrumentGroupRoot, instrumentGroup, ref exportCount));
      }

      using (StreamWriter file = File.CreateText(exportSettings.Filename))   //NOTE: This will always overwrite the text file if it exists.
      {
        int rootNodeCount = rootNodes.Count;
        int rootNodeIndex = 0;
        file.WriteLine("[");
        foreach (JsonObject rootNode in rootNodes)
        {
          rootNodeIndex++;
          file.Write(rootNode.ToJsonString(JsonSerializerOptions.Default));
          if (rootNodeIndex < rootNodeCount - 1) file.WriteLine(",");
        }
        file.WriteLine("");
        file.WriteLine("]");
      }

      if (Debugging.InstrumentGroupExport) m_logger.LogInformation($"Exported {exportCount} groups to \"{exportSettings.Filename}\"");
      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} groups to \"{exportSettings.Filename}\"");
    }

    private JsonObject writeJsonNode(Guid adjustedParentNodeId, InstrumentGroup instrumentGroup, ref int resultCount)
    {
      JsonObject node = new JsonObject
      {
        [tokenJsonId] = instrumentGroup.Id,
        [tokenJsonName] = instrumentGroup.Name,
        [tokenJsonAlternateNames] = new JsonArray(),
        [tokenJsonDescription] = instrumentGroup.Description,
        [tokenJsonUserId] = instrumentGroup.UserId,
        [tokenJsonTag] = instrumentGroup.Tag,
        [tokenJsonAttributes] = ((int)instrumentGroup.AttributeSet).ToString(),   //need to first cast to an integer otherwise it renders the tokens/words of the attribute set
        [tokenJsonInstruments] = new JsonArray(),
        [tokenJsonChildren] = new JsonArray()
      };

      JsonArray alternateNames = node[tokenJsonAlternateNames]!.AsArray();
      foreach (string alternateName in instrumentGroup.AlternateNames) alternateNames.Add(alternateName);

      if (instrumentGroup.Instruments.Count > 0)
      {
        JsonArray instruments = node[tokenJsonInstruments]!.AsArray();
        foreach (string instrumentTicker in instrumentGroup.Instruments)
          instruments.Add(instrumentTicker);
      }

      JsonArray children = node[tokenJsonChildren]!.AsArray();
      foreach (var childInstrumentGroup in Items)
        //NOTE: For the child nodes we keep the "adjusted parent Id" normal for any layers of nodes beyond the selected parent node.
        if (childInstrumentGroup.ParentId == instrumentGroup.Id)
          children.Add(writeJsonNode(instrumentGroup.Id, childInstrumentGroup, ref resultCount));

      resultCount++;

      return node;
    }
  }
}
