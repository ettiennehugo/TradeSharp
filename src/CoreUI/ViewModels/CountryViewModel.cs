using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of countries with their details using the associated item view model.
  /// </summary>
  public class CountryViewModel : MasterDetailViewModel<CountryItemViewModel, Country>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public CountryViewModel(IItemsService<Country> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) { }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      CountryInfo? country = await m_dialogService.ShowSelectCountryAsync();
      if (country != null)
      {
        var newCountry = new Country(Guid.NewGuid(), country.RegionInfo.ThreeLetterISORegionName);
        if (m_itemsService.Items.Contains(newCountry))
          await m_dialogService.ShowPopupMessageAsync("The country you are trying to add already exists in the database.");
        else
        {
          await m_itemsService.AddAsync(newCountry);
          SelectedItem = newCountry;
          Items.Add(newCountry);
        }
      }
    }

    public override void OnUpdate()
    {
      throw new NotImplementedException("Update not supported for countries.");
    }

    public override void OnDelete()
    {
      if (SelectedItem != null)
      {
        var item = SelectedItem;
        Items.Remove(SelectedItem);
        m_itemsService.DeleteAsync(item);
        SelectedItem = Items.FirstOrDefault();
      }
    }

    protected override CountryItemViewModel ToViewModel(Country item)
    {
      return new CountryItemViewModel(item, m_navigationService, m_dialogService);
    }

    //properties


    //methods

  }
}
