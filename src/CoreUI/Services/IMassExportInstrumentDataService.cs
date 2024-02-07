using Microsoft.Extensions.Logging;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to support for the mass export of instrument data.
  /// </summary>
  public interface IMassExportInstrumentDataService
  {
    //constants
    public const string TokenMinute = "minute";
    public const string TokenHour = "hour";
    public const string TokenDay = "day";
    public const string TokenWeek = "week";
    public const string TokenMonth = "month";

    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    ILogger? Logger { get; set; }
    MassExportSettings Settings { get; set; }
    public bool IsRunning { get; }

    //methods
    Task Start(CancellationToken cancellationToken = default(CancellationToken));
  }
}
