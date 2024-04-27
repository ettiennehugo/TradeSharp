using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Views;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty window that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class LoggerViewDialog : Window, ICorrectiveLoggerDialog
  {
    private string m_title;
    public new string Title
    {
      get => m_title;
      set
      {
        lock(this)
        {
          m_title = value;
          DispatcherQueue.TryEnqueue(() => { if (m_titleBarText != null) m_titleBarText.Text = m_title; });
        }
      }
    }

    public LoggerViewDialog(LogEntry? entry = null)
    {
      this.InitializeComponent();
      SetTitleBar(m_titleBar);
      m_titleBarText.Text = m_title;
      if (entry != null)
      {
        if (entry is CollapsibleLogEntry collapsibleLogEntry)
          foreach (var childEntry in collapsibleLogEntry.Children) m_loggerView.Add(childEntry);
        else
          m_loggerView.Add(entry);
      }
    }

    public IDisposable BeginScope(string message)
    {
      return m_loggerView.BeginScope(message);
    }

    public ILogCorrections Log(LogLevel level, string message)
    {
      return m_loggerView.Log(level, message);
    }

    public ILogCorrections LogCritical(string message)
    {
      return m_loggerView.LogCritical(message);
    }

    public ILogCorrections LogDebug(string message)
    {
      return m_loggerView.LogDebug(message);
    }

    public ILogCorrections LogError(string message)
    {
      return m_loggerView.LogError(message);
    }

    public ILogCorrections LogInformation(string message)
    {
      return m_loggerView.LogInformation(message);
    }

    public ILogCorrections LogWarning(string message)
    {
      return m_loggerView.LogWarning(message);
    }

    public void ShowAsync()
    {
      Activate();
    }

    private void m_closeBtn_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
