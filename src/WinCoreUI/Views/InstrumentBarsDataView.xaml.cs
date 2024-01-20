using System;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.CoreUI.ViewModels;
using Microsoft.UI.Xaml;
using TradeSharp.CoreUI.Common;
using TradeSharp.WinCoreUI.Common;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Control to display bar data for a given instrument.
  /// </summary>
  public sealed partial class InstrumentBarsDataView : UserControl
  {
    //constants


    //enums


    //types


    //attributes
    //NOTE: These start/end dates are very much tied to the Date and Time controls, if these controls are changed
    //      these dates would need to be changed as well to ensure the page works optimally.
    public static DateTime s_defaultStartDateTime = new DateTime(1980, 1, 1, 0, 0, 0);
    public static DateTime s_defaultEndDateTime = new DateTime(2075, 12, 30, 23, 59, 00); //NOTE: The 30 December is correct, for some reason that is the date control maximum when you max it out.
    public static DateTimeOffset s_defaultStartDate = new DateTimeOffset(1980, 1, 1, 12, 0, 0, new TimeSpan(0, 0, 0));
    public static DateTimeOffset s_defaultEndDate = new DateTimeOffset(2075, 12, 31, 0, 0, 0, new TimeSpan(0, 0, 0));
    public static TimeSpan s_defaultStartTime = new TimeSpan(0, 0, 0);
    public static TimeSpan s_defaultEndTime = new TimeSpan(23, 59, 59);
    private object m_refreshLock;

    //constructors
    public InstrumentBarsDataView()
    {
      m_refreshLock = new object();
      ViewModel = (InstrumentBarDataViewModel)((IApplication)Application.Current).Services.GetService(typeof(InstrumentBarDataViewModel));
      ViewModel.Resolution = Resolution;
      ViewModel.RefreshEvent += onViewModelRefresh;
      IncrementalItems = new IncrementalObservableCollection<IBarData>(ViewModel);
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
        refreshCopyMenu();
        refreshFilterControls();
        resetFilter();
      }
    }

    public Instrument? Instrument 
    { 
      get => (Instrument?)GetValue(InstrumentProperty); 
      set { 
        SetValue(InstrumentProperty, value); 
        ViewModel.Instrument = value;
        resetFilter();
      } 
    }
    public string FilterStartTooltip { get => (string)GetValue(FilterStartTooltipProperty); internal set { SetValue(FilterStartTooltipProperty, value); } }
    public string FilterEndTooltip { get => (string)GetValue(FilterEndTooltipProperty); internal set { SetValue(FilterEndTooltipProperty, value); } }
    public InstrumentBarDataViewModel ViewModel { get; internal set; }
    public IncrementalObservableCollection<IBarData> IncrementalItems { get; }

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
          m_startDate.DayVisible = true;
          m_startDate.Margin = new Thickness(8, 8, 0, 8);   //place date/time controls right next to each other
          m_endDate.DayVisible = true;
          m_startTime.Visibility = Visibility.Visible;
          m_endTime.Visibility = Visibility.Visible;
          break;
        case Resolution.Hour:
          FilterStartTooltip = "Filter start date/hour";
          FilterEndTooltip = "Filter end date/hour";
          m_startDate.DayVisible = true;
          m_startDate.Margin = new Thickness(8, 8, 0, 8);
          m_endDate.DayVisible = true;
          m_startTime.Visibility = Visibility.Visible;
          m_endTime.Visibility = Visibility.Visible;
          break;
        case Resolution.Day:
        case Resolution.Week:
          FilterStartTooltip = "Filter start date";
          FilterEndTooltip = "Filter end date";
          m_startDate.DayVisible = true;
          m_startDate.Margin = new Thickness(8, 8, 8, 8);   //since time is hidden leave some space between start date field and separating "-"
          m_endDate.DayVisible = true;
          m_startTime.Visibility = Visibility.Collapsed;
          m_endTime.Visibility = Visibility.Collapsed;
          break;
        case Resolution.Month:
          FilterStartTooltip = "Filter start date";
          FilterEndTooltip = "Filter end date";
          m_startDate.DayVisible = false;
          m_startDate.Margin = new Thickness(8, 8, 8, 8);
          m_endDate.DayVisible = false;
          m_startTime.Visibility = Visibility.Collapsed;
          m_endTime.Visibility = Visibility.Collapsed;
          break;
      }
    }

    private void resetFilter()
    {
      lock(m_refreshLock)
      {
        m_startDate.Date = s_defaultStartDate;
        m_endDate.Date = s_defaultEndDate;
        m_startTime.Time = s_defaultStartTime;
        m_endTime.Time = s_defaultEndTime;

        //update the view model filter
        ViewModel.Filter[InstrumentBarDataViewModel.FilterFromDateTime] = new DateTime(m_startDate.Date.Year, m_startDate.Date.Month, m_startDate.Date.Day, m_startTime.Time.Hours, m_startTime.Time.Minutes, 0);
        ViewModel.Filter[InstrumentBarDataViewModel.FilterToDateTime] = new DateTime(m_endDate.Date.Year, m_endDate.Date.Month, m_endDate.Date.Day, m_endTime.Time.Hours, m_endTime.Time.Minutes, 0);

        //restart load of the items using the new filter conditions
        IncrementalItems.Clear();
        ViewModel.OffsetIndex = 0;

        //load the first page of the bar data asynchronously
        if (DataProvider != string.Empty && Instrument != null) _ = IncrementalItems.LoadMoreItemsAsync(InstrumentBarDataViewModel.DefaultPageSize);
      }
    }

    private void m_resetFilter_Click(object sender, RoutedEventArgs e)
    {
      resetFilter();
    }

    private void refreshFilter()
    {
      lock (m_refreshLock)
      {
        //set the view model filter
        ViewModel.Filter[InstrumentBarDataViewModel.FilterFromDateTime] = new DateTime(m_startDate.Date.Year, m_startDate.Date.Month, m_startDate.Date.Day, m_startTime.Time.Hours, m_startTime.Time.Minutes, 0);
        ViewModel.Filter[InstrumentBarDataViewModel.FilterToDateTime] = new DateTime(m_endDate.Date.Year, m_endDate.Date.Month, m_endDate.Date.Day, m_endTime.Time.Hours, m_endTime.Time.Minutes, 0);

        //restart load of the items using the new filter conditions
        IncrementalItems.Clear();
        ViewModel.OffsetIndex = 0;

        //load the first page of the bar data asynchronously
        if (DataProvider != string.Empty && Instrument != null) _ = IncrementalItems.LoadMoreItemsAsync(InstrumentBarDataViewModel.DefaultPageSize);
      }
    }

    private void m_startDate_DateChanged(object sender, DatePickerValueChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_startTime_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_endDate_DateChanged(object sender, DatePickerValueChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_endTime_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
      refreshFilter();
    }

    private void onViewModelRefresh(object? sender, RefreshEventArgs e)
    {
      //NOTE: Event to refresh will most likely come from a background thread, so we need to marshal the call to the UI thread.
      m_dataTable.DispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() => resetFilter()));
    }
  }
}
