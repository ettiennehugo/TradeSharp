using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to support for the mass import of instrument data.
  /// </summary>
  public interface IMassImportInstrumentDataService
  {
    //constants
    public const string TokenLevel1 = "level1";
    public const string TokenLevel2 = "level2";
    public const string TokenMinute = "minute";
    public const string TokenMinutes = "minutes";
    public const string TokenM1 = "m1";
    public const string TokenHour = "hour";
    public const string TokenHours = "hours";
    public const string TokenHourly = "hourly";
    public const string TokenH = "h";
    public const string TokenDay = "day";
    public const string TokenDays = "days";
    public const string TokenDaily = "daily";
    public const string TokenD = "d";
    public const string TokenWeek = "week";
    public const string TokenWeeks = "weeks";
    public const string TokenWeekly = "weekly";
    public const string TokenW = "w";
    public const string TokenMonth = "month";
    public const string TokenMonths = "months";
    public const string TokenMonthly = "monthly";
    public const string TokenM = "months";

    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    ILogger? Logger { get; set; }
    MassImportSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task Start(CancellationToken cancellationToken = default(CancellationToken));

  }
}
