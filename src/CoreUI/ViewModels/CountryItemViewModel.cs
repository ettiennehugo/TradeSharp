using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.Common;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Detailed item view for coutry objects.
  /// </summary>
  public class CountryItemViewModel : ItemViewModel<Country>
  {
    //constants


    //enums


    //types


    //attributes
    protected CountryInfo m_countryInfo;

    //constructors
    public CountryItemViewModel(Country item, INavigationService navigationService, IDialogService dialogService) : base(item, navigationService, dialogService) 
    {
      m_countryInfo = CountryInfo.GetCountryInfo(item.IsoCode) ?? throw new ArgumentException($"Failed to get country informtion for country \"{item.IsoCode}\".");
    }

    //finalizers


    //interface implementations


    //properties
    public CountryInfo CountryInfo
    {
      get => m_countryInfo;
      set => SetProperty(ref m_countryInfo, value);
    }

    //methods


  }
}
