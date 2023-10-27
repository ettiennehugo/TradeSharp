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
  public partial class InstrumentGroupService : ObservableObject, IItemsService<InstrumentGroup>
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentGroupRepository m_instrumentGroupRepository;
    [ObservableProperty] private InstrumentGroup? m_selectedItem;
    [ObservableProperty] private ObservableCollection<InstrumentGroup> m_items;

    //constructors
    public InstrumentGroupService(IInstrumentGroupRepository instrumentGroupRepository)
    {
      m_instrumentGroupRepository = instrumentGroupRepository;
      m_items = new ObservableCollection<InstrumentGroup>();
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    public event EventHandler<InstrumentGroup>? SelectedItemChanged;


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
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<InstrumentGroup> UpdateAsync(InstrumentGroup item)
    {
      return m_instrumentGroupRepository.UpdateAsync(item);
    }
  }
}
