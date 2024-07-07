using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts a country RegionInfo object into a display name.
  /// </summary>
  public class RegionInfoToDisplayNameConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value is RegionInfo regionInfo)
        return $"{regionInfo.Name} - {regionInfo.EnglishName}";
      return "<No display value>";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException();
    }
  }
}
