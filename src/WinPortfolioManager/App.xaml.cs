using Microsoft.UI.Xaml;
using TradeSharp.Common;
using TradeSharp.WinCoreUI.Common;

namespace TradeSharp.WinPortfolioManager
{
  /// <summary>
  /// Provides application-specific behavior to supplement the default Application class.
  /// </summary>
  public partial class App : ApplicationBase, IApplication
  {

    //constants


    //enums


    //types


    //attributes
    private Window m_window;

    //properties


    //constructors
    public App(): base()
    {
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
      base.OnLaunched(args);
      m_window = new MainWindow();
      m_window.Activate();
    }

    //methods


  }
}
