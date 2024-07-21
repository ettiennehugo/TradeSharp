using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Takes a string list and converts it into a comma separated string and from a comma separated string to a string list.
  /// </summary>
  public class StringListToStringConverter : IValueConverter
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
      if (value is string[] stringArray)
      {
        return string.Join(", ", stringArray);
      }
      else if (value is IList<string> stringList)
      {
        return string.Join(", ", stringList);
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      if (value is string stringValue)
      {
        if (targetType == typeof(List<string>))
        {
          return new List<string>(stringValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        
        if (targetType == typeof(ObservableCollection<string>))
        {
          return new ObservableCollection<string>(stringValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
      }

      return value;
    }
  }
}
