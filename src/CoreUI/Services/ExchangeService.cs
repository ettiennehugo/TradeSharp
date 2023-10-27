using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for exchange objects.
  /// </summary>
  public partial class ExchangeService : ObservableObject, IItemsService<Exchange>
  {
    //constants


    //enums


    //types


    //attributes
    private IExchangeRepository m_exchangeRepository;
    private Guid m_parent;
    [ObservableProperty] private Exchange? m_selectedItem;
    [ObservableProperty] private ObservableCollection<Exchange> m_items;


    //constructors
    public ExchangeService(IExchangeRepository exchangeRepository)
    {
      m_parent = Guid.Empty;
      m_exchangeRepository = exchangeRepository;
      m_items = new ObservableCollection<Exchange>();
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId
    {
      get => m_parent;
      set
      {
        if (m_parent != value)
        {
          m_parent = value;
          OnPropertyChanged();
          _ = RefreshAsync();
        }
      }
    }

    public event EventHandler<Exchange>? SelectedItemChanged;

    //methods
    public async Task<Exchange> AddAsync(Exchange item)
    {
      var result = await m_exchangeRepository.AddAsync(item);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<bool> DeleteAsync(Exchange item)
    {
      bool result = await m_exchangeRepository.DeleteAsync(item);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_exchangeRepository.GetItemsAsync();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<Exchange> UpdateAsync(Exchange item)
    {
      return m_exchangeRepository.UpdateAsync(item);
    }

    public Task<Exchange> CopyAsync(Exchange item) => throw new NotImplementedException();

  }
}
