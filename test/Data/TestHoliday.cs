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
    private Mock<IDataManagerService> m_dataManager;
    private Country m_country;

    //constructors
    public TestHoliday()
    {
      m_dataStore = new Mock<IDataStoreService>();
      m_dataManager = new Mock<IDataManagerService>();
      m_country = new Country(m_dataStore.Object, m_dataManager.Object, "en-US");
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    //NOTE: Holiday uses ForYear method to compute the Current property, so we just test the ForYear method here with static dates.
    [TestMethod]
    public void DayOfMonth_NoAdjustForWeekend_Success()
    {
      Holiday holiday = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "test", Months.January, 15, MoveWeekendHoliday.DontAdjust);
      Assert.AreEqual(new DateOnly(2023, 1, 15), holiday.ForYear(2023), "Holiday returned incorrect day for 15 January 2023");
    }

    [TestMethod]
    public void DayOfMonth_AdjustWeekendtoPreviousBusinessDay_Success()
    {
      Holiday holidayPreviousBusinessDay = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "PreviousBusinessDay", Months.January, 15, MoveWeekendHoliday.PreviousBusinessDay);
      //test against Sunday 15 January 2023
      Assert.AreEqual(new DateOnly(2023, 1, 13), holidayPreviousBusinessDay.ForYear(2023), "Adjusted incorrectly for Friday 13 January 2023");

      Holiday holidayNextBusinessDay = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "NextBusinessDay", Months.January, 15, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 16), holidayNextBusinessDay.ForYear(2023), "Adjusted incorrect for Monday 16 January 2023");
    }

    [TestMethod]
    public void NthDayOfNthWeek_NoAdjust_Success()
    {
      Holiday holidayFirstMonday = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "FirstMonday", Months.January, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      Assert.AreEqual(new DateOnly(2023, 1, 2), holidayFirstMonday.ForYear(2023), "Monday of First Week incorrect.");

      Holiday holidayLastMonday = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "LastMonday", Months.January, DayOfWeek.Monday, WeekOfMonth.Last, MoveWeekendHoliday.DontAdjust);
      Assert.AreEqual(new DateOnly(2023, 1, 30), holidayLastMonday.ForYear(2023), "Monday of Last Week incorrect.");
    }

    [TestMethod]
    public void NthDayOfNthWeek_AdjustWeekendToPreviousOrNextBusinessDay_Success()
    {
      Holiday holidayFirstSaturdayPBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "FirstSaturday.PreviousBusinessDay", Months.January, DayOfWeek.Saturday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 6), holidayFirstSaturdayPBD.ForYear(2023), "Saturday PBD of First Week incorrect.");

      Holiday holidayLastSaturdayPBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "LastSaturday.PreviousBusinessDay", Months.January, DayOfWeek.Saturday, WeekOfMonth.Last, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 27), holidayLastSaturdayPBD.ForYear(2023), "Saturday PBD of Last Week incorrect.");

      //1 Janaury 2023 is the first Sunday so adjusting the date to the previous business day would move it into the previous year to 30 December 2022
      Holiday holidayFirstSundayPBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "FirstSaturday.PreviousBusinessDay", Months.January, DayOfWeek.Sunday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2022, 12, 30), holidayFirstSundayPBD.ForYear(2023), "Sunday PBD of First Week incorrect.");

      Holiday holidayLastSundayPBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "LastSaturday.PreviousBusinessDay", Months.January, DayOfWeek.Sunday, WeekOfMonth.Last, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 27), holidayLastSundayPBD.ForYear(2023), "Sunday PBD of Last Week incorrect.");

      Holiday holidayFirstSaturdayNBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "FirstSaturday.NextBusinessDay", Months.January, DayOfWeek.Saturday, WeekOfMonth.First, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 9), holidayFirstSaturdayNBD.ForYear(2023), "Saturday NBD of First Week incorrect.");

      Holiday holidayLastSaturdayNBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "LastSaturday.NextBusinessDay", Months.January, DayOfWeek.Saturday, WeekOfMonth.Last, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 30), holidayLastSaturdayNBD.ForYear(2023), "Saturday NBD of Last Week incorrect.");

      Holiday holidayFirstSundayNBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "FirstSaturday.NextBusinessDay", Months.January, DayOfWeek.Sunday, WeekOfMonth.First, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 2), holidayFirstSundayNBD.ForYear(2023), "Sunday NBD of First Week incorrect.");

      Holiday holidayLastSundayNBD = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "LastSaturday.NextBusinessDay", Months.January, DayOfWeek.Sunday, WeekOfMonth.Last, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 1, 30), holidayLastSundayNBD.ForYear(2023), "Sunday NBD of Last Week incorrect.");
    }

    //contains all other special cases of dates adjusting into the previous month/year and the next month/year
    [TestMethod]
    public void NthDayOfNthWeek_AdjustSpecialCases_Success()
    {
      //1 April 2023 is on a Saturday so adjusting the date to the previous business day would move it into the previous month to 31 March 2023
      Holiday holidayMoveToPreviousMonth = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "HolidayMoveToPreviousMonth", Months.April, DayOfWeek.Saturday, WeekOfMonth.First, MoveWeekendHoliday.PreviousBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 3, 31), holidayMoveToPreviousMonth.ForYear(2023), "Adjusting to previous month for a weekend day is incorrect");

      //30 April 2023 is on a Sunday to adjusting the date to the next buisness day would move it into the next month to 1 May 2023
      Holiday holidayMoveToNextMonth = new Holiday(m_dataStore.Object, m_dataManager.Object, m_country, "HolidayMoveToNExtMonth", Months.April, DayOfWeek.Sunday, WeekOfMonth.Last, MoveWeekendHoliday.NextBusinessDay);
      Assert.AreEqual(new DateOnly(2023, 5, 1), holidayMoveToNextMonth.ForYear(2023), "Adjusting to next month for a weekend day is incorrect");
    }
  }
}