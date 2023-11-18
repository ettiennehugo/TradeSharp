using Microsoft.UI.Xaml;
using TradeSharp.WinDataManager.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using TradeSharp.CoreUI.Events;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;
using Microsoft.UI.Xaml.Controls;

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
 


    //TODO: Need to add a high level exception handler as a catch all for errors.
    //  - If you try to import a file already open in Excel this breaks.
    //  - Set status when an import/export operation is started (currently it would notify you when it ends).
    //  - See how you can indicate a lot of different async processes (e.g. maybe a list of overlayed progress rings)
    //     - Create an API for this related to the progress control in the status bar - this should support creating it with a proper tooltip, updating the state and 
    //       removing it once the process is done.
    //     - Currently the progress bar would always be visible, make it so it only shows up when a process is under way.
    //  - Profile the import operations for CSV and JSON and check where you can optimize them.



    
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
    // Send message to use navigation between views if the window size is not large enough to display a list view and the related detailed data.
    private void OnSizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
      double width = args.Size.Width;
      NavigationMessage navigation = new(new()
      {
        UseNavigation = width < 1024
      });
      WeakReferenceMessenger.Default.Send(navigation);
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
      ViewModel = Ioc.Default.GetRequiredService<MainWindowViewModel>();
      ViewModel.SetNavigationFrame(m_mainContent);

      DialogService dialogService = (DialogService)Ioc.Default.GetRequiredService<IDialogService>();
      dialogService.StatusBarIcon = m_statusBarIcon;
      dialogService.StatusBarText = m_statusBarText;
      dialogService.StatusBarProgress = m_statusBarProgress;

      m_navigationView.SelectionChanged += ViewModel.OnNavigationSelectionChanged;
      m_navigationView.SelectionChanged += this.OnNavigationSelectionChanged;
    }

    public void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
      m_statusBarIcon.Glyph = "";
      m_statusBarText.Text = "";
    }
  }
}
