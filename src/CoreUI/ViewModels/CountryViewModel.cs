using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.Input;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for a list of countries with their details using the associated item view model.
  /// </summary>
  public class CountryViewModel : ListViewModel<Country>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public CountryViewModel(IItemsService<Country> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService) 
    {
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Editable));
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null && SelectedItem.HasAttribute(Attributes.Deletable));
    }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      CountryInfo? country = await m_dialogService.ShowSelectCountryAsync();
      if (country != null)
      {
        var newCountry = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, country.RegionInfo.TwoLetterISORegionName);
        if (m_itemsService.Items.Contains(newCountry))
          await m_dialogService.ShowPopupMessageAsync("The country you are trying to add already exists in the database.");
        else
        {
          await m_itemsService.AddAsync(newCountry);
          SelectedItem = newCountry;
          Items.Add(newCountry);
          await OnRefreshAsync();
        }
      }
    }

    public override void OnUpdate()
    {
      throw new NotImplementedException("Update not supported for countries.");
    }

    //properties


    //methods

  }
}
