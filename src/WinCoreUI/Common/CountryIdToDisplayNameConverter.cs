using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.DependencyInjection;

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
      m_database = Ioc.Default.GetRequiredService<IDatabase>();
    }


    //finalizers


    //interface implementations
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      string result = "<No country>";

      if (value is Guid && (Guid)value != Guid.Empty) 
      {
        Country? country = m_database.GetCountry((Guid)value);
        result = country?.CountryInfo.CultureInfo.DisplayName ?? "<No country>";
      }

      return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      string displayName = value.ToString();

      IList<Country> countries = m_database.GetCountries();
      Country? country = countries.FirstOrDefault<Country>(x => x.CountryInfo.CultureInfo.DisplayName == displayName);

      return country?.Id ?? Guid.Empty;
    }

    //properties


    //methods


  }
}
