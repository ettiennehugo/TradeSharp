using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentBarDataView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private bool m_dateTimeInitialized;

    //constructors
    public InstrumentBarDataView()
    {
      this.InitializeComponent();
      BarData = new BarData(Resolution, DateTime.Now, Constants.DefaultPriceFormatMask, 0, 0, 0, 0, 0);
    }

    public InstrumentBarDataView(IBarData barData)
    {
      m_dateTimeInitialized = false;
      BarData = barData;
      this.InitializeComponent();
      Resolution = barData.Resolution;
      //user can not edit the DateTime of an existing bar (copy can be used to move data)
      m_date.IsEnabled = false;
      m_time.IsEnabled = false;
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register("Resolution", typeof(Resolution), typeof(InstrumentBarsDataView), new PropertyMetadata(Resolution.Days));
    public static readonly DependencyProperty DateProperty = DependencyProperty.Register("Date", typeof(DateTimeOffset), typeof(InstrumentBarsDataView), new PropertyMetadata(new DateTimeOffset(1980, 1, 1, 12, 0, 0, new TimeSpan(0, 0, 0))));
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(TimeSpan), typeof(InstrumentBarsDataView), new PropertyMetadata(new TimeSpan(0, 0, 0)));

    public Resolution Resolution
    {
      get => (Resolution)GetValue(ResolutionProperty);
      set
      {
        SetValue(ResolutionProperty, value);

        //configure date/time controls based on resolution state        
        switch (value)
        {
          case Resolution.Level1:
            throw new ArgumentException("Level1 resolution not supported by bar data view, use view for level1 data.");
          case Resolution.Minutes:
            m_date.DayVisible = true;
            m_time.IsEnabled = true;
            break;
          case Resolution.Hours:
            m_date.DayVisible = true;
            m_time.IsEnabled = true;
            break;
          case Resolution.Days:
          case Resolution.Weeks:
            m_date.DayVisible = true;
            m_time.IsEnabled = false;
            break;
          case Resolution.Months:
            m_date.DayVisible = false;
            m_time.IsEnabled = false;
            break;
        }
      }
    }

    public IBarData BarData { get; set; }
    public DateTimeOffset Date { get => (DateTimeOffset)GetValue(DateProperty); set => SetValue(DateProperty, value); }
    public TimeSpan Time { get => (TimeSpan)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Date = BarData.DateTime;
      Time = BarData.DateTime.TimeOfDay;
      //IsLoading is already set at the beginning of this method and the modification of Date above would fire the change below
      //causing corruption of the BarData.DateTime to the initial Time field value
      m_dateTimeInitialized = true;
    }

    private void m_date_DateChanged(object sender, DatePickerValueChangedEventArgs e)
    {
      if (!m_dateTimeInitialized) return;  //don't modify date/time if not initialized, otherwise it assigns the control initial values
      BarData.DateTime = new DateTime(Date.Year, Date.Month, Date.Day, Time.Hours, Time.Minutes, Time.Seconds, BarData.DateTime.Kind);
    }

    private void m_time_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
      if (!m_dateTimeInitialized) return;  //don't modify date/time if not initialized, otherwise it assigns the control initial values
      BarData.DateTime = new DateTime(Date.Year, Date.Month, Date.Day, Time.Hours, Time.Minutes, Time.Seconds, BarData.DateTime.Kind);
    }
  }
}
