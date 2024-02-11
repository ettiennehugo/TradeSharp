using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using TradeSharp.CoreUI.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class ExportView : Page
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExportView(bool showDateTimeTimeZone, bool showReplaceBehavior)
    {
      ExportSettings = new ExportSettings();
      this.InitializeComponent();
      if (!showDateTimeTimeZone) m_layoutGrid.RowDefinitions[1].Height = new GridLength(0); //hide date/time timezone selector
      if (!showReplaceBehavior) m_layoutGrid.RowDefinitions[2].Height = new GridLength(0); //hide replace behavior selector
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_exportSettingsProperty = DependencyProperty.Register("ExportSettings", typeof(ExportSettings), typeof(ExportView), new PropertyMetadata(null));
    public ExportSettings ExportSettings
    {
      get => (ExportSettings)GetValue(s_exportSettingsProperty);
      set => SetValue(s_exportSettingsProperty, (ExportSettings)value);
    }



    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private async void m_browse_Click(object sender, RoutedEventArgs e)
    {
      //https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/filepicker/
      FileSavePicker savePicker = new FileSavePicker();
      savePicker.DefaultFileExtension = ".csv";
      savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
      savePicker.FileTypeChoices.Add("CSV", new List<string>() { ".csv" }); //default to CSV as it is more compact and faster to write
      savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(savePicker, hwnd);

      StorageFile file = await savePicker.PickSaveFileAsync();
      if (file != null) ExportSettings.Filename = file.Path;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportExportDataDateTimeTimeZone));
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_replaceBehavior, typeof(ExportReplaceBehavior));
    }
  }
}
