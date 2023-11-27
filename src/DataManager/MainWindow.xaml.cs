using Microsoft.UI.Xaml;
using TradeSharp.WinDataManager.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using TradeSharp.CoreUI.Events;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

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
 


    //TODO:
    //  - Read TAP article on how to do this.
    //  - Need to add a high level exception handler as a catch all for errors.
    //  - Set status when an import/export operation is started (currently it would notify you when it ends).
    //  - See how you can indicate a lot of different async processes (e.g. maybe a list of overlayed progress rings or list of progress bars)
    //     - Create an API for this related to the progress control in the status bar - this should support creating it with a proper tooltip, updating the state and 
    //       removing it once the process is done.
    //     - Currently the progress bar would always be visible, make it so it only shows up when a process is under way.

    
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
      ViewModel = Ioc.Default.GetRequiredService<MainWindowViewModel>();
      ViewModel.SetNavigationFrame(m_mainContent);
      DialogService dialogService = (DialogService)Ioc.Default.GetRequiredService<IDialogService>();
      dialogService.StatusBarIcon = m_statusBarIcon;
      dialogService.StatusBarText = m_statusBarText;
      m_navigationView.SelectionChanged += ViewModel.OnNavigationSelectionChanged;
    }

    private void m_mainContent_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
      m_statusBarIcon.Glyph = "";
      m_statusBarText.Text = "";
    }
  }
}
