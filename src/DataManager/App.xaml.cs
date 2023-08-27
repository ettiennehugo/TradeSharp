using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.Hosting;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Security.AccessControl;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataManager
{
  /// <summary>
  /// Provides application-specific behavior to supplement the default Application class.
  /// </summary>
  public partial class App : Application
  {

    //constants


    //enums


    //types


    //attributes
    private Window m_window;

    //constructors
    public App()
    {
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
      registerServices();
      m_window = new MainWindow();
      m_window.Activate();
    }


    //properties
    public IHost ServiceHost { get; private set; }

    //methods
    private void registerServices()
    {
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException(string.Format(TradeSharp.Common.Resources.HomeDirectoryMissing, Constants.TradeSharpHome));
      string configDir = string.Format("{0}\\{1}", tradeSharpHome, Constants.ConfigurationDir);

      ServiceHost = Host.CreateDefaultBuilder()
          .ConfigureAppConfiguration(config =>
          {
            config.SetBasePath(configDir);
            config.AddJsonFile(Constants.ConfigurationFile);
          }).Build();


//          .ConfigureLogging(logging =>
//          {
//#if DEBUG
//            logging.AddDebug();
//#endif
//          })
//          .ConfigureServices(services =>
//          {
//            services.AddSingleton<IConfigurationService, ConfigurationService>();
//            services.AddSingleton<IDataStoreService, SqliteDataStoreService>();    //Sqlite currently the only supported data store, if this changes we need to base this off configuration
//            services.AddSingleton<IDataManagerService, DataManagerService>();
//          }).Build();
    }
  }
}
