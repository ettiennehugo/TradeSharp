using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Shared utilities for Windows UI.
  /// </summary>
  public class Utilities
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
    public static void populateComboBoxFromEnum(ref ComboBox comboBox, Type enumType)
    {
      comboBox.Items.Clear();

      FieldInfo[] fieldsInfo = enumType.GetFields();

      foreach (var value in Enum.GetValues(enumType))
      {
        string comboValue = value.ToString();

        foreach (FieldInfo field in fieldsInfo)
          if (field.Name == value.ToString())
          {
            DescriptionAttribute? description = (DescriptionAttribute?)field.GetCustomAttribute(typeof(DescriptionAttribute));
            if (description != null)
              comboValue = description.Description;
            break;
          }

        comboBox.Items.Add(comboValue);
      }

      if (comboBox.SelectedIndex == -1) comboBox.SelectedIndex = 0;
    }
  }
}
