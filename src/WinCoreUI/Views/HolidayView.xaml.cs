using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Holiday definition view used to create/view/update a holiday.
  /// </summary>
  public sealed partial class HolidayView : Page, IWindowedView
  {
    //constants
    public const int Width = 759;
    public const int Height = 607;

    //enums


    //types


    //attributes
    private Guid m_parentId;
    private IHolidayService m_holidayService;

    //constructors
    public HolidayView(Guid parentId, ViewWindow parent)
    {
      m_holidayService = (IHolidayService)IApplication.Current.Services.GetService(typeof(IHolidayService));
      ParentWindow = parent;
      this.InitializeComponent();
      setParentProperties();
      m_parentId = parentId;
      m_name.Text = "New Holiday";
      Holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributes, "", m_parentId, m_name.Text, HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
    }

    public HolidayView(Holiday holiday, ViewWindow parent)
    {
      m_holidayService = (IHolidayService)IApplication.Current.Services.GetService(typeof(IHolidayService));
      ParentWindow = parent;
      this.InitializeComponent();
      setParentProperties();
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

    public ViewWindow ParentWindow { get; private set; }
    public UIElement UIElement => this;

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_holidayType, typeof(HolidayType));
      Common.Utilities.populateComboBoxFromEnum(ref m_month, typeof(Months));
      Common.Utilities.populateComboBoxFromEnum(ref m_dayOfWeek, typeof(DayOfWeek));
      Common.Utilities.populateComboBoxFromEnum(ref m_weekOfMonth, typeof(WeekOfMonth));
      Common.Utilities.populateComboBoxFromEnum(ref m_moveWeekendHoliday, typeof(MoveWeekendHoliday));
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

    private void setParentProperties()
    {
      ParentWindow.View = this;   //need to set this only once the view screen elements are created
      ParentWindow.ResetSizeable();
      ParentWindow.HideMinimizeAndMaximizeButtons();
      ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(Width, Height));
      ParentWindow.CenterWindow();
    }

    private void m_okButton_Click(object sender, RoutedEventArgs e)
    {
      m_holidayService.Update(Holiday);
      ParentWindow.Close();
    }

    private void m_cancelButton_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
