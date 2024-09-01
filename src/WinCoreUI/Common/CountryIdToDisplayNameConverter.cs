using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Country Id to Diplay name converter.
  /// </summary>
  class CountryIdToDisplayNameConverter : IValueConverter
  {
    //constants


    //enums


    //types


    //attributes
    private IDatabase m_database;

    //constructors
    public CountryIdToDisplayNameConverter()
    {
      m_database = (IDatabase)IApplication.Current.Services.GetService(typeof(IDatabase));
    }


    //finalizers


    //interface implementations
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      string result = "<No country>";

      if (value is Guid && (Guid)value != Guid.Empty)
      {
        Country? country = m_database.GetCountry((Guid)value);
        result = country != null ? $"{country.CountryInfo.RegionInfo.Name} - {country.CountryInfo.RegionInfo.EnglishName}" : "<Country definition not found>";
      }

      return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      string displayName = value.ToString();

      IList<Country> countries = m_database.GetCountries();
      Country? country = countries.FirstOrDefault<Country>(x => x.CountryInfo.RegionInfo.DisplayName.Contains(displayName));

      return country?.Id ?? Guid.Empty;
    }

    //properties


    //methods


  }
}
