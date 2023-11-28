using Microsoft.UI.Xaml.Data;
using System;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts a boolean value to a string value and back.
  /// </summary>
  public class BoolToStringConverter : IValueConverter
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
    public string FalseValue { get; set; }
    public string TrueValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value == null)
        return FalseValue;
      else
        return (bool)value ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      return value != null ? value.Equals(TrueValue) : false;
    }
  }
}
