using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using System;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Convert a log level to a color for display purposes.
  /// </summary>
  public class LogLevelToColorConverter : IValueConverter
  {
    static private SolidColorBrush s_black;
    static private SolidColorBrush s_orange;
    static private SolidColorBrush s_red;
    static private SolidColorBrush s_darkRed;
    static LogLevelToColorConverter()
    {
      s_black = new SolidColorBrush(Windows.UI.Color.FromArgb(255,0,0,0));
      s_orange = new SolidColorBrush(Windows.UI.Color.FromArgb(255,255,165,0));
      s_red = new SolidColorBrush(Windows.UI.Color.FromArgb(255,255,0,0));
      s_darkRed = new SolidColorBrush(Windows.UI.Color.FromArgb(255,200,0,0));
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value is LogLevel level)
      {
        return level switch
        {
          LogLevel.Trace => s_black,
          LogLevel.Debug => s_black,
          LogLevel.Information => s_black,
          LogLevel.Warning => s_orange,
          LogLevel.Error => s_red,
          LogLevel.Critical => s_darkRed,
          LogLevel.None => s_black,
          _ => s_black
        } ;
      }
      return s_black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      return value;   //does not support two-way conversion
    }
  }
}
