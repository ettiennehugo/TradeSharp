using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Common;
using System.Threading;
using WinRT.Interop;
using System.Runtime.InteropServices;
using TradeSharp.Common;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Screen to manage the mass export of instrument data for a given DataProvider.
  /// </summary>
  public sealed partial class MassExportInstrumentDataView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private IMassExportInstrumentDataService m_massExportInstrumentDataService;

    //constructors
    public MassExportInstrumentDataView()
    {
      Settings = new MassExportSettings();
      ExportStructureTooltip = "";
      m_massExportInstrumentDataService = (IMassExportInstrumentDataService)IApplication.Current.Services.GetService(typeof(IMassExportInstrumentDataService));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public string DefaultStartDateTime { get => Constants.DefaultMinimumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public string DefaultEndDateTime { get => Constants.DefaultMaximumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public MassExportSettings Settings { get; internal set; }
    public int ThreadCountMax { get => Environment.ProcessorCount; }
    public string ExportStructureTooltip { get; internal set; }
    public Window ParentWindow { get; set; }
    public string DataProvider { get; set; }

    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportExportDataDateTimeTimeZone));
      Common.Utilities.populateComboBoxFromEnum(ref m_exportStructure, typeof(MassImportExportStructure));
      Common.Utilities.populateComboBoxFromEnum(ref m_fileType, typeof(ImportExportFileTypes));
    }

    private async void m_outputDirectoryBtn_Click(object sender, RoutedEventArgs e)
    {
      var folderPicker = new FolderPicker();
      folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
      folderPicker.FileTypeFilter.Add("*");
      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(folderPicker, hwnd);

      Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
      if (folder != null)
      {
        m_outputDirectory.Text = folder.Path;
        Settings.Directory = folder.Path;
      }
    }

    private void m_exportStructure_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      switch ((MassImportExportStructure)m_exportStructure.SelectedIndex)
      {
        case MassImportExportStructure.DiretoriesAndFiles:
          ExportStructureTooltip = "Export to timeframe directies and files, e.g. OutputDir\\Timeframe\\Ticker.ext";
          break;
        case MassImportExportStructure.FilesOnly:
          ExportStructureTooltip = "Export to files only, e.g. OutputDir\\Ticker_Timeframe.ext";
          break;
      }
    }

    private bool enableExportButton()
    {
      return m_startDateTime != null && DateTime.TryParse(m_startDateTime.Text, out _) && DateTime.TryParse(m_endDateTime.Text, out _) && m_outputDirectory.Text.Length > 0 &&
        ((bool)m_resolutionMinute.IsChecked || (bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_exportBtn_Click(object sender, RoutedEventArgs e)
    {
      m_massExportInstrumentDataService.DataProvider = DataProvider;
      m_massExportInstrumentDataService.Settings = Settings;
      m_massExportInstrumentDataService.Logger = null;    //TODO: Currently we do not set the logger for the mass export service - this can be done as an improvement when we have a progress dialog working.
      IDialogService dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IProgressDialog progressDialog = dialogService.ShowProgressDialog("Mass Export Progress");
      m_massExportInstrumentDataService.StartAsync(progressDialog);
      ParentWindow.Close();
    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }

    private void m_startDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_startDateTime.Text, out DateTime result)) Settings.FromDateTime = result;
      m_exportBtn.IsEnabled = enableExportButton();
    }

    private void m_endDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_endDateTime.Text, out DateTime result)) Settings.ToDateTime = result;
      m_exportBtn.IsEnabled = enableExportButton();
    }

    private void m_outputDirectory_TextChanged(object sender, TextChangedEventArgs e)
    {
      m_exportBtn.IsEnabled = enableExportButton();
    }

    private void m_resolutionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      m_exportBtn.IsEnabled = enableExportButton();
    }

    private void m_resolutionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      m_exportBtn.IsEnabled = enableExportButton();
    }
  }
}
