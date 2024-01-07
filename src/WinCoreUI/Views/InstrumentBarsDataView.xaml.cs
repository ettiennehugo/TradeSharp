using System;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.CoreUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;
using System.Threading.Tasks;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Result of an incremental load of bar data.
  /// </summary>
  class IncrementalBarDataLoadResult : IAsyncOperation<LoadMoreItemsResult>
  {
    //constants


    //enums


    //types


    //attributes
    private Task<IList<IBarData>> m_task;

    //constructors
    public IncrementalBarDataLoadResult(Task<IList<IBarData>> task)
    {
      m_task = task;
    }

    //finalizers


    //interface implementations


    //properties
    Exception IAsyncInfo.ErrorCode => m_task.Exception;
    uint IAsyncInfo.Id => (uint)m_task.Id;
    AsyncOperationCompletedHandler<LoadMoreItemsResult> IAsyncOperation<LoadMoreItemsResult>.Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException("Can not set the task completed state."); }
    AsyncStatus IAsyncInfo.Status => throw new NotImplementedException();

    //methods
    void IAsyncInfo.Cancel()
    {
      throw new NotImplementedException();
    }

    void IAsyncInfo.Close()
    {
      throw new NotImplementedException();
    }

    LoadMoreItemsResult IAsyncOperation<LoadMoreItemsResult>.GetResults()
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>
  /// Control to display bar data for a given instrument.
  /// </summary>
  public sealed partial class InstrumentBarsDataView : UserControl, ISupportIncrementalLoading
  {
    //constants


    //enums


    //types


    //attributes
    //NOTE: These start/end dates are very much tied to the Date and Time controls, if these controls are changed
    //      these dates would need to be changed as well to ensure the page works optimally.
    public static DateTime s_defaultStartDateTime = new DateTime(1980, 1, 1, 0, 0, 0);
    public static DateTime s_defaultEndDateTime = new DateTime(2123, 12, 30, 23, 59, 00); //NOTE: The 30 December is correct, for some reason that is the date control maximum when you max it out.
    public static DateTimeOffset s_defaultStartDate = new DateTimeOffset(1980, 1, 1, 12, 0, 0, new TimeSpan(0, 0, 0));
    public static DateTimeOffset s_defaultEndDate = new DateTimeOffset(2123, 12, 31, 0, 0, 0, new TimeSpan(0, 0, 0));
    public static TimeSpan s_defaultStartTime = new TimeSpan(0, 0, 0);
    public static TimeSpan s_defaultEndTime = new TimeSpan(23, 59, 59);

    private int m_index;

    //constructors
    public InstrumentBarsDataView()
    {
      ViewModel = Ioc.Default.GetRequiredService<InstrumentBarDataViewModel>();
      this.InitializeComponent();
      m_index = 0;
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
    public bool HasMoreItems => throw new NotImplementedException();

    //methods
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      

      //Replaced by incremental loading.
      //if (ViewModel.Items.Count == 0) ViewModel.RefreshCommandAsync.ExecuteAsync(null);
      //m_dataTable.ItemsSource = ViewModel.Items;  //filter will be default so display all items
    
      
      //NOTE: resetFilter/refreshFilter runs a lot with the control initialization so no reason to call it here.
      //Common.Utilities.populateComboBoxFromEnum(ref m_priceDataType, typeof(Data.PriceDataType));
    }

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
      int index = m_index;
      m_index += (int)count;
      //if (m_index > ViewModel.Count) m_index = ViewModel.Count;  TODO - reset to count of bars if it exceeds the count number of bars.
      return new IncrementalBarDataLoadResult(ViewModel.GetItems(index, (int)count));
    }

    private void refreshCopyMenu()
    {

      //TODO: Show/hide PriceDataType in the menu based on the filter selector.

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
      m_startDate.Date = s_defaultStartDate;
      m_endDate.Date = s_defaultEndDate;
      m_startTime.Time = s_defaultStartTime;
      m_endTime.Time = s_defaultEndTime;
      m_dataTable.ItemsSource = ViewModel.Items;
    }

    private void m_resetFilter_Click(object sender, RoutedEventArgs e)
    {
      resetFilter();
    }

    private void refreshFilter()
    {
      if (m_dataTable == null) return;

      DateTime startDateTime = new DateTime(m_startDate.Date.Year, m_startDate.Date.Month, m_startDate.Date.Day, m_startTime.Time.Hours, m_startTime.Time.Minutes, 0);
      DateTime endDateTime = new DateTime(m_endDate.Date.Year, m_endDate.Date.Month, m_endDate.Date.Day, m_endTime.Time.Hours, m_endTime.Time.Minutes, 0);

      //filter items down to the selected from/to dates
      if (startDateTime.Equals(s_defaultStartDateTime) && endDateTime.Equals(s_defaultEndDateTime))
      {
        m_dataTable.ItemsSource = ViewModel.Items;
      }
      else
      {
        m_dataTable.ItemsSource = new ObservableCollection<IBarData>(
          from bar in ViewModel.Items
          where bar.DateTime >= startDateTime && bar.DateTime <= endDateTime
          select bar
        );
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
  }
}
