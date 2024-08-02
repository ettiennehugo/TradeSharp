using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using Microsoft.UI.Xaml.Data;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts an enum value to it's associated DescriptionAttribute if found, otherwise just returns the enum value as string.
  /// </summary>
  public class EnumValueDescriptionConverter : IValueConverter
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
      bool expandDescriptions = true;
      if (parameter is bool parameterExpandDescriptions) expandDescriptions = parameterExpandDescriptions;

      if (targetType == typeof(string))
      {
        Type type = value.GetType();

        if (type.IsEnum)
          foreach (FieldInfo field in type.GetFields())
            if (field.Name == value.ToString())
            {
              DescriptionAttribute? description = expandDescriptions ? (DescriptionAttribute?)field.GetCustomAttribute(typeof(DescriptionAttribute)) : null;
              return description != null ? description.Description : value.ToString();
            }
      }
      else if (targetType == typeof(int))
        return System.Convert.ChangeType(value, targetType);

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      if (targetType.IsEnum)
      {
        Type valueType = value.GetType();

        if (valueType == typeof(string))
        {
          //take into account DescriptionAttribute
          foreach (FieldInfo field in targetType.GetFields())
          {
            DescriptionAttribute? description = (DescriptionAttribute?)field.GetCustomAttribute(typeof(DescriptionAttribute));
            string stringValue = value.ToString();
            if (stringValue == field.Name || (description != null && stringValue == description.Description))
            {
              var enumValues = Enum.GetValues(targetType);
              foreach (var enumValue in enumValues)
                if (enumValue.ToString() == field.Name) return enumValue;
            }
          }
        }
        else if (valueType == typeof(int))
        {
          int intValue = (int)value;
          Array enumValues = targetType.GetEnumValues();
          return enumValues.GetValue(intValue);
        }          
      }

      return value;
    }
  }
}
