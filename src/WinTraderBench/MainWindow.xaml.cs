using Microsoft.UI.Xaml;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.WinTraderWorkbench
{
  /// <summary>
  /// Main window for the trader workbench application.
  /// </summary>
  public sealed partial class MainWindow : Window
  {

    //constants


    //enums


    //types


    //attributes
    private IDialogService m_dialogService;

    //properties


    //constructors
    public MainWindow()
    {
      m_dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      this.InitializeComponent();
      AppWindow.SetIcon("Assets\\Square64x64.ico");
    }

    //finalizers


    //interface implementations


    //methods
    private void m_exitMenu_Click(object sender, RoutedEventArgs e)
    {
      App.Current.Exit();
    }

    private void m_newChartMenu_Click(object sender, RoutedEventArgs e)
    {
      m_dialogService.ShowNewChartAsync();
    }

    private void m_newScannerMenu_Click(object sender, RoutedEventArgs e)
    {
      m_dialogService.ShowNewScannerAsync();
    }

    private void m_newEventStudyButton_Click(object sender, RoutedEventArgs e)
    {
      m_dialogService.ShowNewEventStudyAsync();
    }
  }
}
