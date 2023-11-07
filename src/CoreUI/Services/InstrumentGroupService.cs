using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using CsvHelper;
using System.Globalization;

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
    private const string tokenName = "name";
    private const string tokenDescription = "description";
    private const string tokenTag = "tag";
    private const string tokenTicker = "ticker";

    //enums


    //types
    private class InstrumentGroupRecord
    {
      public InstrumentGroupRecord(Guid parentId, Guid id, string name, string description, string tag, string ticker)
      {
        ParentId = parentId;
        Id = id;
        Name = name;
        Description = description;
        Tag = tag;
        Ticker = ticker;
      }

      public Guid ParentId;
      public Guid Id;
      public string Name;
      public string Description;
      public string Tag;
      public string Ticker;
    }

    //attributes
    private IDataStoreService m_dataStoreService;
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    [ObservableProperty] private ITreeNodeType<Guid, InstrumentGroup>? m_selectedNode;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_selectedNodes;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_nodes;
    [ObservableProperty] private ObservableCollection<InstrumentGroup> m_items;

    //constructors
    public InstrumentGroupService(IDataStoreService dataStoreService, IInstrumentGroupRepository instrumentGroupRepository)
    {
      m_dataStoreService = dataStoreService;
      m_instrumentGroupRepository = instrumentGroupRepository;
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

    public async Task<int> ImportAsync(string filename, ImportReplaceBehavior importReplaceBehavior)
    {
      int result = 0;
      string extension = Path.GetExtension(filename).ToLower();

      if (extension == extensionCSV)
        result = await importCSV(filename, importReplaceBehavior);
      else if (extension == extensionJSON)
        result = importJSON(filename, importReplaceBehavior);
      return result;
    }

    public Task<int> ExportAsync(string filename)
    {
      int result = 0;
      string extension = Path.GetExtension(filename).ToLower();
      if (extension == extensionCSV)
        result = exportCSV(filename);
      else if (extension == extensionJSON)
        result = exportJSON(filename);
      return Task.FromResult(result);
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
    private async Task<int> importCSV(string filename, ImportReplaceBehavior importReplaceBehavior)
    {
      int result = 0;


      //TODO: Need to build in a logger - https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line


      Dictionary<string, InstrumentGroup> definedInstrumentGroups = new Dictionary<string, InstrumentGroup>();
      Dictionary<string, InstrumentGroupRecord> fileInstrumentGroups = new Dictionary<string, InstrumentGroupRecord>();
      List<InstrumentGroupRecord> currentLineRecords = new List<InstrumentGroupRecord>();

      using (var reader = new StreamReader(filename))
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

            lineNo++;

            for (int columnIndex = csv.HeaderRecord.Count() -1; columnIndex >= 0; columnIndex--)
            {
              string? columnValue = null;
              if (csv.TryGetField(columnIndex, out columnValue))
              {
                if (csv.HeaderRecord[columnIndex].ToLower() == tokenName)
                {
                  name = columnValue!;
                  if (description == "") description = name;
                  if (tag == "") tag = name;

                  //create new group to be added for this line
                  currentLineRecords.Add(new InstrumentGroupRecord(InstrumentGroup.InstrumentGroupRoot, Guid.NewGuid(), name, description, tag, ticker));
                  
                  //reset field values
                  name = "";
                  description = "";
                  tag = "";
                  ticker = "";
                }
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenDescription)
                  description = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenTag)
                  tag = columnValue!;
                else if (csv.HeaderRecord[columnIndex].ToLower() == tokenTicker)
                  ticker = columnValue!;

                //resolve the parent id's and add the objects as required - we use the tag as the key since it should be a unique key value used for the instrument groups
                if (columnIndex == 0)
                {
                  Guid previousId = Guid.Empty;
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
                      instrumentGroupRecord.ParentId = previousId != Guid.Empty ? previousId : InstrumentGroup.InstrumentGroupRoot;
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
                switch (importReplaceBehavior)
                {
                  case ImportReplaceBehavior.Skip:
                    
                    //TODO: Log skip message.

                    break;
                  case ImportReplaceBehavior.Replace:
                    //will update the name, description and tag and remove instrument associations
                    await m_instrumentGroupRepository.UpdateAsync(new InstrumentGroup(definedInstrumentGroup.Id, definedInstrumentGroup.AttributeSet, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, new List<Guid>()));
                    result++;
                    break;
                  case ImportReplaceBehavior.Update:
                    //will update the name and description and keep all the associated instruments                  
                    await m_instrumentGroupRepository.UpdateAsync(new InstrumentGroup(definedInstrumentGroup.Id, definedInstrumentGroup.AttributeSet, fileInstrumentGroup.Value.Tag, definedInstrumentGroup.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, definedInstrumentGroup.Instruments));                    
                    result++;
                    break;
                }
              }
              else if (fileInstrumentGroup.Value.Ticker.Length == 0)
              {                
                await m_instrumentGroupRepository.AddAsync(new InstrumentGroup(fileInstrumentGroup.Value.Id, InstrumentGroup.DefaultAttributeSet, fileInstrumentGroup.Value.Tag, fileInstrumentGroup.Value.ParentId, fileInstrumentGroup.Value.Name, fileInstrumentGroup.Value.Description, new List<Guid>())); 
                result++;
              }
            }

            //create/update any instrument associations based on tickers given
            IList<Instrument> instruments = m_dataStoreService.GetInstruments();
            foreach (KeyValuePair<string, InstrumentGroupRecord> fileInstrumentGroup in fileInstrumentGroups)
              if (fileInstrumentGroup.Value.Ticker.Length != 0)
              {
                Instrument? instrument = instruments.FirstOrDefault(x => x.Ticker.ToLower() == fileInstrumentGroup.Value.Ticker.ToLower());
                if (instrument != null)
                {
                  m_dataStoreService.CreateInstrumentGroupInstrument(fileInstrumentGroup.Value.Id, instrument.Id);
                }
                else
                {
                  
                  //TODO: Log error that instrument with ticker was not found.
                  
                }
              }
          }
        }
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
    private int exportCSV(string filename)
    {
      int result = 0;


      throw new NotImplementedException();


      return result;
    }

    /// <summary>
    /// Imports the instrument group definitions from a given file, format must be the following:
    ///   {
    ///     "name" : "Instrument Group Name",
    ///     "description" : "Instrument Group Description",
    ///     "tag" : "Instrument Group Tag Value",
    ///     "children" : [
    ///       
    ///       /// records similar in format with name, description, tag, children (tag is optional)
    /// 
    ///     ],
    ///     "instruments" : [ ticker1, ... , tickerN ]
    ///   }
    /// </summary>
    private int importJSON(string filename, ImportReplaceBehavior importReplaceBehavior)
    {

      return 0;

    }

    private int exportJSON(string filename)
    {

      return 0;

    }
  }
}
