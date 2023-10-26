using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TradeSharp.Common;
using TradeSharp.CoreUI.Repositories;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;
using Windows.Media.Capture.Frames;

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
        CountryInfo? country = CountryInfo.GetCountryInfo(countryKey.Item1);
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
