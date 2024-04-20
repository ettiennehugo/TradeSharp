using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts a reference to a visibility value.
  /// </summary>
  public class ReferenceToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException("Converting back is not supported");
    }
  }
}
