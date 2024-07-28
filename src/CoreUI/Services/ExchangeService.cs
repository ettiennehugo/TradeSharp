using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for exchange objects.
  /// </summary>
  public partial class ExchangeService : ServiceBase, IExchangeService
  {
    //constants


    //enums


    //types


    //attributes
    private IExchangeRepository m_exchangeRepository;
    private Guid m_parent;
    private Exchange? m_selectedItem;

    //constructors
    public ExchangeService(IExchangeRepository exchangeRepository, IDialogService dialogService): base(dialogService)
    {
      m_parent = Guid.Empty;
      m_exchangeRepository = exchangeRepository;
      m_selectedItem = null;
      Items = new ObservableCollection<Exchange>();
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
          OnPropertyChanged(PropertyName.ParentId);
          Refresh();
        }
      }
    }

    public event EventHandler<Exchange?>? SelectedItemChanged;
    public Exchange? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, m_selectedItem); }
    }

    public IList<Exchange> Items { get; set; }

    //methods
    public bool Add(Exchange item)
    {
      var result = m_exchangeRepository.Add(item);
      if (result)
      {
        Items.Add(item);
        SelectedItem = item;
        SelectedItemChanged?.Invoke(this, SelectedItem);
      }
      return result;
    }

    public bool Delete(Exchange item)
    {
      var result = m_exchangeRepository.Delete(item);
      if (result) Items.Remove(item);
      return result;
    }

    public void Refresh()
    {
      LoadedState = Common.LoadedState.Loading;
      var result = m_exchangeRepository.GetItems();
      Items.Clear();
      foreach (var item in result) Items.Add(item);
      LoadedState = Common.LoadedState.Loaded;
      raiseRefreshEvent();
    }

    public bool Update(Exchange item)
    {
      var result = m_exchangeRepository.Update(item);
      if (result)
      {
        var existingItem = Items.FirstOrDefault((i) => i.Id == item.Id);
        if (existingItem != null)
          existingItem.Update(item);
        else
          Items.Add(item);
      }
      return result;
    }

    public bool Copy(Exchange item) => throw new NotImplementedException();
  }
}
