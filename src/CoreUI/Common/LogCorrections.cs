namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Set of corrective/adjustment operations on a log entry.
  /// </summary>
  public class LogCorrections : ILogCorrections
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public LogCorrections()
    {
      Corrections = new List<LogCorrection>();
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public IList<LogCorrection> Corrections { get; protected set; }
    public void Add(string name, Action<object?> fix, object? parameter, string fixTooltip = "", bool isDefault = false)
    {
      Corrections.Add(new LogCorrection { Name = name, Fix = fix, Parameter = parameter, Tooltip = fixTooltip, IsDefault = isDefault });
    }
  }
}
