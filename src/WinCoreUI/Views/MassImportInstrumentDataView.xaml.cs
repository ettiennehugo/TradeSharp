using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Common;
using System.Threading;

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
    private IMassImportInstrumentDataService m_massImportInstrumentDataService;
    private CancellationToken m_cancellationToken;

    //constructors
    public MassImportInstrumentDataView()
    {
      Settings = new MassImportSettings();
      ImportStructureTooltip = "";
      m_massImportInstrumentDataService = (IMassImportInstrumentDataService)IApplication.Current.Services.GetService(typeof(IMassImportInstrumentDataService));
      m_cancellationToken = new CancellationToken();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public MassImportSettings Settings { get; internal set; }
    public int ThreadCountMax { get => Environment.ProcessorCount; }
    public string ImportStructureTooltip { get; internal set; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportDataDateTimeTimeZone));
      Common.Utilities.populateComboBoxFromEnum(ref m_importStructure, typeof(MassImportExportStructure));
      Common.Utilities.populateComboBoxFromEnum(ref m_fileType, typeof(ImportExportFileTypes));
    }

    private async void m_inputDirectoryBtn_Click(object sender, RoutedEventArgs e)
    {
      var folderPicker = new FolderPicker();
      folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
      folderPicker.FileTypeFilter.Add("*");

      Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
      if (folder != null)
      {
        m_inputDirectory.Text = folder.Path;
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

    private void m_exportBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
