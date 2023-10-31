using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts values between a DateTime to DateTimeOffset.
  /// </summary>
  public class DateTimeToDateTimeOffsetConverter : IValueConverter
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      DateTime? concreteValue = (DateTime?)value;
      return concreteValue.HasValue ? new DateTimeOffset(concreteValue.Value) : DateTimeOffset.MaxValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      DateTimeOffset? concreteValue = (DateTimeOffset?)value;
      return concreteValue.HasValue ? concreteValue.Value.DateTime : DateTime.MaxValue;
    }

    //properties


    //methods


  }
}
