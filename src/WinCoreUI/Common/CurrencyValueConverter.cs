using System;
using Microsoft.UI.Xaml.Data;
using System.Globalization;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Converts the value to a currency string based on the culture information.
  /// </summary>
  public class CurrencyValueConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      if (value == null) return null;

      //get amount value
      decimal amount = System.Convert.ToDecimal(value);

      //default to 2 decimal places and try different input parameter types
      int decimalDigits = 2;
      if (parameter is int parameterDigits)
        decimalDigits = parameterDigits;
      else if (parameter is CultureInfo cultureInfo)
        decimalDigits = cultureInfo.NumberFormat.CurrencyDecimalDigits;
      return Math.Round(amount, decimalDigits).ToString("N" + decimalDigits, CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException();
    }
  }
}


//public string FormatValueToAccountCurrency(decimal value)
//{
//  int decimalPlaces = Account.Currency.DecimalPlaces;
//  string format = $"F{decimalPlaces}";
//  return value.ToString(format);
//}