namespace TradeSharp.Data
{
  /// <summary>
  /// Base interface for holidays. 
  /// </summary>
  public interface IHoliday
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    Guid Id { get; }
    ICountry Country { get; internal set; }
    DateOnly Current { get; }
    int DayOfMonth { get; }
    DayOfWeek DayOfWeek { get; }
    Months Month { get; }
    MoveWeekendHoliday MoveWeekendHoliday { get; }
    HolidayType Type { get; }
    WeekOfMonth WeekOfMonth { get; }

    //methods
    DateOnly ForYear(int year);
  }
}