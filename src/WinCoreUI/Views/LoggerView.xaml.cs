using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Template selector for log entries displayed in the tree.
  /// </summary>
  class LogEntryTemplateSelector : DataTemplateSelector
  {
    public DataTemplate LogEntryTemplate { get; set; }
    public DataTemplate CollapsibleLogEntryTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
      //CollapsibleLogEntry is a sub-class of LogEntry for we check against it which template to use
      return item is CollapsibleLogEntry ? CollapsibleLogEntryTemplate : LogEntryTemplate;
    }
  }

  /// <summary>
  /// Log view control for LogEntry type objects.
  /// Based on example from - https://stackoverflow.com/questions/16743804/implementing-a-log-viewer-with-wpf
  /// </summary>
  public sealed partial class LoggerView : UserControl, ILoggerView
  {
    //constants


    //enums


    //types
    /// <summary>
    /// Creates a disposable logging scope for nested logging.
    /// </summary>
    public class LoggingScope : IDisposable
    {
      private LoggerView m_parent;
      private IDisposable? m_loggerScope;

      public LoggingScope(LoggerView parent, IDisposable? loggerScope)
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
    private List<LogEntry> m_entries;
    private bool m_flushToView;
    
    //constructors
    public LoggerView()
    {
      m_entries = new List<LogEntry>();
      m_scopedLogs = new Stack<CollapsibleLogEntry>();
      Entries = new ObservableCollection<LogEntry>();
      m_flushToView = false;    //default to not flush log entries to the view since it can be expensive on the UI thread for long running processes
      this.InitializeComponent();
      //NOTE: The m_logTreeView does not have a binding on its ItemsSource property yet, it's only set in filterEntries to perform fast filtering.
    }

    //finalizers


    //interface implementations


    //properties
    ILogger? Logger { get; set; }  //logger to echo log entries to
    public bool FlushToView { 
      get => m_flushToView;
      set
      {
        if (m_flushToView == value) return;
        m_flushToView = value;
        DispatcherQueue.TryEnqueue(filterEntries);
      }
    }  //flush log entries to the view
    public ObservableCollection<LogEntry> Entries { get; set; }  //observable collection of log entries for display

    //methods
    /// <summary>
    /// Begin a new scope for logging messages.
    /// NOTE: Entries and CollapsibleLog entry in the scopedLog would raise UI updates and must be executed from the UI thread.
    /// </summary>
    public IDisposable BeginScope(string message)
    {
      CollapsibleLogEntry logEntry = new CollapsibleLogEntry() { Timestamp = DateTime.Now, Level = LogLevel.Information, Message = message };
      
      //add new scope log entry as a child of the previous collapsible log entry if we are already in a scope or add it to the
      //Entries collection if we are not in a scope push it onto the scoped logs stack to make it the new logging scope
      if (m_scopedLogs.Count > 0)
        m_scopedLogs.Peek().Children.Add(logEntry);
      else
      {
        m_entries.Add(logEntry);
      }
      m_scopedLogs.Push(logEntry);

      if (FlushToView && filter(logEntry)) DispatcherQueue.TryEnqueue(() => Entries.Add(logEntry));

      IDisposable? loggerScope = Logger is not null ? Logger.BeginScope(message) : null;
      return new LoggingScope(this, loggerScope);      
    }

    /// <summary>
    /// Route the new log entries based on the current logging scope.
    /// NOTE: Entries and CollapsibleLog entry in the scopedLog would raise UI updates and must be executed from the UI thread.
    /// </summary>
    public void Log(LogLevel level, string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "")
    {
      //ensure we're always thread safe in case the progress logger is used by multiple threads
      lock (this)
      {
        LogEntry entry = new LogEntry() { Timestamp = DateTime.Now, Level = level, Message = message, Fix = fix, FixParameter = parameter, FixTooltip = fixTooltip };
       
        //add the log entry to the current scope if we are in a scope or add it to the Entries collection if we are not in a scope
        if (m_scopedLogs.Count > 0)
        {
          var scopedLog = m_scopedLogs.Peek();
          scopedLog.Children.Add(entry);
          if (level > scopedLog.Level) scopedLog.Level = level; //scope adopts the highest log level to make it visible in the logs
        }
        else
          m_entries.Add(entry);

        if (FlushToView && filter(entry)) DispatcherQueue.TryEnqueue(() => Entries.Add(entry));
        Logger?.Log(level, message);
      }
    }

    public void LogCritical(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "")
    {
      Log(LogLevel.Critical, message, fix, parameter, fixTooltip);
    }

    public void LogDebug(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "")
    {
      Log(LogLevel.Debug, message, fix, parameter, fixTooltip);
    }

    public void LogError(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "")
    {
      Log(LogLevel.Error, message, fix, parameter, fixTooltip);
    }

    public void LogInformation(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "")
    {
      Log(LogLevel.Information, message, fix, parameter, fixTooltip);
    }

    public void LogWarning(string message, Action<object?>? fix = null, object? parameter = null, string fixTooltip = "")
    {
      Log(LogLevel.Warning, message, fix, parameter, fixTooltip);
    }

    private bool filter(LogEntry entry)
    {
      string filterText = m_filter.Text.ToUpper();
      bool typeMatch = ((bool)m_toggleInformation.IsChecked! && entry.Level == LogLevel.Information) ||
                       ((bool)m_toggleWarnings.IsChecked! && entry.Level == LogLevel.Warning) ||
                       ((bool)m_toggleError.IsChecked! && entry.Level == LogLevel.Error) ||
                       ((bool)m_toggleCritical.IsChecked! && entry.Level == LogLevel.Critical);
      return typeMatch && entry.Matches(filterText);
    }

    private void filterEntries()
    {
      if (FlushToView)
      {
        //NOTE: We filter in memory to speed up adding and then recreate the binding to the ItemsSource.
        List<LogEntry> entries = new List<LogEntry>();
        foreach (LogEntry entry in m_entries)
          if (filter(entry)) entries.Add(entry);
        Entries = new ObservableCollection<LogEntry>(entries);
        m_logTreeView.SetBinding(TreeView.ItemsSourceProperty, new Binding() { Source = Entries, Mode = BindingMode.OneWay });
      }
    }

    private void m_filter_TextChanged(object sender, TextChangedEventArgs e)
    {
      filterEntries();
    }

    private void toggleLogEntries_Click(object sender, RoutedEventArgs e)
    {
      filterEntries();
    }

    private void fixButton_Click(object sender, RoutedEventArgs e)
    {
      Button button = (Button)sender;
      LogEntry entry = (LogEntry)button.DataContext;
      entry.Fix?.Invoke(entry.FixParameter);
    }
  }
}
