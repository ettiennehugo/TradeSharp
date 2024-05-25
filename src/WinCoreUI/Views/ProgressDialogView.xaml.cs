using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Progress dialog with an optional log display.
  /// NOTE: The API of this window can be accessed from background threads so we need to run a lot of it through the dispatcher queue on the UI thread.
  /// </summary>
  public sealed partial class ProgressDialogView : Page, IProgressDialog
  {
    //constants


    //enums


    //types


    //attributes
    private CancellationTokenSource m_cancellationTokenSource;
    private string m_title;
    private object m_minimumLock;
    private double m_minimum;
    private object m_maximumLock;
    private double m_maximum;
    private object m_progressLock;
    private double m_progress;
    private double m_progressPercent;
    private object m_completeLock;
    private bool m_complete;
    private object m_statusMessageLock;
    private string m_statusMessageText;
    private bool m_parentWindowSizeInit;
    private ILogger? m_logger;
    private bool m_logViewVisible;
    private bool m_closeOnCancelClick;

    //constructors
    public ProgressDialogView()
    {
      m_cancellationTokenSource = new CancellationTokenSource();
      m_title = "Progress";
      m_minimumLock = new object();
      m_minimum = 0;
      m_maximumLock = new object();
      m_maximum = 100;
      m_progressLock = new object();
      m_progress = 0;
      m_progressPercent = 0;
      m_completeLock = new object();
      m_complete = false;
      m_statusMessageLock = new object();
      m_statusMessageText = "";
      m_logger = null;
      m_logViewVisible = false;
      m_parentWindowSizeInit = false;
      m_closeOnCancelClick = false;
      InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public CancellationTokenSource CancellationTokenSource => m_cancellationTokenSource;
    public Window ParentWindow { set; get; }
    public string Title
    {
      get => m_title;
      set
      {
        m_title = value;
        DispatcherQueue.TryEnqueue(() => m_titleBarText.Text = m_title);
      }
    }

    //NOTE: All these methods need to be thread safe and run the set methods on the UI thread since they would be called from the worker threads.
    public double Minimum 
    {
      get => m_minimum;
      set 
      { 
        lock (m_minimumLock) 
        {
          m_minimum = value;  //NOTE: We need to keep a deep copy of the new value since dispather queue will look for it later in the UI thread when value parameter is already deallocated.
          DispatcherQueue.TryEnqueue(() => m_progressBar.Minimum = m_minimum);
        }
      }
    }
    
    public double Maximum 
    { 
      get => m_maximum;
      set 
      {
        lock (m_maximumLock)
        {
          m_maximum = value;  //NOTE: We need to keep a deep copy of the new value since dispather queue will look for it later in the UI thread when value parameter is already deallocated.
          DispatcherQueue.TryEnqueue(() => m_progressBar.Maximum = m_maximum);
        }
      }
    }

    public double Progress 
    { 
      get => m_progress;
      set
      {
        lock (m_progressLock)
        {
          m_progress = value;  //NOTE: We need to keep a deep copy of the new value since dispather queue will look for it later in the UI thread when value parameter is already deallocated.
          m_progressPercent = m_maximum > 0 && m_progress >= 0 ? (m_progress / m_maximum) * 100 : 0;
          DispatcherQueue.TryEnqueue(() => { m_progressBar.Value = m_progress; m_progressLabel.Text = $"{m_progressPercent:#0}%"; } );
        }
      }
    }

    public bool Complete
    {
      get => m_complete;
      set
      {
        lock (m_completeLock)
        {
          m_complete = value;
          if (m_complete)
            DispatcherQueue.TryEnqueue(() => { m_cancelBtn.Content = "Close"; m_closeOnCancelClick = true; ToolTipService.SetToolTip(m_cancelBtn, "Close dialog"); });
        }
      }
    }

    public string StatusMessage 
    { 
      get => m_statusMessage.Text;
      set 
      { 
        lock (m_statusMessageLock) 
        {
          m_statusMessageText = value;   //NOTE: We need to keep a deep copy of the new value since dispather queue will look for it later in the UI thread when value parameter is already deallocated.
          DispatcherQueue.TryEnqueue(() => m_statusMessage.Text = m_statusMessageText);
        }
      }
    }

    public ILogger? Logger
    {
      get => m_logger;
      set => m_logger = value;
    }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      ParentWindow.SetTitleBar(m_titleBar);
      m_parentWindowSizeInit = true;
    }

    public Task ShowAsync()
    {
      return Task.Run(() => ParentWindow.DispatcherQueue.TryEnqueue(() => ParentWindow.Activate()));
    }

    public void Close(bool cancelOperation)
    {
      if (cancelOperation) m_cancellationTokenSource.Cancel();
      ParentWindow.Close();
    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {
      //transition "cancel" button to "close" state or close if already in that state - the transition only happens if there are log entries
      //present otherwise the user does not have a chance to see the log entries.
      if (!m_closeOnCancelClick)
      {
        if (m_loggerView.UnfilteredEntryCount > 0)
        {
          if (Progress != Maximum) m_cancellationTokenSource.Cancel();  //only cancel run if process is not already completed.
          m_cancelBtn.Content = "Close";
          ToolTipService.SetToolTip(m_cancelBtn, "Close dialog");
          m_closeOnCancelClick = true;
        }
        else
          ParentWindow.Close(); //no log to view (user was viewing only the progress) so just close the dialog
      }
      else
        ParentWindow.Close();
    }

    public IDisposable BeginScope(string message)
    {
      ensureLogVisible();
      return m_loggerView.BeginScope(message);
    }

    public ILogCorrections Log(LogLevel level, string message)
    {
      ensureLogVisible();
      return m_loggerView.Log(level, message);
    }

    public ILogCorrections LogInformation(string message)
    {
      ensureLogVisible();
      return m_loggerView.LogInformation(message);
    }

    public ILogCorrections LogDebug(string message)
    {
      ensureLogVisible();
      return m_loggerView.LogDebug(message); 
    }

    public ILogCorrections LogWarning(string message)
    {
      ensureLogVisible();
      return m_loggerView.LogWarning(message);
    }

    public ILogCorrections LogError(string message)
    {
      ensureLogVisible();
      return m_loggerView.LogError(message);
    }

    public ILogCorrections LogCritical(string message)
    {
      ensureLogVisible();
      return m_loggerView.LogCritical(message);
    }

    /// <summary>
    /// Logger view row is started out as size zero to collapse it, this method sets it up to be visible when required.
    /// </summary>
    private void ensureLogVisible()
    {
      //make log viewer visible and resize window to accomodate it
      //NOTE: Logging can be called before the window is initialized so we ignore the resize in this method
      //      otherwise the initialization of the window size will override this resize.
      if (!m_parentWindowSizeInit || m_logViewVisible) return;

      m_logViewVisible = true;  //flag that we already requested the log view to be visible
      DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
      {
        //resize the controls and window to accomodate the larger log view
        m_progressBar.Width = 1060;
        ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(2230, 1285));
        m_loggerView.Visibility = Visibility.Visible;
        m_parentWindowSizeInit = true;
      });
    }
  }
}
