using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.Common;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class ProgressDialogView : Page, IProgressDialog
    {


    //constants


    //enums


    //types


    //attributes
    private CancellationTokenSource m_cancellationTokenSource;
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

    //constructors
    public ProgressDialogView()
    {
      m_cancellationTokenSource = new CancellationTokenSource();
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
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public CancellationTokenSource CancellationTokenSource => m_cancellationTokenSource;
    public Window ParentWindow { get; set; }
    public string Title { get; set ; }

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

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      ParentWindow.SetTitleBar(m_titleBar);
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
  }
}
