using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for instrument group objects.
  /// </summary>
  public partial class InstrumentGroupService : ObservableObject, ITreeItemsService<Guid, InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    [ObservableProperty] private ITreeNodeType<Guid, InstrumentGroup>? m_selectedNode;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_selectedNodes;
    [ObservableProperty] private ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> m_nodes;
    [ObservableProperty] private ObservableCollection<InstrumentGroup> m_items;

    //constructors
    public InstrumentGroupService(IInstrumentGroupRepository instrumentGroupRepository)
    {
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

    public Task<int> ImportAsync(string filename, ImportReplaceBehavior importReplaceBehavior)
    {
      return Task.FromResult<int>(0);
    }

    public Task<int> ExportAsync(string filename)
    {
      return Task.FromResult<int>(0);
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
  }
}
