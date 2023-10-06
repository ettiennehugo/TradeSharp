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
using TradeSharp.Data;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Exchange definition view used to create/view/update an exchange.
  /// </summary>
  public sealed partial class ExchangeView : Page
  {

    //constants


    //enums


    //types


    //attributes
    private IDataStoreService m_dataStoreService;

    //constructors
    public ExchangeView()
    {
      loadCountries();
      loadTimeZones();
      this.InitializeComponent();
      m_name.Text = "New Exchange";
      Exchange = new Exchange(Guid.NewGuid(), Guid.Empty, m_name.Text, TimeZoneInfo.Local);
      m_countryId.SelectedIndex = 0; 
    }

    public ExchangeView(Exchange exchange)
    {
      loadCountries();
      loadTimeZones();
      this.InitializeComponent();
      Exchange = exchange;

      m_countryId.SelectedIndex = 0;  //default to first valid country
      for (int i = 0; i < Countries.Count; i++)
        if (Countries[i].Id == Exchange.CountryId)
        {
          m_countryId.SelectedIndex = i;
          break;
        }

      m_timeZone.SelectedItem = 0;
      for (int i = 0; i < TimeZones.Count; i++)
        if (TimeZones[i] == Exchange.TimeZone)
        {
          m_timeZone.SelectedItem = i;
          break;
        }
    }

    //finalizers


    //interface implementations


    //properties
    public IList<Country> Countries { get; internal set; }
    public IList<TimeZoneInfo> TimeZones { get; internal set; }

    public static readonly DependencyProperty s_exchangeProperty = DependencyProperty.Register("Exchange", typeof(Exchange), typeof(ExchangeView), new PropertyMetadata(null));
    public Exchange? Exchange
    {
      get => (Exchange?)GetValue(s_exchangeProperty);
      set => SetValue(s_exchangeProperty, value);
    }

    //methods
    private void loadCountries()
    {
      //load defined countries
      IDataStoreService dataStoreService = Ioc.Default.GetRequiredService<IDataStoreService>();
      Countries = dataStoreService.GetCountries();
    }

    private void loadTimeZones()
    {
      TimeZones = new List<TimeZoneInfo>();
      foreach (TimeZoneInfo timeZoneInfo in TimeZoneInfo.GetSystemTimeZones()) TimeZones.Add(timeZoneInfo);
    }

    private void m_countryId_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      Exchange.CountryId = ((Country)m_countryId.SelectedItem).Id;
    }

    private void m_timeZone_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      Exchange.TimeZone = (TimeZoneInfo)m_timeZone.SelectedItem;
    }
  }
}
