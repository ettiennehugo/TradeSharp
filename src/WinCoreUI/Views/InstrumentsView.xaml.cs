using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
      ViewModel = Ioc.Default.GetRequiredService<InstrumentViewModel>();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentViewModel ViewModel { get; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommandAsync.ExecuteAsync(null);
    }

    public bool filter(Instrument instrument)
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

    private void refreshFilter()
    {
      if (m_instrumentFilter == null) return;

      //show all items on clear filter text
      if (m_instrumentFilter.Text.Length == 0)
      {
        if (m_instruments.ItemsSource != ViewModel.Items) m_instruments.ItemsSource = ViewModel.Items;
        return;
      }

      m_instruments.ItemsSource = new ObservableCollection<Instrument>(from instrument in ViewModel.Items where filter(instrument) select instrument);
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
