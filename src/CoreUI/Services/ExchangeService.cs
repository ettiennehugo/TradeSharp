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
  public partial class ExchangeService : ObservableObject, IListItemsService<Exchange>
  {
    //constants


    //enums


    //types


    //attributes
    private IExchangeRepository m_exchangeRepository;
    private Guid m_parent;
    [ObservableProperty] private Exchange? m_selectedItem;
    [ObservableProperty] private ObservableCollection<Exchange> m_items;
    [ObservableProperty] private string m_statusMessage;
    [ObservableProperty] private double m_statusProgress;

    //constructors
    public ExchangeService(IExchangeRepository exchangeRepository)
    {
      m_parent = Guid.Empty;
      m_exchangeRepository = exchangeRepository;
      m_items = new ObservableCollection<Exchange>();
      m_statusMessage = "";
      m_statusProgress = 0;
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
      return result;
    }

    public async Task<bool> DeleteAsync(Exchange item)
    {
      bool result = await m_exchangeRepository.DeleteAsync(item);
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_exchangeRepository.GetItemsAsync();
      Items.Clear();
      foreach (var item in result) Items.Add(item);
    }

    public Task<Exchange> UpdateAsync(Exchange item)
    {
      return m_exchangeRepository.UpdateAsync(item);
    }

    public Task<Exchange> CopyAsync(Exchange item) => throw new NotImplementedException();
    public Task<ImportReplaceResult> ImportAsync(ImportSettings importSettings) => throw new NotImplementedException();
    public Task<long> ExportAsync(string filename) => throw new NotImplementedException();
  }
}
