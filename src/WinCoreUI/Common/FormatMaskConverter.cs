using System;
using Microsoft.UI.Xaml.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Value converter that applies a format mask to an output value.
  /// NOTE: XAML does NOT support binding to the ConverterParameter, to bind to the format mask parameter use a IMultiValueConverter instead that allows binding to multiple parameters
  ///       the first input parameter should be the value to convert and the second input parameter should be the format mask.
  /// </summary>
  public class FormatMaskConverter : IValueConverter
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
      string valueString = value.ToString();
      string formatMask = parameter.ToString();
      return string.Format(formatMask, valueString);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException();
    }
  }


}
