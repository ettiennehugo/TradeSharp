using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Common;
using System.Threading;

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
    private CancellationToken m_cancellationToken;

    //constructors
    public MassExportInstrumentDataView()
    {
      Settings = new MassExportSettings();
      ExportStructureTooltip = "";
      m_massExportInstrumentDataService = (IMassExportInstrumentDataService)IApplication.Current.Services.GetService(typeof(IMassExportInstrumentDataService));
      m_cancellationToken = new CancellationToken();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public MassExportSettings Settings { get; internal set; }
    public int ThreadCountMax { get => Environment.ProcessorCount; }
    public string ExportStructureTooltip { get; internal set; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportDataDateTimeTimeZone));
      Common.Utilities.populateComboBoxFromEnum(ref m_exportStructure, typeof(MassImportExportStructure));
      Common.Utilities.populateComboBoxFromEnum(ref m_fileType, typeof(ImportExportFileTypes));
    }

    private async void m_outputDirectoryBtn_Click(object sender, RoutedEventArgs e)
    {
      var folderPicker = new FolderPicker();
      folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
      folderPicker.FileTypeFilter.Add("*");

      Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
      if (folder != null)
      {
        m_outputDirectory.Text = folder.Path;
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

    private void m_exportBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
