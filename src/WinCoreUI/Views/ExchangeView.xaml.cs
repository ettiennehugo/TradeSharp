using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage.Pickers;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.Storage;
using System.ComponentModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Exchange definition view used to create/view/update an exchange.
  /// </summary>
  [INotifyPropertyChanged]
  public sealed partial class ExchangeView : Page, IWindowedView
  {
    //constants
    public const int Width = 1198;
    public const int Height = 660;

    //enums


    //types


    //attributes
    private IExchangeService m_exchangeService;

    //constructors
    public ExchangeView(ViewWindow parent)
    { 
      ParentWindow = parent;
      m_exchangeService = (IExchangeService)IApplication.Current.Services.GetService(typeof(IExchangeService));
      loadCountries();
      loadTimeZones();
      Exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributes, "", Guid.Empty, "", Array.Empty<string>(), TimeZoneInfo.Local, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);
      ExchangeLogoPath = Data.Exchange.GetLogoPath(Guid.Empty);
      this.InitializeComponent();
      setParentProperties();
      m_countryId.SelectedIndex = 0; 
    }

    public ExchangeView(Exchange exchange, ViewWindow parent)
    {
      ParentWindow = parent;
      m_exchangeService = (IExchangeService)IApplication.Current.Services.GetService(typeof(IExchangeService));
      loadCountries();
      loadTimeZones();
      Exchange = exchange;
      ExchangeLogoPath = Exchange.LogoPath;
      this.InitializeComponent();
      setParentProperties();

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
    public ViewWindow ParentWindow { get; private set; }
    public UIElement UIElement => this;

    [ObservableProperty] private Exchange? m_exchange;
    [ObservableProperty] private string m_exchangeLogoPath;

    //methods
    private void setParentProperties()
    {
      ParentWindow.View = this;   //need to set this only once the view screen elements are created
      ParentWindow.ResetSizeable();
      ParentWindow.HideMinimizeAndMaximizeButtons();
      ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(Width, Height));
      ParentWindow.CenterWindow();
    }

    private void loadCountries()
    {
      //load defined countries
      IDatabase database = (IDatabase)IApplication.Current.Services.GetService(typeof(IDatabase));
      Countries = database.GetCountries();
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

    private void m_okButton_Click(object sender, RoutedEventArgs e)
    {
      m_exchangeService.Update(Exchange);
      ParentWindow.Close();
    }

    private void m_cancelButton_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
