using Microsoft.UI.Xaml;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;
using TradeSharp.WinDataManager.ViewModels;
using TradeSharp.WinCoreUI.Common;
using TradeSharp.WinCoreUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TradeSharp.WinDataManager
{
  /// <summary>
  /// Setup the main window for the data manager application and add additional services required for it.
  /// </summary>
  public partial class App : ApplicationBase
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
    protected override void configureServices(IServiceCollection services)
    {
      base.configureServices(services);
      services.AddSingleton<IInitNavigationService, InitNavigationService>();
      services.AddSingleton<INavigationService, NavigationService>();
      services.AddSingleton<MainWindowViewModel>();
    }
  }
}
