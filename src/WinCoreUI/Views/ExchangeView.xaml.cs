using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.Storage.Pickers;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Storage;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Exchange definition view used to create/view/update an exchange.
  /// </summary>
  [INotifyPropertyChanged]
  public sealed partial class ExchangeView : Page
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExchangeView()
    {
      loadCountries();
      loadTimeZones();
      Exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "", Guid.Empty, "", TimeZoneInfo.Local, Guid.Empty);
      ExchangeLogoPath = Data.Exchange.GetLogoPath(Guid.Empty);
      this.InitializeComponent();
      m_countryId.SelectedIndex = 0; 
    }

    public ExchangeView(Exchange exchange)
    {
      loadCountries();
      loadTimeZones();
      Exchange = exchange;
      ExchangeLogoPath = Exchange.LogoPath;
      this.InitializeComponent();

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
    [ObservableProperty] private Exchange? m_exchange;
    [ObservableProperty] private string m_exchangeLogoPath;

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

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private async void m_logo_Click(object sender, RoutedEventArgs e)
    {
      //https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/filepicker/
      FileOpenPicker openPicker = new FileOpenPicker();
      openPicker.ViewMode = PickerViewMode.Thumbnail;
      openPicker.SuggestedStartLocation = PickerLocationId.Downloads;
      openPicker.FileTypeFilter.Add(".jpg");
      openPicker.FileTypeFilter.Add(".jpeg");
      openPicker.FileTypeFilter.Add(".png");

      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(openPicker, hwnd);

      StorageFile file = await openPicker.PickSingleFileAsync();
      if (file != null) ExchangeLogoPath = file.Path;
    }

  }
}
