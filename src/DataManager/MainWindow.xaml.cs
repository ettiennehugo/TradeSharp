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
