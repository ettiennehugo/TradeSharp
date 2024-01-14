using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using TradeSharp.WinCoreUI.Common;

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
      IncrementalItems = new IncrementalObservableCollection<Instrument>(ViewModel);
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentViewModel ViewModel { get; }
    public IncrementalObservableCollection<Instrument> IncrementalItems;

    //methods
    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
      //instrument view model is instantiated once and shared between screens, so we need to reset the filters when new screens are loaded
      ViewModel.Filters.Clear();
      refreshFilter();
    }

    private void refreshFilter()
    {
      if (m_instrumentFilter == null) return;

      //restart load of the items using the new filter conditions
      IncrementalItems.Clear();
      ViewModel.OffsetIndex = 0;

      //setup filters for view model
      ViewModel.Filters.Clear();

      if (m_instrumentFilter.Text.Length > 0)
      {
        switch ((FilterField)m_filterMatchFields.SelectedIndex)
        {
          case FilterField.Ticker:
            ViewModel.Filters[InstrumentViewModel.FilterTicker] = m_instrumentFilter.Text;
            break;
          case FilterField.Name:
            ViewModel.Filters[InstrumentViewModel.FilterName] = m_instrumentFilter.Text;
            break;
          case FilterField.Description:
            ViewModel.Filters[InstrumentViewModel.FilterDescription] = m_instrumentFilter.Text;
            break;
          case FilterField.Any:
            ViewModel.Filters[InstrumentViewModel.FilterTicker] = m_instrumentFilter.Text;
            ViewModel.Filters[InstrumentViewModel.FilterName] = m_instrumentFilter.Text;
            ViewModel.Filters[InstrumentViewModel.FilterDescription] = m_instrumentFilter.Text;
            break;
        }
      }

      //load the first page of the filtered items asynchronously
      _ = IncrementalItems.LoadMoreItemsAsync(InstrumentViewModel.DefaultPageSize);
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
