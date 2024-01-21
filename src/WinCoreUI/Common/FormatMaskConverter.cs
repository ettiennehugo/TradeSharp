using System;
using Microsoft.UI.Xaml.Data;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Value converter that applies a format mask to an output value.
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
