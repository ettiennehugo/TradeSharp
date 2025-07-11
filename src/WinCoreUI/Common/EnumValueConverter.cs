﻿using System;
using Microsoft.UI.Xaml.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Value converter for enums to integer and back.
  /// </summary>
  public class EnumValueConverter : IValueConverter
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties


    //methods
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value.GetType().IsEnum)
        return (int)value;
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      if (value is int && targetType.IsEnum)
      {
          int intValue = (int)value;
          Array enumValues = targetType.GetEnumValues();
          return enumValues.GetValue(intValue);
      }
      else
        return value;
    }
  }
}
