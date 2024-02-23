﻿using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.UI.Xaml;

namespace TradeSharp.WinCoreUI.Common
{
  class BooleanToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value is bool && (bool)value)
        return Visibility.Visible;
      return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      return value is Visibility && (Visibility)value == Visibility.Visible;
    }
  }
}
