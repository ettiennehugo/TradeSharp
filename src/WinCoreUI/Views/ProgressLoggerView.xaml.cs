using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Log view control for LogEntry type objects.
  /// Based on example from - https://stackoverflow.com/questions/16743804/implementing-a-log-viewer-with-wpf
  /// </summary>
  public sealed partial class ProgressLoggerView : UserControl, IProgressLogger
  {
    //constants


    //enums


    //types
    /// <summary>
    /// Creates a disposable logging scope for nested logging.
    /// </summary>
    public class LoggingScope : IDisposable
    {
      private ProgressLoggerView m_parent;
      private IDisposable? m_loggerScope;

      public LoggingScope(ProgressLoggerView parent, IDisposable loggerScope)
      {
        m_parent = parent;
        m_loggerScope = loggerScope;
      }

      public void Dispose()
      {
        m_parent.m_scopedLogs.Pop();
      }
    }


    //attributes
    internal Stack<CollapsibleLogEntry> m_scopedLogs;

    //constructors
    public ProgressLoggerView()
    {
      m_scopedLogs = new Stack<CollapsibleLogEntry>();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    ILogger? Logger { get; set; }  //logger to echo log entries to
    public ObservableCollection<LogEntry> Entries { get; set; }  //observable collection of log entries

    //methods
    public IDisposable BeginScope(LogLevel level, string message)
    {
      CollapsibleLogEntry logEntry = new CollapsibleLogEntry() { Timestamp = DateTime.Now, Level = level, Message = message };
      //add new scope log entry as a child of the previous collapsible log entry if we are already in a scope or add it to the
      //Entries collection if we are not in a scope push it onto the scoped logs stack to make it the new logging scope
      if (m_scopedLogs.Count > 0)
        m_scopedLogs.Peek().Children.Add(logEntry);
      else
        Entries.Add(logEntry);        
      m_scopedLogs.Push(logEntry);
      return new LoggingScope(this, Logger.BeginScope(message));      
    }

    //Route the new log entries based on the current logging scope
    public void Log(LogLevel level, string message)
    {
      //ensure we're always thread safe in case the progress logger is used by multiple threads
      lock (this)
      {
        LogEntry entry = new LogEntry() { Timestamp = DateTime.Now, Level = level, Message = message };
        if (m_scopedLogs.Count > 0)
          m_scopedLogs.Peek().Children.Add(entry);
        else
          Entries.Add(entry);
        Logger?.Log(level, message);
      }
    }

    public void LogCritical(string message)
    {
      Log(LogLevel.Critical, message);
    }

    public void LogDebug(string message)
    {
      Log(LogLevel.Debug, message);
    }

    public void LogError(string message)
    {
      Log(LogLevel.Error, message);
    }

    public void LogInformation(string message)
    {
      Log(LogLevel.Information, message);
    }

    public void LogWarning(string message)
    {
      Log(LogLevel.Warning, message);
    }
  }
}
