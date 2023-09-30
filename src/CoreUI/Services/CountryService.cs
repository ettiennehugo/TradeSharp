using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using System.Security.Principal;

namespace TradeSharp.CoreUI.Services
{
    /// <summary>
    /// Observable service class for country objects.
    /// </summary>
    public partial class CountryService : ObservableObject, IItemsService<Country>
  {
    //constants


    //enums


    //types


    //attributes
    private ICountryRepository m_countryRepository;
    [ObservableProperty] private Country? m_selectedItem;
    [ObservableProperty] private ObservableCollection<Country> m_items;

    //constructors
    public CountryService(ICountryRepository countryRepository)
    {
      m_countryRepository = countryRepository;
      m_items = new ObservableCollection<Country>();
    }

    //finalizers


    //interface implementations
    public async Task<Country> AddAsync(Country item)
    {
      var result = await m_countryRepository.AddAsync(item);
      SelectedItem = result;
      SelectedItemChanged?.Invoke(this, SelectedItem);
      return result;
    }

    public async Task<bool> DeleteAsync(Country item)
    {
      bool result = await m_countryRepository.DeleteAsync(item.Id);
      if (item == SelectedItem)
      {
        SelectedItemChanged?.Invoke(this, SelectedItem);
        SelectedItem = null;
      }
      return result;
    }

    public async Task RefreshAsync()
    {
      var result = await m_countryRepository.GetItemsAsync();
      Items.Clear();
      SelectedItem = result.FirstOrDefault(); //need to populate selected item first otherwise collection changes fire off UI changes with SelectedItem null
      foreach (var item in result) Items.Add(item);
      if (SelectedItem != null) SelectedItemChanged?.Invoke(this, SelectedItem);
    }

    public Task<Country> UpdateAsync(Country item) => m_countryRepository.UpdateAsync(item);

    //properties
    public event EventHandler<Country>? SelectedItemChanged;
    public Guid ParentId { get => Guid.Empty; set { /* nothing to do */ } } //countries to not have a parent

    //methods

  }
}
