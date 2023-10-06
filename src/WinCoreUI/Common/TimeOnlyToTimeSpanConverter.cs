using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converter for TimeOnly to TimeSpan converter and back. 
  /// </summary>
  class TimeOnlyToTimeSpanConverter : IValueConverter
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
      return TimeSpan.FromTicks(((TimeOnly)value).Ticks);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      return TimeOnly.FromTimeSpan((TimeSpan)value);
    }

    //properties


    //methods


  }
}
