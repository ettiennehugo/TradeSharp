using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Observable log entry with optional fix action.
  /// </summary>
  public partial class LogEntry: ObservableObject
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public LogEntry()
    {
      Timestamp = DateTime.Now;
      Level = LogLevel.Information;
      Message = "";
      Fix = null;
      FixParameter = null;
      FixTooltip = "";
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private DateTime m_timestamp;
    [ObservableProperty] private LogLevel m_level;
    [ObservableProperty] private string m_message;
    public Action<object?>? Fix { get; set; }
    public object? FixParameter { get; set; }
    [ObservableProperty] private string m_fixTooltip;

    //methods
    public virtual bool Matches(string filterText)
    {
      return filterText.Length == 0 || Message.ToUpper().Contains(filterText);
    }
  }
}
