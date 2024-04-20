using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Log to record progress of a long running task.
  /// </summary>
  public interface IProgressLogger
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    IDisposable BeginScope(string message);
    void Log(LogLevel level, string message, Action<object?>? fix = null, object? parameter = null);
    void LogInformation(string message, Action<object?>? fix = null, object? parameter = null);
    void LogDebug(string message, Action<object?>? fix = null, object? parameter = null);
    void LogWarning(string message, Action<object?>? fix = null, object? parameter = null);
    void LogError(string message, Action<object?>? fix = null, object? parameter = null);
    void LogCritical(string message, Action<object?>? fix = null, object? parameter = null);
  }
}
