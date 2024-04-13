using Microsoft.UI.Xaml.Data;
using Microsoft.Extensions.Logging;
using System;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Convert a log level to a color for display purposes.
  /// </summary>
  public class LogLevelToColorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {

      if (value is LogLevel level)
      {
        return level switch
        {
          LogLevel.Trace => "Black",
          LogLevel.Debug => "Black",
          LogLevel.Information => "Black",
          LogLevel.Warning => "Orange",
          LogLevel.Error => "Red",
          LogLevel.Critical => "DarkRed",
          LogLevel.None => "None",
          _ => "Black"
        };
      }
      return "Black";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      //NOTE: This is a lossy conversion.
      if (value is string logLevel)
      {
        return logLevel switch
        {
          "Black" => LogLevel.Information,
          "Orange" => LogLevel.Warning,
          "Red" => LogLevel.Error,
          "DarkRed" => LogLevel.Critical,
          _ => LogLevel.None
        };
      }
      return LogLevel.None;
    }
  }
}
