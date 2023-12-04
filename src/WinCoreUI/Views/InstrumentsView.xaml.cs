using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using System;
using TradeSharp.CoreUI.Services;

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
      Instruments = new AdvancedCollectionView(ViewModel.Items, true); 
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentViewModel ViewModel { get; }
    public AdvancedCollectionView Instruments { get; }


    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommand.Execute(null);
    }

    public bool filter(object o)
    {
      if (m_instrumentFilter == null || m_instrumentFilter.Text.Length == 0) return true; //no filter specified - m_instrumentFilter is null on screen init

      if (o == null || o is not Instrument) return false;
      Instrument instrument = (Instrument)o;

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

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      Instruments.Filter = new Predicate<object>(filter);
      Instruments.RefreshFilter();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      Instruments.Filter = new Predicate<object>(filter);
      Instruments.RefreshFilter();
    }


  }
}
