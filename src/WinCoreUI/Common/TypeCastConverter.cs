using Microsoft.UI.Xaml.Data;
using System;

namespace TradeSharp.WinCoreUI.Common
{
	/// <summary>
	/// No operation converter, this would result in a type cast being inserted for the x:Bind operation.
	/// </summary>
  public class TypeCastConverter : IValueConverter
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //delegates


    //events


    //interface implementations


    //properties


    //methods
    public object Convert(object value, Type targetType, object parameter, string language) => value;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
  }
}
