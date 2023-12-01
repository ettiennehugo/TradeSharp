using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.CoreUI.ViewModels;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.Data;
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
    private IInstrumentService m_instrumentService;

    //constructors
    public InstrumentDataView()
    {
      DataProviders = new ObservableCollection<string>();
      FilteredInstruments = new ObservableCollection<Instrument>();
      m_configurationService = Ioc.Default.GetRequiredService<IConfigurationService>();
      m_instrumentService = Ioc.Default.GetRequiredService<IInstrumentService>();
      ViewModel = Ioc.Default.GetRequiredService<InstrumentBarDataViewModel>();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public ObservableCollection<string> DataProviders { get; set; }
    public ObservableCollection<Instrument> FilteredInstruments { get; set; }
    public InstrumentViewModel InstrumentViewModel { get; set; }
    public InstrumentBarDataViewModel ViewModel { get; }

    //methods
    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      m_instrumentFilter.Text = "";
      if (m_instrumentService.Items.Count == 0) await m_instrumentService.RefreshAsync();
      FilteredInstruments.Clear();
      foreach (var instrument in m_instrumentService.Items) FilteredInstruments.Add(instrument);

      DataProviders.Clear();
      foreach (var provider in m_configurationService.DataProviders) DataProviders.Add(provider.Key);
    }

    private bool filter(Instrument instrument)
    {
      if (m_instrumentFilter == null || m_instrumentFilter.Text.Length == 0) return true; //no filter specified - m_instrumentFilter is null on screen init

      switch ((FilterField)m_filterMatchFields.SelectedIndex)
      {
        case FilterField.Ticker:
          return instrument.Ticker.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
        case FilterField.Name:
          return instrument.Name.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
        case FilterField.Description:
          return instrument.Description.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
        case FilterField.Any:
          return instrument.Ticker.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase) || instrument.Name.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase) || instrument.Description.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
      }

      return false;   //in general should not happen if match field is mandatory selection
    }

    private void filterInstruments()
    {
        //apply filter to service's list of instruments
        var filteredInstruments = m_instrumentService.Items.Where(instrument => filter(instrument));

        //remove items that were filtered
        for (int i = FilteredInstruments.Count - 1; i >= 0; i--)
        {
          var instrument = FilteredInstruments[i];
          if (!filteredInstruments.Contains(instrument)) FilteredInstruments.Remove(instrument);
        }

        //add items that were added by new filter - need to insert them to keep the instruments ordered by ticker
        foreach (var instrument in filteredInstruments)
          if (!FilteredInstruments.Contains(instrument))
          {
            if (FilteredInstruments.Count == 0)
              FilteredInstruments.Add(instrument);
            else
            {
              int i = 0;
              while (i < FilteredInstruments.Count - 1 && string.Compare(FilteredInstruments[i].Ticker, instrument.Ticker, StringComparison.OrdinalIgnoreCase) < 0) i++;
              if (i < FilteredInstruments.Count - 1)
                FilteredInstruments.Insert(i, instrument);
              else
                FilteredInstruments.Add(instrument);
            }
          }
    }

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      filterInstruments();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      filterInstruments();
    }

    private async void m_refreshCommand_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      InstrumentViewModel.RefreshCommandAsync.Execute(this);
      await m_instrumentService.RefreshAsync();
      filterInstruments();
    }

    private void m_dataProviders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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
  }
}
