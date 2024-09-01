using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.ViewModels;
using System.Collections.Generic;
using TradeSharp.Data;
using TradeSharp.Common;
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


    //types


    //attributes
    private IPluginsViewModel m_pluginViewModel;
    private IDialogService m_dialogService;

    //constructors
    public InstrumentDataView()
    {
      m_pluginViewModel = (IPluginsViewModel)IApplication.Current.Services.GetService(typeof(IPluginsViewModel));
      m_pluginViewModel.PluginsToDisplay = PluginsToDisplay.DataProviders;
      InstrumentViewModel = (IInstrumentViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentViewModel));
      m_dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      PriceFormatMask = "0.00";
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public IPlugin SelectedDataProvider { get; set; }
    public IList<IPlugin> DataProviders { get => m_pluginViewModel.Items; }
    public IInstrumentViewModel InstrumentViewModel { get; set; }
    public string PriceFormatMask { get; set; }

    //methods
    private void m_refreshCommand_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      InstrumentViewModel.RefreshCommandAsync.Execute(this);
    }

    private void m_dataProviders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_massImport.IsEnabled = true;
      m_massExport.IsEnabled = true;
      m_massCopy.IsEnabled = true;
      m_massDownload.IsEnabled = true;
      if (SelectedDataProvider != null)
      {
        m_minuteBarsData.DataProvider = SelectedDataProvider.Name;
        m_hoursBarsData.DataProvider = SelectedDataProvider.Name;
        m_daysBarsData.DataProvider = SelectedDataProvider.Name;
        m_weeksBarsData.DataProvider = SelectedDataProvider.Name;
        m_monthsBarsData.DataProvider = SelectedDataProvider.Name;
      }
    }

    private async void m_massImport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataImportAsync(SelectedDataProvider.Name);
    }

    private async void m_massExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataExportAsync(SelectedDataProvider.Name);
    }

    private async void m_massCopy_Click(object sender, RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataCopyAsync(SelectedDataProvider.Name);
    }

    private async void m_massDownload_Click(object sender, RoutedEventArgs e)
    {
      await m_dialogService.ShowMassDataDownloadAsync(SelectedDataProvider.Name);
    }

    private void m_instrumentSelectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_minuteBarsData.Instrument = m_instrumentSelectionView.SelectedItems.Count > 0 ? m_instrumentSelectionView.SelectedItems[0] : null;
      m_hoursBarsData.Instrument = m_instrumentSelectionView.SelectedItems.Count > 0 ? m_instrumentSelectionView.SelectedItems[0] : null;
      m_daysBarsData.Instrument = m_instrumentSelectionView.SelectedItems.Count > 0 ? m_instrumentSelectionView.SelectedItems[0] : null;
      m_weeksBarsData.Instrument = m_instrumentSelectionView.SelectedItems.Count > 0 ? m_instrumentSelectionView.SelectedItems[0] : null;
      m_monthsBarsData.Instrument = m_instrumentSelectionView.SelectedItems.Count > 0 ? m_instrumentSelectionView.SelectedItems[0] : null;
    }
  }
}
