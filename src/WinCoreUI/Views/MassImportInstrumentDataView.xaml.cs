using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using TradeSharp.Common;
using TradeSharp.CoreUI.Commands;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Common;
using System.Runtime.InteropServices;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Mass import of instrument data for a given DataProvider.
  /// </summary>
  public sealed partial class MassImportInstrumentDataView : Page
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public MassImportInstrumentDataView()
    {
      Settings = new MassImportSettings();
      ImportStructureTooltip = "";
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public string DefaultStartDateTime { get => Constants.DefaultMinimumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public string DefaultEndDateTime { get => Constants.DefaultMaximumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public MassImportSettings Settings { get; internal set; }
    public int ThreadCountMax { get => Environment.ProcessorCount; }
    public string ImportStructureTooltip { get; internal set; }
    public Window ParentWindow { get; set; }
    public string DataProvider { get; set; }

    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportExportDataDateTimeTimeZone));
      Common.Utilities.populateComboBoxFromEnum(ref m_importStructure, typeof(MassImportExportStructure));
      Common.Utilities.populateComboBoxFromEnum(ref m_fileType, typeof(ImportExportFileTypes));
    }

    private async void m_inputDirectoryBtn_Click(object sender, RoutedEventArgs e)
    {
      var folderPicker = new FolderPicker();
      folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
      folderPicker.FileTypeFilter.Add("*");
      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(folderPicker, hwnd);

      Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
      if (folder != null)
      {
        m_inputDirectory.Text = folder.Path;
        Settings.Directory = folder.Path;
      }
    }

    private void m_importStructure_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      switch ((MassImportExportStructure)m_importStructure.SelectedIndex)
      {
        case MassImportExportStructure.DiretoriesAndFiles:
          ImportStructureTooltip = "Import from timeframe directies and files, e.g. OutputDir\\Timeframe\\Ticker.ext";
          break;
        case MassImportExportStructure.FilesOnly:
          ImportStructureTooltip = "Import from files only, e.g. OutputDir\\Ticker_Timeframe.ext";
          break;
      }
    }

    private bool enableImportButton()
    {
      return m_startDateTime != null && DateTime.TryParse(m_startDateTime.Text, out DateTime startDateTime) && DateTime.TryParse(m_endDateTime.Text, out DateTime endDateTime) && startDateTime < endDateTime && m_inputDirectory.Text.Length > 0 &&
        ((bool)m_resolutionMinute.IsChecked || (bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_importBtn_Click(object sender, RoutedEventArgs e)
    {
      IMassImportInstrumentData massImportInstrumentData = new MassImportInstrumentData();
      IMassImportInstrumentData.Context context = new IMassImportInstrumentData.Context();
      context.DataProvider = DataProvider;
      context.Settings = Settings;
      IDialogService dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IProgressDialog progressDialog = dialogService.CreateProgressDialog("Mass Import Progress", null);
      massImportInstrumentData.StartAsync(progressDialog, context);
      ParentWindow.Close();
    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }

    private void m_startDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_startDateTime.Text, out DateTime result)) Settings.FromDateTime = result;
      m_importBtn.IsEnabled = enableImportButton();
    }

    private void m_endDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_endDateTime.Text, out DateTime result)) Settings.ToDateTime = result;
      m_importBtn.IsEnabled = enableImportButton();
    }

    private void m_inputDirectory_TextChanged(object sender, TextChangedEventArgs e)
    {
      m_importBtn.IsEnabled = enableImportButton();
    }

    private void m_resolutionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      m_importBtn.IsEnabled = enableImportButton();
    }

    private void m_resolutionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      m_importBtn.IsEnabled = enableImportButton();
    }
  }
}
