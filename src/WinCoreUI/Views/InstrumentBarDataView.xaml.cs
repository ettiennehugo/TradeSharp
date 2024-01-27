using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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


    //constructors
    public InstrumentBarDataView()
    {
      BarData = new BarData(Resolution, new DateTime(Date.Year, Date.Month, Date.Day, Time.Hours, Time.Minutes, Time.Seconds), 0, 0, 0, 0, 0);
      this.InitializeComponent();
    }

    public InstrumentBarDataView(IBarData barData)
    {
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
    public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register("Resolution", typeof(Resolution), typeof(InstrumentBarsDataView), new PropertyMetadata(Resolution.Day));
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
          case Resolution.Minute:
            m_date.DayVisible = true;
            m_time.IsEnabled = true;
            break;
          case Resolution.Hour:
            m_date.DayVisible = true;
            m_time.IsEnabled = true;
            break;
          case Resolution.Day:
          case Resolution.Week:
            m_date.DayVisible = true;
            m_time.IsEnabled = false;
            break;
          case Resolution.Month:
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
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
      BarData.DateTime = new DateTime(Date.Year, Date.Month, Date.Day, Time.Hours, Time.Minutes, Time.Seconds, BarData.DateTime.Kind);
    }
  }
}
