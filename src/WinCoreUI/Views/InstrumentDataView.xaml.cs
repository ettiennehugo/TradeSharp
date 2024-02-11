using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.CoreUI.ViewModels;
using System.Collections.ObjectModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentDataView : Page
  {
    //constants


    //enums
    private enum FilterField
    {
      Ticker = 0,
      Name,
      Description,
      Any
    }

    //types


    //attributes
    private IConfigurationService m_configurationService;
    private IDialogService m_dialogService;

    //constructors
    public InstrumentDataView()
    {
      m_configurationService = (IConfigurationService)IApplication.Current.Services.GetService(typeof(IConfigurationService));
      InstrumentViewModel = (IInstrumentViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentViewModel));
      m_dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      DataProviders = new ObservableCollection<string>();
      Instruments = new ObservableCollection<Instrument>(InstrumentViewModel.Items);
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public ObservableCollection<string> DataProviders { get; set; }
    public ObservableCollection<Instrument> Instruments { get; set; }
    public IInstrumentViewModel InstrumentViewModel { get; set; }

    //methods
    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      //refresh the list of data providers
      DataProviders.Clear();
      foreach (var provider in m_configurationService.DataProviders) DataProviders.Add(provider.Key);
    }

    private bool filterInstrument(Instrument instrument)
    {
      if (m_instrumentFilter.Text.Length == 0) return true;

      switch ((FilterField)m_filterMatchFields.SelectedIndex)
      {
        case FilterField.Ticker:
          return instrument.Ticker.Contains(m_instrumentFilter.Text);
        case FilterField.Name:
          return instrument.Name.Contains(m_instrumentFilter.Text);
        case FilterField.Description:
          return instrument.Description.Contains(m_instrumentFilter.Text);
        case FilterField.Any:
          return instrument.Ticker.Contains(m_instrumentFilter.Text) || instrument.Name.Contains(m_instrumentFilter.Text) || instrument.Description.Contains(m_instrumentFilter.Text);
        default:
          return false;
      }
    }

    private void refreshFilter()
    {
      if (m_instrumentFilter == null) return;
      var filteredResult = from instrument in InstrumentViewModel.Items where filterInstrument(instrument) select instrument;
      Instruments.Clear();
      foreach (var instrument in filteredResult) Instruments.Add(instrument);
      InstrumentViewModel.SelectedItem = Instruments.FirstOrDefault();
    }

    private void resetFilter()
    {
      m_instrumentFilter.ClearValue(TextBox.TextProperty);
      m_filterMatchFields.SelectedIndex = (int)FilterField.Any;
      Instruments.Clear();
      foreach (var instrument in InstrumentViewModel.Items) Instruments.Add(instrument);
      InstrumentViewModel.SelectedItem = Instruments.FirstOrDefault();
    }

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_refreshCommand_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      InstrumentViewModel.RefreshCommandAsync.Execute(this);
      resetFilter();
    }

    private void m_dataProviders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_massImport.IsEnabled = true;
      m_massExport.IsEnabled = true;
      m_massCopy.IsEnabled = true;
      m_massDownload.IsEnabled = true;
      m_minuteBarsData.DataProvider = (string)m_dataProviders.SelectedItem;
      m_hoursBarsData.DataProvider = (string)m_dataProviders.SelectedItem;
      m_daysBarsData.DataProvider = (string)m_dataProviders.SelectedItem;
      m_weeksBarsData.DataProvider = (string)m_dataProviders.SelectedItem;
      m_monthsBarsData.DataProvider = (string)m_dataProviders.SelectedItem;
    }

    private void m_instrumentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_minuteBarsData.Instrument = (Instrument)m_instrumentsGrid.SelectedItem;
      m_hoursBarsData.Instrument = (Instrument)m_instrumentsGrid.SelectedItem;
      m_daysBarsData.Instrument = (Instrument)m_instrumentsGrid.SelectedItem;
      m_weeksBarsData.Instrument = (Instrument)m_instrumentsGrid.SelectedItem;
      m_monthsBarsData.Instrument = (Instrument)m_instrumentsGrid.SelectedItem;
    }

    private async void m_massImport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataImportAsync((string)m_dataProviders.SelectedItem);
    }

    private async void m_massExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataExportAsync((string)m_dataProviders.SelectedItem);
    }

    private async void m_massCopy_Click(object sender, RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataCopyAsync((string)m_dataProviders.SelectedItem);
    }

    private async void m_massDownload_Click(object sender, RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataDownloadAsync((string)m_dataProviders.SelectedItem);
    }
  }
}
