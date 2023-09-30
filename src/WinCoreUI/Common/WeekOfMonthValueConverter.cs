using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts the SelectedIndex of a ComboBox to a WeekOfMonth enumeration value by adding/subtracting 1.
  /// </summary>
  public class WeekOfMonthValueConverter : IValueConverter
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
      return (int)value - 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      return (int)value + 1;
    }

    //properties


    //methods


  }
}
