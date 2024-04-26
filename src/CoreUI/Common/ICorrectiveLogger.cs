using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Log to record events with potential corrections/adjustments to issues found.
  /// As a general rule, ILogCorrections with only one entry will be displayed as a normal button with the FixToolTip while
  /// multiple entries will be displayed as a drop-down button with the names of the fixes and the FixToolTip.
  /// </summary>
  public interface ICorrectiveLogger
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    IDisposable BeginScope(string message);
    ILogCorrections Log(LogLevel level, string message);
    ILogCorrections LogInformation(string message);
    ILogCorrections LogDebug(string message);
    ILogCorrections LogWarning(string message);
    ILogCorrections LogError(string message);
    ILogCorrections LogCritical(string message);
  }
}
