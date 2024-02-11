using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
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
    private IConfigurationService m_configurationService;
    private IExchangeViewModel m_exchangeViewModel;
    private ILogger<InstrumentBarDataView> m_logger;

    //constructors
    public InstrumentBarsDataView()
    {
      m_configurationService = (IConfigurationService)IApplication.Current.Services.GetService(typeof(IConfigurationService));
      m_exchangeViewModel = (IExchangeViewModel)IApplication.Current.Services.GetService(typeof(IExchangeViewModel));
      ViewModel = (WinInstrumentBarDataViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataViewModel));
      ViewModel.UIDispatcherQueue = DispatcherQueue.GetForCurrentThread();
      ViewModel.Resolution = Resolution;
      ViewModel.RefreshEvent += onViewModelRefresh;
      m_logger = (ILogger<InstrumentBarDataView>)IApplication.Current.Services.GetService(typeof(ILogger<InstrumentBarDataView>));
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
      string timeZoneValue = m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone].ToString();
      switch (Resolution)
      {
        case Resolution.Level1:
          throw new ArgumentException("Level1 resolution not supported by bar data view, use view for level1 data.");
        case Resolution.Minute:
          FilterStartTooltip = $"Filter start date/time ({timeZoneValue} time-zone)";
          FilterEndTooltip = $"Filter end date/time ({timeZoneValue} time-zone)";
          m_startDateTime.PlaceholderText = "yyyy/mm/dd hh:mm";
          m_endDateTime.PlaceholderText = "yyyy/mm/dd hh:mm";
          break;
        case Resolution.Hour:
          FilterStartTooltip = $"Filter start date/hour ({timeZoneValue} time-zone)";
          FilterEndTooltip = $"Filter end date/hour ({timeZoneValue} time-zone)";
          m_startDateTime.PlaceholderText = "yyyy/mm/dd hh:00";
          m_endDateTime.PlaceholderText = "yyyy/mm/dd hh:00";
          break;
        case Resolution.Day:
        case Resolution.Week:
          FilterStartTooltip = $"Filter start date ({timeZoneValue} time-zone)";
          FilterEndTooltip = $"Filter end date ({timeZoneValue} time-zone)";
          m_startDateTime.PlaceholderText = "yyyy/mm/dd";
          m_endDateTime.PlaceholderText = "yyyy/mm/dd";
          break;
        case Resolution.Month:
          FilterStartTooltip = $"Filter start date ({timeZoneValue} time-zone)";
          FilterEndTooltip = $"Filter end date ({timeZoneValue} time-zone)";
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
      ViewModel.FromDateTime = Constants.DefaultMinimumDateTime;
      ViewModel.ToDateTime = Constants.DefaultMaximumDateTime;

      //restart load of the items using the new filter conditions
      ViewModel.OnRefreshAsync();
    }

    private static DateTime convertDateTimeBasedOnConfiguration(DateTime utcDateTime, IConfigurationService.TimeZone timeZoneToUse, Exchange? exchange)
    {
      //NOTE: Database stores the data in UTC, so we need to convert the date/time to the appropriate time zone. This is the opposite of the database
      //      which converts the date/time to UTC before storing it.
      switch (timeZoneToUse)
      {
        case IConfigurationService.TimeZone.UTC:
          return utcDateTime;
        case IConfigurationService.TimeZone.Local:
          return utcDateTime.Subtract(TimeZoneInfo.Local.GetUtcOffset(utcDateTime));    //can not use the BaseUtcOffset, we need to use the conversion function to take into account daylight savings time
        case IConfigurationService.TimeZone.Exchange:
          if (exchange == null) throw new ArgumentException("Exchange must be specified when using Exchange time zone.");
          return utcDateTime.Subtract(exchange.TimeZone.GetUtcOffset(utcDateTime));    //can not use the BaseUtcOffset, we need to use the conversion function to take into account daylight savings time
      }

      return utcDateTime; //default to UTC
    }

    private void refreshFilter()
    {
      //set the view model filter
      //NOTE: Date/time must be parsed as a UTC date/time since that is what is stored in the database - otherwise results will be wrong.
      if (DateTime.TryParse(m_startDateTime.Text, null, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out DateTime startDateTime))
      {
        if (Debugging.InstrumentBarDataFilterParse) m_logger.LogInformation($"Parsed filter start date/time - {startDateTime}");
        Exchange exchange = m_exchangeViewModel.GetItem(ViewModel.Instrument!.PrimaryExchangeId) ?? m_exchangeViewModel.GlobalExchange;
        startDateTime = convertDateTimeBasedOnConfiguration(startDateTime, (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone], exchange);
        ViewModel.FromDateTime = startDateTime;
      }
      else
        ViewModel.FromDateTime = Constants.DefaultMinimumDateTime;

      //NOTE: Date/time must be parsed as a UTC date/time since that is what is stored in the database - otherwise results will be wrong.
      if (DateTime.TryParse(m_endDateTime.Text, null, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out DateTime endDateTime))
      {
        if (Debugging.InstrumentBarDataFilterParse) m_logger.LogInformation($"Parsed filter end date/time - {endDateTime}");
        Exchange exchange = m_exchangeViewModel.GetItem(ViewModel.Instrument!.PrimaryExchangeId) ?? m_exchangeViewModel.GlobalExchange;
        endDateTime = convertDateTimeBasedOnConfiguration(endDateTime, (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone], exchange);
        ViewModel.ToDateTime = endDateTime;
      }
      else
        ViewModel.ToDateTime = Constants.DefaultMaximumDateTime;

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
      DispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() => resetFilter()));
    }

    private void m_startDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (m_startDateTime.Text.Trim().Length == 0 || DateTime.TryParse(m_startDateTime.Text, out _)) refreshFilter();
    }

    private void m_endDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (m_endDateTime.Text.Trim().Length == 0 || DateTime.TryParse(m_endDateTime.Text, out _)) refreshFilter();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      resetFilter();
    }
  }
}
