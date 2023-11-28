using System;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.CoreUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;


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
    public static readonly DependencyProperty TickerProperty = DependencyProperty.Register("Ticker", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata(""));
    public static readonly DependencyProperty InstrumentProperty = DependencyProperty.Register("Instrument", typeof(Instrument), typeof(InstrumentBarsDataView), new PropertyMetadata(null));
    public static readonly DependencyProperty StartProperty = DependencyProperty.Register("Start", typeof(DateTime), typeof(InstrumentBarsDataView), new PropertyMetadata(DateTime.MinValue));
    public static readonly DependencyProperty EndProperty = DependencyProperty.Register("End", typeof(DateTime), typeof(InstrumentBarsDataView), new PropertyMetadata(DateTime.MaxValue));
    public static readonly DependencyProperty PriceDataTypeProperty = DependencyProperty.Register("PriceDataType", typeof(DateTime), typeof(InstrumentBarsDataView), new PropertyMetadata(PriceDataType.Both));
    public static readonly DependencyProperty FilterStartTooltipProperty = DependencyProperty.Register("FilterStartTooltip", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata("Filter start date/time"));
    public static readonly DependencyProperty FilterEndTooltipProperty = DependencyProperty.Register("FilterEndTooltip", typeof(string), typeof(InstrumentBarsDataView), new PropertyMetadata("Filter end date/time"));

    public string DataProvider { get => (string)GetValue(DataProviderProperty); set { SetValue(DataProviderProperty, value); ViewModel.DataProvider = value; } }
    
    public Resolution Resolution 
    { 
      get => (Resolution)GetValue(ResolutionProperty); 
      set 
      { 
        SetValue(ResolutionProperty, value); 
        ViewModel.Resolution = value;

        //configure date/time controls based on resolution state        
        switch (value)
        {
          case Resolution.Level1:
            throw new ArgumentException("Level1 resolution not supported by bar data view, use view for level1 data.");
          case Resolution.Minute:
            FilterStartTooltip = "Filter start date/time";
            FilterEndTooltip = "Filter end date/time";
            m_startDate.DayVisible = true;
            m_endDate.DayVisible = true;
            m_startTime.Visibility = Visibility.Visible;
            m_endTime.Visibility = Visibility.Visible;
            break;
          case Resolution.Hour:
            FilterStartTooltip = "Filter start date/hour";
            FilterEndTooltip = "Filter end date/hour";
            m_startDate.DayVisible = true;
            m_endDate.DayVisible = true;
            m_startTime.Visibility = Visibility.Visible;
            m_endTime.Visibility = Visibility.Visible;
            break;
          case Resolution.Day:
          case Resolution.Week:
            FilterStartTooltip = "Filter start date";
            FilterEndTooltip = "Filter end date";
            m_startDate.DayVisible = true;
            m_endDate.DayVisible = true;
            m_startTime.Visibility = Visibility.Collapsed;
            m_endTime.Visibility = Visibility.Collapsed;
            break;
          case Resolution.Month:
            FilterStartTooltip = "Filter start date";
            FilterEndTooltip = "Filter end date";
            m_startDate.DayVisible = false;
            m_endDate.DayVisible = false;
            m_startTime.Visibility = Visibility.Collapsed;
            m_endTime.Visibility = Visibility.Collapsed;
            break;
        }

        resetFilter();
      }
    }
    
    public Instrument? Instrument { get => (Instrument?)GetValue(InstrumentProperty); set { SetValue(InstrumentProperty, value); ViewModel.Instrument = value; } }
    public string Ticker { get => (string)GetValue(TickerProperty); set { SetValue(TickerProperty, value); ViewModel.Ticker = value; } }
    public DateTime Start { get => (DateTime)GetValue(StartProperty); set { SetValue(StartProperty, value); ViewModel.Start = value; } }
    public DateTime End { get => (DateTime)GetValue(EndProperty); set { SetValue(EndProperty, value); ViewModel.Start = value; } }
    public PriceDataType PriceDataType { get => (PriceDataType)GetValue(PriceDataTypeProperty); set { SetValue(PriceDataTypeProperty, value); ViewModel.PriceDataType = value; } }
    public string FilterStartTooltip { get => (string)GetValue(FilterStartTooltipProperty); internal set { SetValue(FilterStartTooltipProperty, value); } }
    public string FilterEndTooltip { get => (string)GetValue(FilterEndTooltipProperty); internal set { SetValue(FilterEndTooltipProperty, value); } }
    public InstrumentBarDataViewModel ViewModel { get; internal set; }

    private void resetFilter(bool resetDate = true, bool resetTime = true)
    {
      if (resetDate)
      {
        m_startDate.Date = new DateTimeOffset(1980, 1, 1, 12, 0, 0, new TimeSpan(0, 0, 0));    //TBD: Should we create config for these low/high dates used for filters - it can inadvertantly created bugs since date/time pickers do not allow arbutrary values but are clipped by magic numbers.
        m_endDate.Date = new DateTimeOffset(9999, 12, 31, 0, 0, 0, new TimeSpan(0, 0, 0));
      }

      if (resetTime)
      {
        m_startTime.Time = new TimeSpan(0, 0, 0);
        m_endTime.Time = new TimeSpan(23, 59, 59);
      }
    }

    private void m_resetFilter_Click(object sender, RoutedEventArgs e)
    {
      resetFilter();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      resetFilter();
    }

    //methods


  }
}
