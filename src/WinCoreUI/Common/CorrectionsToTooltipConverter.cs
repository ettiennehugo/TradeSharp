using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Returns the tooltip for the first correction in the list of ILogCorrections.
  /// </summary>
  public class CorrectionsToTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, string language)
    {
      return value != null && value is ILogCorrections logCorrections && logCorrections.Corrections.Count > 0 ? logCorrections.Corrections[0].Tooltip : "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
      throw new NotImplementedException("Converting back is not supported, use OneWay/OneTime binding");
    }
  }

}
