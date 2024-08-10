using Microsoft.UI.Xaml;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;

namespace TradeSharp.WinPortfolioManager
{
  /// <summary>
  /// Main window for the portfolio manager application.
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
    private void m_newPortfolioButton_Click(object sender, RoutedEventArgs e)
    {
      m_dialogService.ShowNewPortfolioAsync();
    }

    private void m_exitMenu_Click(object sender, RoutedEventArgs e)
    {
      App.Current.Exit();
    }
  }
}
