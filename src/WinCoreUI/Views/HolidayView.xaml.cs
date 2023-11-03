using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TradeSharp.Common;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TradeSharp.Data;
using TradeSharp.WinCoreUI.Common;
using System.ComponentModel;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Holiday definition view used to create/view/update a holiday.
  /// </summary>
  public sealed partial class HolidayView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private Guid m_parentId;

    //constructors
    public HolidayView(Guid parentId)
    {
      this.InitializeComponent();
      m_parentId = parentId;
      m_name.Text = "New Holiday";
      Holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_parentId, m_name.Text, HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
    }

    public HolidayView(Holiday holiday)
    {
      this.InitializeComponent();
      m_parentId = holiday.ParentId;
      Holiday = holiday;
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_holidayProperty = DependencyProperty.Register("Holiday", typeof(Holiday), typeof(HolidayView), new PropertyMetadata(null));
    public Holiday? Holiday
    {
      get => (Holiday?)GetValue(s_holidayProperty);
      set => SetValue(s_holidayProperty, value);
    }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Utilities.populateComboBoxFromEnum(ref m_holidayType, typeof(HolidayType));
      Utilities.populateComboBoxFromEnum(ref m_month, typeof(Months));
      Utilities.populateComboBoxFromEnum(ref m_dayOfWeek, typeof(DayOfWeek));
      Utilities.populateComboBoxFromEnum(ref m_weekOfMonth, typeof(WeekOfMonth));
      Utilities.populateComboBoxFromEnum(ref m_moveWeekendHoliday, typeof(MoveWeekendHoliday));
    }

    private void m_holidayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      switch (m_holidayType.SelectedIndex)
      {
        case (int)HolidayType.DayOfMonth:
          m_dayOfMonth.IsEnabled = true;
          m_dayOfWeek.IsEnabled = false;
          m_weekOfMonth.IsEnabled = false;
          break;
        case (int)HolidayType.DayOfWeek:
          m_dayOfMonth.IsEnabled = false;
          m_dayOfWeek.IsEnabled = true;
          m_weekOfMonth.IsEnabled = true;
          break;
      }
    }

    private void m_month_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_dayOfMonth.Items.Clear();
      int selectedDay = m_dayOfMonth.SelectedIndex;
      int days = DateTime.DaysInMonth(2000, m_month.SelectedIndex + 1); //just pick any leap year for February max
      for (int day = 1; day <= days; day++) m_dayOfMonth.Items.Add(day.ToString());
      if (selectedDay + 1 >= days) m_dayOfMonth.SelectedIndex = days;
      if (selectedDay == -1) m_dayOfMonth.SelectedIndex = 0;
    }
  }
}
