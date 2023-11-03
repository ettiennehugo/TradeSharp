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


//https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/ControlPages/TreeViewPage.xaml.cs

  /// <summary>
  /// Tree node item used to decorate an instrument group or instrument in a hierarchical fashion.
  /// </summary>
  public partial class InstrumentGroupServiceNode : ObservableObject
  {
    //constants


    //enums


    //types


    //attributes
    private InstrumentGroupService m_instrumentGroupService;
    [ObservableProperty] private Instrument? m_instrument;
    [ObservableProperty] private InstrumentGroup? m_instrumentGroup;
    [ObservableProperty] private Guid m_id;
    [ObservableProperty] private string m_name;
    [ObservableProperty] private string m_description;
    [ObservableProperty] private ObservableCollection<InstrumentGroupServiceNode> m_children;

    //constructors
    public InstrumentGroupServiceNode(InstrumentGroupService service, object value)
    {
      m_instrumentGroupService = service;
      m_children = new ObservableCollection<InstrumentGroupServiceNode>();

      if (value is InstrumentGroup)
      {
        m_instrumentGroup = (InstrumentGroup)value;
        m_id = m_instrumentGroup.Id;
        m_name = m_instrumentGroup.Name;
        m_description = m_instrumentGroup.Description;

        foreach (InstrumentGroup instrumentGroup in m_instrumentGroupService.Items)
          if (instrumentGroup.ParentId == m_id) m_children.Add(new InstrumentGroupServiceNode(m_instrumentGroupService, instrumentGroup));
      }
      else
      {
        m_instrument = (Instrument)value;
        m_id = m_instrument.Id;
        m_name = m_instrument.Name;
        m_description = m_instrument.Description;
      }
    }

    //finalizers


    //interface implementations


    //properties


    //methods


  }

  /// <summary>
  /// Observable service class for instrument group objects.
  /// </summary>
  public partial class InstrumentGroupService : ObservableObject, IItemsService<InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    [ObservableProperty] private InstrumentGroup? m_selectedItem;
    [ObservableProperty] private ObservableCollection<InstrumentGroup> m_items;
    [ObservableProperty] private InstrumentGroupServiceNode? m_selectedNode;
    [ObservableProperty] private ObservableCollection<InstrumentGroupServiceNode> m_nodes;

    //constructors
    public InstrumentGroupService(IInstrumentGroupRepository instrumentGroupRepository)
    {
      m_instrumentGroupRepository = instrumentGroupRepository;
      m_items = new ObservableCollection<InstrumentGroup>();
      m_nodes = new ObservableCollection<InstrumentGroupServiceNode>();
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    public event EventHandler<InstrumentGroup>? SelectedItemChanged;
    public event EventHandler<InstrumentGroupServiceNode>? SelectedNodeChanged;

    //methods
    public async Task<InstrumentGroup> AddAsync(InstrumentGroup item)
    {
      var result = await m_instrumentGroupRepository.AddAsync(item);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<InstrumentGroup> CopyAsync(InstrumentGroup item)
    {
      InstrumentGroup clone = (InstrumentGroup)item.Clone();
      clone.Id = Guid.NewGuid();
      var result = await m_instrumentGroupRepository.AddAsync(clone);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<bool> DeleteAsync(InstrumentGroup item)
    {
      bool result = await m_instrumentGroupRepository.DeleteAsync(item);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_instrumentGroupRepository.GetItemsAsync();
      Items.Clear();
      Nodes.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result)
      {
        Items.Add(item);
        if (item.ParentId == InstrumentGroup.InstrumentGroupRoot) Nodes.Add(new InstrumentGroupServiceNode(this, item));  //add the set of root item nodes
      }
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<InstrumentGroup> UpdateAsync(InstrumentGroup item)
    {
      return m_instrumentGroupRepository.UpdateAsync(item);
    }

    public Task<int> ImportAsync(string filename, ImportReplaceBehavior importReplaceBehavior)
    {
      return Task.FromResult<int>(0);
    }

    public Task<int> ExportAsync(string filename)
    {
      return Task.FromResult<int>(0);
    }
  }
}
