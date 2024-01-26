using System;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.CoreUI.ViewModels;
using Microsoft.UI.Xaml;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.WinCoreUI.ViewModels;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Control to display bar data for a given instrument.
  /// </summary>
  public sealed partial class InstrumentBarsDataView : UserControl
  {
    //constants
    /// <summary>
    /// Constants to define how many bars to incrementally load in terms of pages and number of items per page when the user
    /// crolls through the collection.
    /// </summary>
    public const double MinuteDataFethSize = 5.0;
    public const double MinuteIncrementalLoadingThreshold = 1000.0;
    public const double HourDataFethSize = 1.0;
    public const double HourIncrementalLoadingThreshold = 10.0;
    public const double DayDataFethSize = 1.0;
    public const double DayIncrementalLoadingThreshold = 2.0;
    public const double WeekDataFethSize = 1.0;
    public const double WeekIncrementalLoadingThreshold = 1.0;     //weeks per year
    public const double MonthDataFethSize = 1.0;
    public const double MonthIncrementalLoadingThreshold = 1.0;    //months per year

    //enums


    //types


    //attributes
    private ILogger<InstrumentBarDataView> m_logger;

    //constructors
    public InstrumentBarsDataView()
    {
      ViewModel = (WinInstrumentBarDataViewModel)((IApplication)Application.Current).Services.GetService(typeof(WinInstrumentBarDataViewModel));
      ViewModel.Resolution = Resolution;
      ViewModel.RefreshEvent += onViewModelRefresh;
      m_logger = (ILogger<InstrumentBarDataView>)((IApplication)Application.Current).Services.GetService(typeof(ILogger<InstrumentBarDataView>));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty DataProviderProperty = DependencyProperty.Register("DataProvider", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata(""));
    public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register("Resolution", typeof(Resolution), typeof(InstrumentBarsDataView), new PropertyMetadata(Resolution.Day));
    public static readonly DependencyProperty InstrumentProperty = DependencyProperty.Register("Instrument", typeof(Instrument), typeof(InstrumentBarsDataView), new PropertyMetadata(null));
    public static readonly DependencyProperty FilterStartTooltipProperty = DependencyProperty.Register("FilterStartTooltip", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata("Filter start date/time"));
    public static readonly DependencyProperty FilterEndTooltipProperty = DependencyProperty.Register("FilterEndTooltip", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata("Filter end date/time"));

    public string DataProvider
    {
      get => (string)GetValue(DataProviderProperty);
      set
      {
        SetValue(DataProviderProperty, value);
        ViewModel.DataProvider = value;
        resetFilter();
      }
    }

    public Resolution Resolution
    {
      get => (Resolution)GetValue(ResolutionProperty);
      set
      {
        SetValue(ResolutionProperty, value);
        ViewModel.Resolution = value;
        refreshIncrementalLoading();
        refreshCopyMenu();
        refreshFilterControls();
        resetFilter();
      }
    }

    public Instrument? Instrument
    {
      get => (Instrument?)GetValue(InstrumentProperty);
      set
      {
        SetValue(InstrumentProperty, value);
        ViewModel.Instrument = value;
        resetFilter();
      }
    }

    public string FilterStartTooltip { get => (string)GetValue(FilterStartTooltipProperty); internal set { SetValue(FilterStartTooltipProperty, value); } }
    public string FilterEndTooltip { get => (string)GetValue(FilterEndTooltipProperty); internal set { SetValue(FilterEndTooltipProperty, value); } }
    public WinInstrumentBarDataViewModel ViewModel { get; internal set; }

    //methods
    private void refreshCopyMenu()
    {
      switch (Resolution)
      {
        case Resolution.Level1:
          throw new ArgumentException("Level1 resolution not supported by bar data view, use view for level1 data.");
        case Resolution.Minute:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Visible;
          m_copyToDay.Visibility = Visibility.Visible;
          m_copyToWeek.Visibility = Visibility.Visible;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Hour:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Visible;
          m_copyToWeek.Visibility = Visibility.Visible;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Day:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Collapsed;
          m_copyToWeek.Visibility = Visibility.Visible;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Week:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Collapsed;
          m_copyToWeek.Visibility = Visibility.Collapsed;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Month:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Collapsed;
          m_copyToWeek.Visibility = Visibility.Collapsed;
          m_copyToMonth.Visibility = Visibility.Collapsed;
          break;
      }
    }

    private void refreshFilterControls()
    {
      switch (Resolution)
      {
        case Resolution.Level1:
          throw new ArgumentException("Level1 resolution not supported by bar data view, use view for level1 data.");
        case Resolution.Minute:
          FilterStartTooltip = "Filter start date/time";
          FilterEndTooltip = "Filter end date/time";
          m_startDateTime.PlaceholderText = "yyyy/mm/dd hh:mm";
          m_endDateTime.PlaceholderText = "yyyy/mm/dd hh:mm";
          break;
        case Resolution.Hour:
          FilterStartTooltip = "Filter start date/hour";
          FilterEndTooltip = "Filter end date/hour";
          m_startDateTime.PlaceholderText = "yyyy/mm/dd hh:00";
          m_endDateTime.PlaceholderText = "yyyy/mm/dd hh:00";
          break;
        case Resolution.Day:
        case Resolution.Week:
          FilterStartTooltip = "Filter start date";
          FilterEndTooltip = "Filter end date";
          m_startDateTime.PlaceholderText = "yyyy/mm/dd";
          m_endDateTime.PlaceholderText = "yyyy/mm/dd";
          break;
        case Resolution.Month:
          FilterStartTooltip = "Filter start date";
          FilterEndTooltip = "Filter end date";
          m_startDateTime.PlaceholderText = "yyyy/mm/01";
          m_endDateTime.PlaceholderText = "yyyy/mm/01";
          break;
      }
    }

    /// <summary>
    /// Setup the incremental loading based on the resolution to make sure that incremental
    /// blocks loaded would be appropriate for the resolution, e.g. minute bar data could be hundreds
    /// of thousamds to millions while monthly resolution could be a few hundred.
    /// </summary>
    private void refreshIncrementalLoading()
    {
      switch (Resolution)
      {
        case Resolution.Level1:
          throw new ArgumentException("Level1 resolution not supported by bar data view, use view for level1 data.");
        case Resolution.Minute:
          m_dataTable.IncrementalLoadingThreshold = MinuteIncrementalLoadingThreshold;
          m_dataTable.DataFetchSize = MinuteDataFethSize;
          break;
        case Resolution.Hour:
          m_dataTable.IncrementalLoadingThreshold = HourIncrementalLoadingThreshold;
          m_dataTable.DataFetchSize = HourDataFethSize;
          break;
        case Resolution.Day:
          m_dataTable.IncrementalLoadingThreshold = DayIncrementalLoadingThreshold;
          m_dataTable.DataFetchSize = DayDataFethSize;
          break;
        case Resolution.Week:
          m_dataTable.IncrementalLoadingThreshold = WeekIncrementalLoadingThreshold;
          m_dataTable.DataFetchSize = WeekDataFethSize;
          break;
        case Resolution.Month:
          m_dataTable.IncrementalLoadingThreshold = MonthIncrementalLoadingThreshold;
          m_dataTable.DataFetchSize = MonthDataFethSize;
          break;
      }
    }

    private void resetFilter()
    {
      //reset the date and time controls
      m_startDateTime.ClearValue(TextBox.TextProperty);
      m_endDateTime.ClearValue(TextBox.TextProperty);

      //reset the view model filter
      ViewModel.Filter[InstrumentBarDataViewModel.FilterFromDateTime] = WinInstrumentBarDataViewModel.s_defaultStartDateTime;
      ViewModel.Filter[InstrumentBarDataViewModel.FilterToDateTime] = WinInstrumentBarDataViewModel.s_defaultEndDateTime;

      //restart load of the items using the new filter conditions
      ViewModel.OnRefreshAsync();
    }

    private void refreshFilter()
    {
      //set the view model filter
      if (DateTime.TryParse(m_startDateTime.Text, out DateTime startDateTime))
      {
        if (Debugging.InstrumentBarDataFilterParse) m_logger.LogInformation($"Parsed filter start date/time - {startDateTime}");
        ViewModel.Filter[InstrumentBarDataViewModel.FilterFromDateTime] = startDateTime;
      }
      else
        ViewModel.Filter[InstrumentBarDataViewModel.FilterFromDateTime] = WinInstrumentBarDataViewModel.s_defaultStartDateTime;

      if (DateTime.TryParse(m_endDateTime.Text, out DateTime endDateTime))
      {
        if (Debugging.InstrumentBarDataFilterParse) m_logger.LogInformation($"Parsed filter end date/time - {endDateTime}");
        ViewModel.Filter[InstrumentBarDataViewModel.FilterToDateTime] = endDateTime;
      }
      else
        ViewModel.Filter[InstrumentBarDataViewModel.FilterToDateTime] = WinInstrumentBarDataViewModel.s_defaultEndDateTime;

      //restart load of the items using the new filter conditions
      ViewModel.OnRefreshAsync();
    }

    private void m_resetFilter_Click(object sender, RoutedEventArgs e)
    {
      resetFilter();
    }

    private void onViewModelRefresh(object? sender, RefreshEventArgs e)
    {
      //NOTE: Event to refresh will most likely come from a background thread, so we need to marshal the call to the UI thread.
      m_dataTable.DispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() => resetFilter()));
    }

    private void m_startDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_startDateTime.Text, out _)) refreshFilter();
    }

    private void m_endDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_endDateTime.Text, out _)) refreshFilter();
    }
  }
}
