using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Observable service class for country objects.
  /// </summary>
  public partial class CountryService : ServiceBase, ICountryService
  {
    //constants


    //enums


    //types


    //attributes
    private ICountryRepository m_countryRepository;
    private Country? m_selectedItem;

    //constructors
    public CountryService(ICountryRepository countryRepository, IDialogService dialogService): base(dialogService)
    {
      m_countryRepository = countryRepository;
      m_selectedItem = null;
      Items = new ObservableCollection<Country>();
    }

    //finalizers


    //interface implementations
    public bool Add(Country item)
    {
      var result = m_countryRepository.Add(item);
      Items.Add(item);
      SelectedItem = item;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public bool Delete(Country item)
    {
      bool result = m_countryRepository.Delete(item);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }
      return result;
    }

    public void Refresh()
    {
      var result = m_countryRepository.GetItems();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public bool Update(Country item)
    {
      var result = m_countryRepository.Update(item);
      SelectedItem = item;
      return result;
    }

    public bool Copy(Country item) => throw new NotImplementedException();

    //properties
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } } //countries to not have a parent

    public event EventHandler<Country?>? SelectedItemChanged;
    public Country? SelectedItem
    {
      get => m_selectedItem;
      set { SetProperty(ref m_selectedItem, value); SelectedItemChanged?.Invoke(this, value); }
    }

    public IList<Country> Items { get; set; }

    //methods

  }
}
