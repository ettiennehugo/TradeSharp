using Microsoft.UI.Xaml;
using TradeSharp.WinDataManager.ViewModels;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;

namespace TradeSharp.WinDataManager
{
  /// <summary>
  /// An empty window that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainWindow : Window
  {
    //constants


    //enums


    //types


    //attributes


    //constructors   
    public MainWindow()
    {
      this.InitializeComponent();
      //https://learn.microsoft.com/en-us/windows/apps/develop/title-bar?tabs=wasdk
      this.ExtendsContentIntoTitleBar = true;
      this.SetTitleBar(m_titleBar);
    }

    //finalizers


    //interface implementations


    //properties
    public MainWindowViewModel ViewModel { get; internal set; }

    //methods
    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
      ViewModel = (MainWindowViewModel)((IApplication)Application.Current).Services.GetService(typeof(MainWindowViewModel));
      ViewModel.SetNavigationFrame(m_mainContent);
      DialogService dialogService = (DialogService)((IApplication)Application.Current).Services.GetService(typeof(IDialogService));
      dialogService.StatusBarIcon = m_statusBarIcon;
      dialogService.StatusBarText = m_statusBarText;
      dialogService.UIDispatcherQueue = DispatcherQueue;
      m_navigationView.SelectionChanged += ViewModel.OnNavigationSelectionChanged;
    }

    private void m_mainContent_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
      m_statusBarIcon.Glyph = "";
      m_statusBarText.Text = "";
    }
  }
}
