using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.Common;
using static TradeSharp.Data.IDataStoreService;

namespace TradeSharp.Data
{
  /// <summary>
  /// Gregorian calendar months. 
  /// </summary>
  public enum Months
  {
    January = 1,
    Febraury,
    March,
    April,
    May,
    June,
    July,
    August,
    September,
    October,
    November,
    December,
  }

  /// <summary>
  /// Assign numbers to the weeks of the month for holiday computations.
  /// </summary>
  public enum WeekOfMonth
  {
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Last = 5,
  }

  /// <summary>
  /// Define the types of holidays, static day of month vs a specific day of the week.
  /// </summary>
  public enum HolidayType
  {
    DayOfMonth,
    DayOfWeek,
  }

  /// <summary>
  /// Define how holidays are handled that fall on a weekend.
  /// </summary>
  public enum MoveWeekendHoliday
  {
    DontAdjust = 0,
    PreviousBusinessDay,
    NextBusinessDay,
  }

  /// <summary>
  /// Base class for holiday implementations, supports only Gregorian calendar.
  /// </summary>
  public class Holiday : NameObject, IHoliday
  {

    //constants


    //enums


    //types


    //attributes
    protected Months m_month;
    protected int m_dayOfMonth;
    protected DayOfWeek m_dayOfWeek;
    protected WeekOfMonth m_weekOfMonth;

    //constructors
    public Holiday(IDataStoreService dataStore, IDataManagerService dataManager, ICountry country, string name, Months month, int dayOfMonth, MoveWeekendHoliday moveWeekendHoliday) : base(dataStore, dataManager, name)
    {
      if (dayOfMonth <= 0) throw new InvalidDataException("Day of month can not be negative or zero.");
      if (dayOfMonth > 31) throw new InvalidDataException("Day of month can not be larger than 31.");
      Country = country;
      Type = HolidayType.DayOfMonth;
      m_month = month;
      m_dayOfMonth = dayOfMonth;
      MoveWeekendHoliday = moveWeekendHoliday;
    }

    public Holiday(IDataStoreService dataStore, IDataManagerService dataManager, ICountry country, string name, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday) : base(dataStore, dataManager, name)
    {
      Country = country;
      Type = HolidayType.DayOfWeek;
      m_month = month;
      m_dayOfWeek = dayOfWeek;
      m_weekOfMonth = weekOfMonth;
      MoveWeekendHoliday = moveWeekendHoliday;
    }

    public Holiday(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Holiday holiday) : base(dataStore, dataManager, holiday.Name)
    {
      Id = holiday.Id;
      NameTextId = holiday.NameTextId;
      Name = DataStore.GetText(NameTextId);
      Country = CountryInternational.Instance;
      Type = holiday.Type;
      m_month = holiday.Month;
      m_dayOfMonth = holiday.DayOfMonth;
      m_dayOfWeek = holiday.DayOfWeek;
      m_weekOfMonth = holiday.WeekOfMonth;
      MoveWeekendHoliday = holiday.MoveWeekendHoliday;
    }

    //finalizers


    //interface implementations
    public DateOnly ForYear(int year)
    {
      switch (Type)
      {
        case HolidayType.DayOfMonth:
          return getDayOfMonth(year, m_month, m_dayOfMonth);
        case HolidayType.DayOfWeek:
          return getNthDayOfNthWeek(year, m_month, m_dayOfWeek, m_weekOfMonth);
        default:
          throw new NotImplementedException("Unknown holiday type.");
      }
    }

    //properties
    public HolidayType Type { get; }
    public MoveWeekendHoliday MoveWeekendHoliday { get; }
    public DateOnly Current { get { return ForYear(DateTime.Now.Year); } }
    public ICountry Country { get; set; }
    public Months Month { get { return m_month; } }
    public WeekOfMonth WeekOfMonth { get { return m_weekOfMonth; } }
    public int DayOfMonth { get { return m_dayOfMonth; } }
    public DayOfWeek DayOfWeek { get { return m_dayOfWeek; } }

    //methods
    public override bool Equals(object? obj)
    {
      //holidays are name equivalent in a country.
      return obj is Holiday holiday &&
             EqualityComparer<ICountry>.Default.Equals(Country, holiday.Country) &&
             Name.ToUpper() == holiday.Name.ToUpper();
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Country, Name.ToUpper());
    }

    /// <summary>
    /// Adjust computed holiday if it is required for holidays falling over the weekend. 
    /// </summary>
    protected DateTime adjustForWeekend(DateTime dateTime)
    {
      DateTime result = dateTime;

      if (MoveWeekendHoliday != MoveWeekendHoliday.DontAdjust)
      {
        switch (result.DayOfWeek)
        {
          //TBD: This will not always work correctly if you have holidays close together, it will move the holiday to the previsou Friday or next Monday
          //     but if those days are also holidays then it will be incorrect. For now this solution will do, to fix it you'd need to have the notion
          //     of a holiday calendar that is a parent of this holiday object and this holiday object can then refer to it to get the other holidays to
          //     adjust itself correctly.
          case DayOfWeek.Saturday:
            if (MoveWeekendHoliday == MoveWeekendHoliday.PreviousBusinessDay)
              result = result.AddDays(-1);
            else //NextBusinessDay
              result = result.AddDays(2);
            break;
          case DayOfWeek.Sunday:
            if (MoveWeekendHoliday == MoveWeekendHoliday.PreviousBusinessDay)
              result = result.AddDays(-2);
            else //NextMonday
              result = result.AddDays(1);
            break;
        }
      }

      return result;
    }

    /// <summary>
    /// Returns the specific day of the month adjusted for weekends.
    /// </summary>
    protected DateOnly getDayOfMonth(int year, Months month, int day)
    {
      DateTime result = new DateTime(year, (int)month, day);
      result = adjustForWeekend(result);
      return DateOnly.FromDateTime(result);
    }

    /// <summary>
    /// Specify which day of which week of a month and this function will get the date 
    /// this function uses the month and year of the date provided
    /// </summary>
    protected DateOnly getNthDayOfNthWeek(int year, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth)
    {
      DateTime firstOfMonth = new DateTime(year, (int)month, 1); //get first date of month
      DateTime result = firstOfMonth.AddDays(6 - (double)firstOfMonth.AddDays(-((int)dayOfWeek + 1)).DayOfWeek); //get first dayOfWeek of month
      result = result.AddDays(((int)weekOfMonth - 1) * 7);  //get the correct week
      if (result >= firstOfMonth.AddMonths(1)) result = result.AddDays(-7);   //if day is past end of month then adjust backwards a week
      result = adjustForWeekend(result);
      return DateOnly.FromDateTime(result);
    }
  }
}
