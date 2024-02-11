using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TradeSharp.CoreUI.Services;
using WinRT.Interop;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class ImportView : Page
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public ImportView(bool showDateTimeTimeZone, bool showReplaceBehavior)
    {
      ImportSettings = new ImportSettings();
      this.InitializeComponent();
      if (!showDateTimeTimeZone) m_layoutGrid.RowDefinitions[1].Height = new GridLength(0); //hide date/time timezone selector
      if (!showReplaceBehavior) m_layoutGrid.RowDefinitions[2].Height = new GridLength(0); //hide replace behavior selector
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_importSettingsProperty = DependencyProperty.Register("ImportSettings", typeof(ImportSettings), typeof(ImportView), new PropertyMetadata(null));
    public ImportSettings ImportSettings
    {
      get => (ImportSettings)GetValue(s_importSettingsProperty);
      set => SetValue(s_importSettingsProperty, (ImportSettings)value);
    }

    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private async void m_browse_Click(object sender, RoutedEventArgs e)
    {
      //https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/filepicker/
      FileOpenPicker openPicker = new FileOpenPicker();
      openPicker.ViewMode = PickerViewMode.Thumbnail;
      openPicker.SuggestedStartLocation = PickerLocationId.Downloads;
      openPicker.FileTypeFilter.Add(".json");
      openPicker.FileTypeFilter.Add(".csv");

      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(openPicker, hwnd);

      StorageFile file = await openPicker.PickSingleFileAsync();
      if (file != null) ImportSettings.Filename = file.Path;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportExportDataDateTimeTimeZone));
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_replaceBehavior, typeof(ImportReplaceBehavior));
    }


  }
}
