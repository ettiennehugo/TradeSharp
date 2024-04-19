using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Extensions.Logging;

namespace TradeSharp.WinCoreUI.Common
{
  internal class LogLevelConverter : IValueConverter
  {
  public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value is LogLevel logLevel)
      {
        return logLevel switch
        {
          LogLevel.Trace => "Trace",
          LogLevel.Debug => "Debug",
          LogLevel.Information => "Information",
          LogLevel.Warning => "Warning",
          LogLevel.Error => "Error",
          LogLevel.Critical => "Critical",
          LogLevel.None => "None",
          _ => "Unknown"
        };
      }
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      if (value is string logLevel)
      {
        return logLevel switch
        {
           "Trace" => LogLevel.Trace,
           "Debug" => LogLevel.Debug,
           "Information" => LogLevel.Information,
           "Warning" => LogLevel.Warning,
           "Error" => LogLevel.Error,
           "Critical" => LogLevel.Critical,
           "None"  => LogLevel.None,
           _ => LogLevel.None
        };
      }
      return value;
    }
  }
}
