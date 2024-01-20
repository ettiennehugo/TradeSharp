using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.CoreUI.ViewModels;
using System.Collections.ObjectModel;
using TradeSharp.Data;
using TradeSharp.WinCoreUI.Common;
using TradeSharp.CoreUI.Common;
using Microsoft.Extensions.DependencyInjection;

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

    //constructors
    public InstrumentDataView()
    {
      m_configurationService = (IConfigurationService)((IApplication)Application.Current).Services.GetService(typeof(IConfigurationService));
      InstrumentViewModel = (InstrumentViewModel)((IApplication)Application.Current).Services.GetService(typeof(InstrumentViewModel));
      DataProviders = new ObservableCollection<string>();
      IncrementalInstruments = new IncrementalObservableCollection<Instrument>(InstrumentViewModel);
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public ObservableCollection<string> DataProviders { get; set; }
    public IncrementalObservableCollection<Instrument> IncrementalInstruments { get; }
    public InstrumentViewModel InstrumentViewModel { get; set; }

    //methods
    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      //refresh the list of data providers
      DataProviders.Clear();
      foreach (var provider in m_configurationService.DataProviders) DataProviders.Add(provider.Key);

      //instrument view model is instantiated once and shared between screens, so we need to reset the filters when new screens are loaded
      InstrumentViewModel.Filters.Clear();
      refreshFilter();
    }

    private void refreshFilter()
    {
      if (m_instrumentFilter == null) return;

      //restart load of the items using the new filter conditions
      IncrementalInstruments.Clear();
      InstrumentViewModel.OffsetIndex = 0;

      //setup filters for view model
      InstrumentViewModel.Filters.Clear();

      if (m_instrumentFilter.Text.Length > 0)
      {
        switch ((FilterField)m_filterMatchFields.SelectedIndex)
        {
          case FilterField.Ticker:
            InstrumentViewModel.Filters[InstrumentViewModel.FilterTicker] = m_instrumentFilter.Text;
            break;
          case FilterField.Name:
            InstrumentViewModel.Filters[InstrumentViewModel.FilterName] = m_instrumentFilter.Text;
            break;
          case FilterField.Description:
            InstrumentViewModel.Filters[InstrumentViewModel.FilterDescription] = m_instrumentFilter.Text;
            break;
          case FilterField.Any:
            InstrumentViewModel.Filters[InstrumentViewModel.FilterTicker] = m_instrumentFilter.Text;
            InstrumentViewModel.Filters[InstrumentViewModel.FilterName] = m_instrumentFilter.Text;
            InstrumentViewModel.Filters[InstrumentViewModel.FilterDescription] = m_instrumentFilter.Text;
            break;
        }
      }

      //load the first page of the filtered items asynchronously
      _ = IncrementalInstruments.LoadMoreItemsAsync(InstrumentViewModel.DefaultPageSize);
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
    }

    private void m_dataProviders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_massImport.IsEnabled = true;
      m_massExport.IsEnabled = true;
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

    private void m_massImport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      throw new NotImplementedException();    //TODO
    }

    private void m_massExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      throw new NotImplementedException();    //TODO
    }
  }
}
