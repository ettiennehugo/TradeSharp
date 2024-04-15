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
    void Log(LogLevel level, string message);
    void LogInformation(string message);
    void LogDebug(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogCritical(string message);
  }
}
