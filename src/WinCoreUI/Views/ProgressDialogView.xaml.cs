using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// NOTE: The API of this window can be accessed from background threads so we need to run a lot of it through the dispatcher queue on the UI thread.
  /// </summary>
  public sealed partial class ProgressDialogView : Page, IProgressDialog
  {
    //constants
    public const int LogHeight = 500;

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
    private ILogger? m_logger;

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
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public CancellationTokenSource CancellationTokenSource => m_cancellationTokenSource;
    public Window ParentWindow { get; set; }
    public string Title
    {
      get => m_title;
      set
      {
        m_title = value;
        m_titleBar.DispatcherQueue.TryEnqueue(() => m_titleBarText.Text = m_title);
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
          m_progressBar.DispatcherQueue.TryEnqueue(() => m_progressBar.Minimum = m_minimum);
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
          m_progressBar.DispatcherQueue.TryEnqueue(() => m_progressBar.Maximum = m_maximum);
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
          m_progressBar.DispatcherQueue.TryEnqueue(() => m_progressBar.Value = m_progress);
          m_progressLabel.DispatcherQueue.TryEnqueue(() => m_progressLabel.Text = $"{m_progressPercent:#0}%");
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
          if (m_complete) m_cancelBtn.DispatcherQueue.TryEnqueue(() => { m_cancelBtn.Content = "Close"; ToolTipService.SetToolTip(m_cancelBtn, "Close dialog"); });
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
          m_statusMessage.DispatcherQueue.TryEnqueue(() => m_statusMessage.Text = m_statusMessageText);
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
      m_layoutMain.RowDefinitions[3].Height = new GridLength(0);
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
      if (Progress != Maximum) m_cancellationTokenSource.Cancel();  //only cancel run if process is not already completed.
      ParentWindow.Close();
    }

    public IDisposable BeginScope(LogLevel level, string message)
    {
      ensureLogVisible();
      return m_progressLogger.BeginScope(level, message);
    }

    public void Log(LogLevel level, string message)
    {
      ensureLogVisible();
      m_progressLogger.Log(level, message);
    }

    public void LogInformation(string message)
    {
      ensureLogVisible();
      m_progressLogger.LogInformation(message);
    }

    public void LogDebug(string message)
    {
      ensureLogVisible();
      m_progressLogger.LogDebug(message); 
    }

    public void LogWarning(string message)
    {
      ensureLogVisible();
      m_progressLogger.LogWarning(message);
    }

    public void LogError(string message)
    {
      ensureLogVisible();
      m_progressLogger.LogError(message);
    }

    public void LogCritical(string message)
    {
      ensureLogVisible();
      m_progressLogger.LogCritical(message);
    }

    private void ensureLogVisible()
    {
      //make log row visible and resize window to accomodate it
      if (m_layoutMain.RowDefinitions[3].Height == new GridLength(0))
      {        
        DispatcherQueue.TryEnqueue(() => m_layoutMain.RowDefinitions[3].Height = new GridLength(LogHeight));
        Windows.Graphics.SizeInt32 size = ParentWindow.AppWindow.ClientSize;
        ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(size.Width, size.Height + LogHeight));
      }
    }
  }
}
