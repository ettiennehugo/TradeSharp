using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.CoreUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

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


    //constructors
    public InstrumentBarsDataView()
    {
      ViewModel = Ioc.Default.GetRequiredService<InstrumentBarDataViewModel>();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty DataProviderProperty = DependencyProperty.Register("DataProvider", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata(""));
    public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register("Resolution", typeof(Resolution), typeof(InstrumentBarsDataView), new PropertyMetadata(Resolution.Minute));
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

    public Instrument? Instrument { get => (Instrument?)GetValue(InstrumentProperty); set { SetValue(InstrumentProperty, value); ViewModel.Instrument = value; } }
    public string FilterStartTooltip { get => (string)GetValue(FilterStartTooltipProperty); internal set { SetValue(FilterStartTooltipProperty, value); } }
    public string FilterEndTooltip { get => (string)GetValue(FilterEndTooltipProperty); internal set { SetValue(FilterEndTooltipProperty, value); } }
    public InstrumentBarDataViewModel ViewModel { get; internal set; }

    //methods
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommandAsync.ExecuteAsync(null);
      m_dataTable.ItemsSource = ViewModel.Items;
      //NOTE: resetFilter/refreshFilter runs a lot with the control initialization so no reason to call it here.
      Common.Utilities.populateComboBoxFromEnum(ref m_priceDataType, typeof(Data.PriceDataType));
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
          m_copyToSynthetic.Visibility = Visibility.Visible;
          m_copyToActual.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Visible;
          m_copyToDay.Visibility = Visibility.Visible;
          m_copyToWeek.Visibility = Visibility.Visible;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Hour:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToSynthetic.Visibility = Visibility.Visible;
          m_copyToActual.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Visible;
          m_copyToWeek.Visibility = Visibility.Visible;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Day:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToSynthetic.Visibility = Visibility.Visible;
          m_copyToActual.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Collapsed;
          m_copyToWeek.Visibility = Visibility.Visible;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Week:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToSynthetic.Visibility = Visibility.Visible;
          m_copyToActual.Visibility = Visibility.Visible;
          m_copyToHour.Visibility = Visibility.Collapsed;
          m_copyToDay.Visibility = Visibility.Collapsed;
          m_copyToWeek.Visibility = Visibility.Collapsed;
          m_copyToMonth.Visibility = Visibility.Visible;
          break;
        case Resolution.Month:
          m_buttonCopy.Visibility = Visibility.Visible;
          m_copyToSynthetic.Visibility = Visibility.Visible;
          m_copyToActual.Visibility = Visibility.Visible;
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
      m_priceDataType.SelectedIndex = 0;
      m_startDate.Date = new DateTimeOffset(1980, 1, 1, 12, 0, 0, new TimeSpan(0, 0, 0));
      m_endDate.Date = new DateTimeOffset(9999, 12, 31, 0, 0, 0, new TimeSpan(0, 0, 0));
      m_startTime.Time = new TimeSpan(0, 0, 0);
      m_endTime.Time = new TimeSpan(23, 59, 59);
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
      m_dataTable.ItemsSource = new ObservableCollection<IBarData>(
        from bar in ViewModel.Items where bar.DateTime >= startDateTime && bar.DateTime <= endDateTime &&   //filter according to DateTime
                                          (((PriceDataType)m_priceDataType.SelectedIndex) == PriceDataType.Both || (((PriceDataType)m_priceDataType.SelectedIndex) == PriceDataType.Actual && !bar.Synthetic) || (((PriceDataType)m_priceDataType.SelectedIndex) == PriceDataType.Synthetic && bar.Synthetic))    //filter according to PriceDataType selection
        select bar
      );
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

    private void m_priceDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_copyToSynthetic_Click(object sender, RoutedEventArgs e)
    {

    }

    private void m_copyToActual_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
