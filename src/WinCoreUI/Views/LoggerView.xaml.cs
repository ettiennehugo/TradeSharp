using Microsoft.UI.Xaml.Controls;
using System;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using TradeSharp.WinCoreUI.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.Views;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Template selector for log entries displayed in the tree.
  /// </summary>
  class LogEntryTemplateSelector : DataTemplateSelector
  {
    public DataTemplate LogEntrySingleCorrectionTemplate { get; set; }
    public DataTemplate LogEntryMultiCorrectionTemplate { get; set; }
    public DataTemplate CollapsibleLogEntryTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
      LogEntryDecorator logEntryDecorator = (LogEntryDecorator)item;  
      if (logEntryDecorator.Entry is CollapsibleLogEntry)
        return CollapsibleLogEntryTemplate;
      else
        return logEntryDecorator.Entry.Corrections.Corrections.Count > 1 ? LogEntryMultiCorrectionTemplate : LogEntrySingleCorrectionTemplate;
    }
  }

  /// <summary>
  /// Decorator for log entries to add additional screen specific functionality.
  /// </summary>
  public partial class LogEntryDecorator : ObservableObject
  {
    public LogEntryDecorator(LogEntry entry)
    {
      Entry = entry;

      switch (entry.Corrections.Corrections.Count)
      {
        case 0:
          Tooltip = "No corrections available";
          break;
        case 1:
          Tooltip = entry.Corrections.Corrections[0].Tooltip;
          break;
        default:
          Tooltip = "Select correction to apply";
          break;
      }
    }

    //properties
    [ObservableProperty] LogEntry m_entry;
    [ObservableProperty] string m_tooltip;
  }

  /// <summary>
  /// Log view control for LogEntry type objects.
  /// Based on example from - https://stackoverflow.com/questions/16743804/implementing-a-log-viewer-with-wpf
  /// </summary>
  public sealed partial class LoggerView : UserControl, ICorrectiveLogger, IIncrementalSource<LogEntryDecorator>
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
    private int m_lastReturnedIndex;
    private MenuFlyout m_correctionsMenuFlyout;
    IDialogService m_dialogService;

    //constructors
    public LoggerView()
    {
      m_entries = new List<LogEntry>();
      m_scopedLogs = new Stack<CollapsibleLogEntry>();
      Entries = new IncrementalObservableCollection<LogEntryDecorator>(this);
      m_dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IsLoading = false;
      HasMoreItems = true;
      this.InitializeComponent();
      //NOTE: The m_logView does not have a binding on its ItemsSource property yet, it's only set in filterEntries to perform fast filtering.
      m_correctionsMenuFlyout = Resources["CorrectionsMenuFlyout"] as MenuFlyout;
    }

    //finalizers


    //interface implementations


    //properties
    ILogger? Logger { get; set; }  //logger to echo log entries to
    public IncrementalObservableCollection<LogEntryDecorator> Entries { get; set; }  //observable collection of log entries for display
    public bool HasMoreItems { get; internal set; }
    public bool IsLoading;
    public int UnfilteredEntryCount { get => m_entries.Count; }

    //methods
    /// <summary>
    /// Begin a new scope for logging messages.
    /// NOTE: Entries and CollapsibleLog entry in the scopedLog would raise UI updates and must be executed from the UI thread.
    /// </summary>
    public IDisposable BeginScope(string message)
    {
      CollapsibleLogEntry entry = new CollapsibleLogEntry() { Timestamp = DateTime.Now, Level = LogLevel.Information, Message = message };

      lock (this)
      {
        //add new scope log entry as a child of the previous collapsible log entry if we are already in a scope or add it to the
        //Entries collection if we are not in a scope push it onto the scoped logs stack to make it the new logging scope
        if (m_scopedLogs.Count > 0)
          m_scopedLogs.Peek().Children.Add(entry);
        else
        {
          m_entries.Add(entry);
        }
        m_scopedLogs.Push(entry);

        //update the incremental loading state
        HasMoreItems = m_lastReturnedIndex < m_entries.Count;
      }

      IDisposable? loggerScope = Logger is not null ? Logger.BeginScope(message) : null;
      return new LoggingScope(this, loggerScope);
    }

    /// <summary>
    /// Adds the given entry to the log entries collection.
    /// </summary>
    public void Add(LogEntry entry)
    {
      lock(this)
      {
        m_entries.Add(entry);
        //update the incremental loading state
        HasMoreItems = m_lastReturnedIndex < m_entries.Count;
      }
    }

    /// <summary>
    /// Clear all log entries.
    /// </summary>
    public void Clear()
    {
      lock(this)
      {
        m_entries.Clear();
        m_lastReturnedIndex = 0;
        HasMoreItems = false;
      }
    }

    /// <summary>
    /// Route the new log entries based on the current logging scope.
    /// NOTE: Entries and CollapsibleLog entry in the scopedLog would raise UI updates and must be executed from the UI thread.
    /// </summary>
    public ILogCorrections Log(LogLevel level, string message)
    {
      //ensure we're always thread safe in case the progress logger is used by multiple threads
      LogEntry entry = new LogEntry() { Timestamp = DateTime.Now, Level = level, Message = message };

      lock (this)
      {
        //add the log entry to the current scope if we are in a scope or add it to the Entries collection if we are not in a scope
        if (m_scopedLogs.Count > 0)
        {
          var scopedLog = m_scopedLogs.Peek();
          scopedLog.Children.Add(entry);
          if (level > scopedLog.Level) scopedLog.Level = level; //scope adopts the highest log level to make it visible in the logs
        }
        else
          m_entries.Add(entry);

        Logger?.Log(level, message);

        //update the incremental loading state
        HasMoreItems = m_lastReturnedIndex < m_entries.Count;

        //need to force a refresh to make sure log view remains in sync with new entries
        //being added to the collection otherwise log view goes out of sync with m_entries
        //collection
        DispatcherQueue.TryEnqueue(() => m_logView.LoadMoreItemsAsync());
      }

      return entry.Corrections;
    }

    public ILogCorrections LogCritical(string message)
    {
      return Log(LogLevel.Critical, message);
    }

    public ILogCorrections LogDebug(string message)
    {
      return Log(LogLevel.Debug, message);
    }

    public ILogCorrections LogError(string message)
    {
      return Log(LogLevel.Error, message);
    }

    public ILogCorrections LogInformation(string message)
    {
      return Log(LogLevel.Information, message);
    }

    public ILogCorrections LogWarning(string message)
    {
      return Log(LogLevel.Warning, message);
    }

    /// <summary>
    /// Synchronously read the filter data from the UI for use in a background thread.
    /// </summary>
    private void readUIFilters(out string filterText, out bool includeInformation, out bool includeWarnings, out bool includeError, out bool includeCritical)
    {
      string filterTextInt = filterText = "";
      bool includeInformationInt = includeInformation = false;
      bool includeWarningsInt = includeWarnings = false;
      bool includeErrorInt = includeError = false;
      bool includeCriticalInt = includeCritical = false;

      TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
      DispatcherQueue.TryEnqueue(() =>
      {
        filterTextInt = m_filter.Text.ToUpper();
        includeInformationInt = (bool)m_toggleInformation.IsChecked!;
        includeWarningsInt = (bool)m_toggleWarnings.IsChecked!;
        includeErrorInt = (bool)m_toggleError.IsChecked!;
        includeCriticalInt = (bool)m_toggleCritical.IsChecked!;
        taskCompletionSource.SetResult(true);
      });
      taskCompletionSource.Task.Wait();

      filterText = filterTextInt;
      includeInformation = includeInformationInt;
      includeWarnings = includeWarningsInt;
      includeError = includeErrorInt;
      includeCritical = includeCriticalInt;
    }

    /// <summary>
    /// Resets the incremental loading state to reload the log entries, used when filters are changed.
    /// </summary>
    private void reloadEntries()
    {
      Entries.Clear();
      Entries.RefreshAsync();
      HasMoreItems = true;
      m_lastReturnedIndex = 0;
    }

    private void m_filter_TextChanged(object sender, TextChangedEventArgs e)
    {
      reloadEntries();
    }

    private void toggleLogEntries_Click(object sender, RoutedEventArgs e)
    {
      reloadEntries();
    }

    /// <summary>
    /// Handler for log entries that only have a single correction associated with it.
    /// </summary>
    private void fixEntryClick(object sender, RoutedEventArgs e)
    {
      Button button = (Button)sender;
      LogEntry entry = (LogEntry)button.DataContext;
      entry.Corrections.Corrections[0].Fix?.Invoke(entry.Corrections.Corrections[0].Parameter);
    }

    /// <summary>
    /// Handler for log entries that have multiple corrections associated with it.
    /// </summary>
    private void mutliCorrectionDropDownClick(object sender, RoutedEventArgs e)
    {
      //construct the corrections menu flyout
      m_correctionsMenuFlyout.Items.Clear();
      DropDownButton dropDownButton = (DropDownButton)sender;
      LogEntry entry = (LogEntry)dropDownButton.DataContext;
      foreach (var correction in entry.Corrections.Corrections)
      {
        MenuFlyoutItem item = new MenuFlyoutItem() { Text = correction.Name };
        ToolTipService.SetToolTip(item, correction.Tooltip);
        item.Click += (s, e) => correction.Fix?.Invoke(correction.Parameter);
        m_correctionsMenuFlyout.Items.Add(item);
      }
      m_correctionsMenuFlyout.ShowAt(dropDownButton);
    }

    /// <summary>
    /// Cleans up menu entries when the flyout is closed.
    /// </summary>
    private void correctionsMenuFlyoutClosed(object sender, object e)
    {
      m_correctionsMenuFlyout.Items.Clear();
    }

    private void collapsibleLogEntryClick(object sender, RoutedEventArgs e)
    {
      Button button = (Button)sender;
      LogEntry entry = (LogEntry)button.DataContext;
      ICorrectiveLoggerDialog correctiveLoggerDialog = m_dialogService.CreateCorrectiveLoggerDialog(entry.Message, entry);
      correctiveLoggerDialog.ShowAsync();
    }

    public Task<IList<LogEntryDecorator>> LoadMoreItemsAsync(int count)
    {
      if (IsLoading) return Task.FromResult((IList<LogEntryDecorator>)Array.Empty<LogEntryDecorator>());

      return Task.Run(() =>
      {
        var items = new List<LogEntryDecorator>();

        lock (this)
        {
          IsLoading = true;

          if (HasMoreItems)
          {
            readUIFilters(out string filterText, out bool includeInformation, out bool includeWarnings, out bool includeError, out bool includeCritical);
            int returnedCount = 0;
            for (int i = m_lastReturnedIndex; returnedCount < count && i < m_entries.Count; i++)
            {
              var entry = m_entries[i];
              m_lastReturnedIndex++;

              //filter item based on the current filter settings
              bool typeMatch = (includeInformation && entry.Level == LogLevel.Information) ||
                               (includeWarnings && entry.Level == LogLevel.Warning) ||
                               (includeError && entry.Level == LogLevel.Error) ||
                               (includeCritical && entry.Level == LogLevel.Critical);
              if (typeMatch && entry.Matches(filterText))
              {
                returnedCount++;
                items.Add(new LogEntryDecorator(entry));
              }
            }

            HasMoreItems = m_lastReturnedIndex < m_entries.Count;
          }

          IsLoading = false;
        }

        return (IList<LogEntryDecorator>)items;
      });
    }
  }
}
