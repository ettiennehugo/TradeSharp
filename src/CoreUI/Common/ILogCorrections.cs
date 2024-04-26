using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Log entry correction/adjustment.
  /// </summary>
  public class LogCorrection
  {
    public LogCorrection()
    {
      Name = string.Empty;
      IsDefault = false;
      Fix = (parameter) => { };
      Parameter = null;
      Tooltip = string.Empty;
    }

    public string Name { get; set; }
    public bool IsDefault { set; get; }
    public Action<object?> Fix { get; set; }
    public object? Parameter { get; set; }
    public string Tooltip { get; set; }
  }

  /// <summary>
  /// Modifiable set of potential corrective/adjustment operations on a log entry.
  /// </summary>
  public interface ILogCorrections
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IList<LogCorrection> Corrections { get; }

    //methods
    void Add(string name, Action<object?> fix, object? parameter, string fixTooltip = "", bool isDefault = false);
  }
}
