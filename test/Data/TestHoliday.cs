using Moq;

namespace TradeSharp.Data.Testing
{
  [TestClass]
  public class TestHoliday
  {

    //constants


    //enums


    //types


    //attributes
    private Mock<IDataStoreService> m_dataStore;
    private Country m_country;

    //constructors
    public TestHoliday()
    {
      m_dataStore = new Mock<IDataStoreService>();
      m_country = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, "TagValue", "en-US");
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    //NOTE: Holiday uses ForYear method to compute the Current property, so we just test the ForYear method here with static dates.
    [TestMethod]
    public void DayOfMonth_NoAdjustForWeekend_Success()
    {
      Holiday holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "test", HolidayType.DayOfMonth, Months.January, 15, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      Assert.AreEqual(new DateOnly(2023, 1, 15), holiday.ForYear(2023), "Holiday returned incorrect day for 15 January 2023");
    }

    [TestMethod]
    public void DayOfMonth_AdjustWeekendtoPreviousBusinessDay_Success()
    {
      Holiday holidayPreviousBusinessDay = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "PreviousBusinessDay", HolidayType.DayOfMonth, Months.January, 15, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      //test against Sunday 15 January 2023
      Assert.AreEqual(new DateOnly(2023, 1, 13), holidayPreviousBusinessDay.ForYear(2023), "Adjusted incorrectly for Friday 13 January 2023");

      Holiday holidayNextBusinessDay = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "NextBusinessDay", HolidayType.DayOfMonth, Months.January, 15, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 16), holidayNextBusinessDay.ForYear(2023), "Adjusted incorrect for Monday 16 January 2023");
    }

    [TestMethod]
    public void NthDayOfNthWeek_NoAdjust_Success()
    {
      Holiday holidayFirstMonday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "FirstMonday", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      Assert.AreEqual(new DateOnly(2023, 1, 2), holidayFirstMonday.ForYear(2023), "Monday of First Week incorrect.");

      Holiday holidayLastMonday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "LastMonday", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.Last, MoveWeekendHoliday.DontAdjust);
      Assert.AreEqual(new DateOnly(2023, 1, 30), holidayLastMonday.ForYear(2023), "Monday of Last Week incorrect.");
    }

    [TestMethod]
    public void NthDayOfNthWeek_AdjustWeekendToPreviousOrNextBusinessDay_Success()
    {
      Holiday holidayFirstSaturdayPBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "FirstSaturday.PreviousBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Saturday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 6), holidayFirstSaturdayPBD.ForYear(2023), "Saturday PBD of First Week incorrect.");

      Holiday holidayLastSaturdayPBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "LastSaturday.PreviousBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Saturday, WeekOfMonth.Last, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 27), holidayLastSaturdayPBD.ForYear(2023), "Saturday PBD of Last Week incorrect.");

      //1 Janaury 2023 is the first Sunday so adjusting the date to the previous business day would move it into the previous year to 30 December 2022
      Holiday holidayFirstSundayPBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "FirstSaturday.PreviousBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Sunday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2022, 12, 30), holidayFirstSundayPBD.ForYear(2023), "Sunday PBD of First Week incorrect.");

      Holiday holidayLastSundayPBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "LastSaturday.PreviousBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Sunday, WeekOfMonth.Last, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 27), holidayLastSundayPBD.ForYear(2023), "Sunday PBD of Last Week incorrect.");

      Holiday holidayFirstSaturdayNBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "FirstSaturday.NextBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Saturday, WeekOfMonth.First, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 9), holidayFirstSaturdayNBD.ForYear(2023), "Saturday NBD of First Week incorrect.");

      Holiday holidayLastSaturdayNBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "LastSaturday.NextBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Saturday, WeekOfMonth.Last, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 30), holidayLastSaturdayNBD.ForYear(2023), "Saturday NBD of Last Week incorrect.");

      Holiday holidayFirstSundayNBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "FirstSaturday.NextBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Sunday, WeekOfMonth.First, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 2), holidayFirstSundayNBD.ForYear(2023), "Sunday NBD of First Week incorrect.");

      Holiday holidayLastSundayNBD = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "LastSaturday.NextBusinessDay", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Sunday, WeekOfMonth.Last, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 30), holidayLastSundayNBD.ForYear(2023), "Sunday NBD of Last Week incorrect.");
    }

    //contains all other special cases of dates adjusting into the previous month/year and the next month/year
    [TestMethod]
    public void NthDayOfNthWeek_AdjustSpecialCases_Success()
    {
      //1 April 2023 is on a Saturday so adjusting the date to the previous business day would move it into the previous month to 31 March 2023
      Holiday holidayMoveToPreviousMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "HolidayMoveToPreviousMonth", HolidayType.DayOfWeek, Months.April, 1, DayOfWeek.Saturday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 3, 31), holidayMoveToPreviousMonth.ForYear(2023), "Adjusting to previous month for a weekend day is incorrect");

      //30 April 2023 is on a Sunday to adjusting the date to the next buisness day would move it into the next month to 1 May 2023
      Holiday holidayMoveToNextMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "HolidayMoveToNExtMonth", HolidayType.DayOfWeek, Months.April, 1, DayOfWeek.Sunday, WeekOfMonth.Last, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 5, 1), holidayMoveToNextMonth.ForYear(2023), "Adjusting to next month for a weekend day is incorrect");
    }
  }
}