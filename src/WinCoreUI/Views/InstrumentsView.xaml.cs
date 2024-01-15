using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.WinCoreUI.Common;
using System.Data;

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
    private object m_refreshLock;

    //constructors
    public InstrumentsView()
    {
      m_refreshLock = new object();
      ViewModel = Ioc.Default.GetRequiredService<InstrumentViewModel>();
      ViewModel.RefreshEvent += onViewModelRefresh;
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

      lock (m_refreshLock)
      {
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
    }

    private void resetFilter()
    {
      lock (m_refreshLock)
      {
        m_instrumentFilter.ClearValue(TextBox.TextProperty);
        m_filterMatchFields.SelectedIndex = (int)FilterField.Any;
        ViewModel.Filters.Clear();

        IncrementalItems.Clear();
        ViewModel.OffsetIndex = 0;
        _ = IncrementalItems.LoadMoreItemsAsync(InstrumentBarDataViewModel.DefaultPageSize);
      }
    }

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      refreshFilter();
    }

    private void onViewModelRefresh(object? sender, RefreshEventArgs e)
    {
      //NOTE: Event to refresh will most likely come from a background thread, so we need to marshal the call to the UI thread.
      m_instruments.DispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() => resetFilter()));
    }
  }
}
