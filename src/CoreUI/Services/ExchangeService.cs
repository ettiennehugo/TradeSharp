using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Repositories;
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
          OnPropertyChanged();
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
      Items.Add(item);
      SelectedItem = item;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Delete(Exchange item)
    {
      return m_exchangeRepository.Delete(item); ;
    }

    public void Refresh()
    {
      var result = m_exchangeRepository.GetItems();
      Items.Clear();
      foreach (var item in result) Items.Add(item);
    }

    public bool Update(Exchange item)
    {
      return m_exchangeRepository.Update(item);
    }

    public bool Copy(Exchange item) => throw new NotImplementedException();
  }
}
