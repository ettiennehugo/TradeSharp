using System.ComponentModel;

namespace TradeSharp.Data
{

  /// <summary>
  /// Gregorian calendar months. 
  /// </summary>
  public enum Months
  {
    January = 1,
    February,
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
    [Description("Day of Month")]
    DayOfMonth,
    [Description("Day of Week")]
    DayOfWeek,
  }

  /// <summary>
  /// Define how holidays are handled that fall on a weekend.
  /// </summary>
  public enum MoveWeekendHoliday
  {
    [Description("Don't Adjust")]
    DontAdjust,
    [Description("Previous Business Day")]
    PreviousBusinessDay,
    [Description("Next Business Day")]
    NextBusinessDay,
  }

  /// <summary>
  /// Different types of resolutions that can be supported by a data provider/data feed. 
  /// </summary>
  public enum Resolution 
  {
    Minute,
    Hour,
    Day,
    Week,
    Month,
    Level1,   //tick data
    //Level2,   //order book data - currently not supported
  }
}
