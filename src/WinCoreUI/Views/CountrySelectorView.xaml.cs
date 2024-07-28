using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// View that allows the user to select a country.
  /// </summary>
  public sealed partial class CountrySelectorView : Page, IWindowedView
  {
    //constants
    public const int Width = 1621;
    public const int Height = 168;

    //enums


    //types


    //attributes
    private List<CountryInfo> m_countries;
    private ICountryService m_countryService;

    //constructors
    public CountrySelectorView(ViewWindow window)
    {
      m_countryService = (ICountryService)IApplication.Current.Services.GetService(typeof(ICountryService));
      ParentWindow = window;
      this.InitializeComponent();
      setParentProperties();
      
      m_countries = new List<CountryInfo>();
      foreach (var countryKey in CountryInfo.CountryCodes)
      {
        if (m_countryService.Items.FirstOrDefault(x => x.IsoCode == countryKey.IsoCode) == null)
        {
          CountryInfo? country = CountryInfo.GetCountryInfo(countryKey.IsoCode);
          if (country != null) m_countries.Add(country);
        }
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

    public ViewWindow ParentWindow { get; protected set; }
    public UIElement UIElement => this;

    //methods
    private void setParentProperties()
    {
      ParentWindow.View = this;   //need to set this only once the view screen elements are created
      ParentWindow.ResetSizeable();
      ParentWindow.HideMinimizeAndMaximizeButtons();
      ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(Width, Height));
      ParentWindow.CenterWindow();
    }

    private void m_okButton_Click(object sender, RoutedEventArgs e)
    {
      Country country = new Country(Guid.NewGuid(), Country.DefaultAttributes, string.Empty, SelectedCountry?.IsoCode);
      m_countryService.Add(country);
      ParentWindow.Close();
    }

    private void m_cancelButton_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
