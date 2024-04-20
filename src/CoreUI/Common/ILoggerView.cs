using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Log to record events with potential fixes.
  /// </summary>
  public interface ILoggerView
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    bool FlushToView { get; set; }    //flush log entries to the view as they are added

    //methods
    IDisposable BeginScope(string message);
    void Log(LogLevel level, string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "");
    void LogInformation(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "");
    void LogDebug(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "");
    void LogWarning(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "");
    void LogError(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "");
    void LogCritical(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "");
  }
}
