using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts a set of ILogCorrections to visibility value. Visible when there are one or more corrections available otherwise collapsed.
  /// </summary>
  public class CorrectionsToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      return value != null && value is ILogCorrections logCorrections && logCorrections.Corrections.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException("Converting back is not supported, use OneWay/OneTime binding");
    }
  }
}
