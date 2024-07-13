using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// View that allows the user to select a country.
  /// </summary>
  public sealed partial class CountrySelectorView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private List<CountryInfo> m_countries;

    //constructors
    public CountrySelectorView()
    {
      this.InitializeComponent();
      m_countries = new List<CountryInfo>();
      foreach (var countryKey in CountryInfo.CountryCodes)
      {
        CountryInfo? country = CountryInfo.GetCountryInfo(countryKey.IsoCode);
        if (country != null) m_countries.Add(country);
      }
      SelectedCountry = null;
      if (m_countries.Count() > 0) SelectedCountry = m_countries.First();
    }

    //finalizers


    //interface implementations


    //properties
    //https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/dependency-properties-overview
    public static readonly DependencyProperty s_selectedCountryProperty = DependencyProperty.Register("SelectedItem", typeof(CountryInfo), typeof(CountrySelectorView), new PropertyMetadata(null));
    public IEnumerable<CountryInfo> Countries { get => m_countries; }
    public CountryInfo? SelectedCountry
    {
      get => (CountryInfo?)GetValue(s_selectedCountryProperty);
      set => SetValue(s_selectedCountryProperty, value);
    }

    //methods


  }
}
