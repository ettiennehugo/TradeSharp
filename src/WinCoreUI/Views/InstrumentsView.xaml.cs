using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the list of instruments defined for trading.
  /// </summary>  
  public sealed partial class InstrumentsView : Page
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


    //constructors
    public InstrumentsView()
    {
      ViewModel = (InstrumentViewModel)((IApplication)Application.Current).Services.GetService(typeof(InstrumentViewModel));
      Instruments = new ObservableCollection<Instrument>(ViewModel.Items);
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentViewModel ViewModel { get; }
    public ObservableCollection<Instrument> Instruments;

    //methods
    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      //instrument view model is instantiated once and shared between screens, so we need to reset the filters when new screens are loaded
      resetFilter();
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
      var filteredResult = from instrument in ViewModel.Items where filterInstrument(instrument) select instrument;
      Instruments.Clear();
      foreach (var instrument in filteredResult) Instruments.Add(instrument);
      ViewModel.SelectedItem = Instruments.FirstOrDefault();
    }

    private void resetFilter()
    {
      m_instrumentFilter.ClearValue(TextBox.TextProperty);
      m_filterMatchFields.SelectedIndex = (int)FilterField.Any;
      Instruments.Clear();
      foreach (var instrument in ViewModel.Items) Instruments.Add(instrument);
      ViewModel.SelectedItem = Instruments.FirstOrDefault();
    }

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      refreshFilter();
    }
  }
}
