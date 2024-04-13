using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Observable log entry to display. 
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
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] private DateTime m_timestamp;
    [ObservableProperty] private LogLevel m_level;
    [ObservableProperty] private string m_message;

    //methods


  }
}
