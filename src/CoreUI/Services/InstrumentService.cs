using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;

namespace TradeSharp.CoreUI.Services
{
  public partial class InstrumentService : ObservableObject, IListItemsService<Instrument>
  {
    //constants


    //enums


    //types


    //attributes
    private IInstrumentRepository m_instrumentRepository;
    [ObservableProperty] private Instrument? m_selectedItem;
    [ObservableProperty] private ObservableCollection<Instrument> m_items;

    //constructors
    public InstrumentService(IInstrumentRepository instrumentRepository)
    {
      m_instrumentRepository = instrumentRepository;
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

    public Task<int> ImportAsync(string filename, ImportReplaceBehavior importReplaceBehavior)
    {
      return Task.FromResult<int>(0);
    }

    public Task<int> ExportAsync(string filename)
    {
      return Task.FromResult<int>(0);
    }

    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } }
    public event EventHandler<Instrument>? SelectedItemChanged;

    //methods



  }
}
