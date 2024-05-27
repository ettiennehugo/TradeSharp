using System;
using Microsoft.UI.Xaml;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;
using TradeSharp.WinDataManager.ViewModels;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.WinCoreUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.WinCoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinDataManager
{
  /// <summary>
  /// Creates the default application with the required services for the data manager.
  /// </summary>
  public partial class App : Application, IApplication
  {
    //constants


    //enums


    //types


    //attributes
    private Window m_window;
    private IHost m_host;

    //constructors
    public App()
    {
      this.InitializeComponent();
      IApplication.Current = this;
    }

    //finalizers


    //interface implementations
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
      registerServices();
      loadCachedData();
      m_window = new MainWindow();
      m_window.Activate();
      UnhandledException += OnAppUnhandledException;
    }

    public void Shutdown()
    {
      shutdownServices();
    }

    //properties
    public IServiceProvider Services { get => m_host.Services; }

    //methods
    private void registerServices()
    {
      //https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines
      m_host = Host.CreateDefaultBuilder()
        .ConfigureServices(services  =>
        {
          services.AddSingleton<IConfigurationService, ConfigurationService>();
          services.AddSingleton<IDatabase, SqliteDatabase>();    //Sqlite is currently the only supported data store, if this changes we need to base this off configuration and add the services dynamically
          services.AddSingleton<IDialogService, DialogService>();
          services.AddSingleton<INavigationService, NavigationService>();
          services.AddSingleton<InitNavigationService>();
          services.AddSingleton<MainWindowViewModel>();
          services.AddSingleton<IDataProviderRepository, DataProviderRepository>();
          services.AddSingleton<ICountryRepository, CountryRepository>();
          services.AddSingleton<IHolidayRepository, HolidayRepository>();
          services.AddSingleton<IExchangeRepository, ExchangeRepository>();
          services.AddSingleton<ISessionRepository, SessionRepository>();
          services.AddSingleton<IInstrumentRepository, InstrumentRepository>();
          services.AddSingleton<IInstrumentGroupRepository, InstrumentGroupRepository>();
          services.AddSingleton<ICountryService, CountryService>();
          services.AddSingleton<IHolidayService, HolidayService>();
          services.AddSingleton<IExchangeService, ExchangeService>();
          services.AddSingleton<ISessionService, SessionService>();
          services.AddSingleton<IInstrumentService, InstrumentService>();
          services.AddSingleton<IInstrumentGroupService, InstrumentGroupService>();
          services.AddSingleton<ICountryViewModel, CountryViewModel>();
          services.AddSingleton<IHolidayViewModel, HolidayViewModel>();
          services.AddSingleton<IExchangeViewModel, ExchangeViewModel>();
          services.AddSingleton<ISessionViewModel, SessionViewModel>();
          services.AddSingleton<IMassDownloadInstrumentDataService, MassDownloadInstrumentDataService>();
          services.AddSingleton<IMassCopyInstrumentDataService, MassCopyInstrumentDataService>();
          services.AddSingleton<IMassImportInstrumentDataService, MassImportInstrumentDataService>();
          services.AddSingleton<IMassExportInstrumentDataService, MassExportInstrumentDataService>();
          services.AddSingleton<IInstrumentViewModel, InstrumentViewModel>();
          services.AddSingleton<IInstrumentGroupViewModel, WinCoreUI.ViewModels.InstrumentGroupViewModel>();
          services.AddTransient<IInstrumentBarDataRepository, InstrumentBarDataRepository>(); //this repository must be transient as it requires keying around the data provider, instrument and resolution passed from the view model which is also transient
          services.AddTransient<IInstrumentBarDataService, InstrumentBarDataService>(); //this service must be transient as it requires keying around the data provider, instrument and resolution passed from the view model which is also transient
          services.AddTransient<IInstrumentBarDataViewModel, WinCoreUI.ViewModels.InstrumentBarDataViewModel>();  //windows implementation is used in order to support incremental loading
          //NOTE: Plugins needs to be loaded last of all the services/view models since the base repositories/services/view models need to be in place to support the plugins.
          services.AddSingleton<IPluginsService, PluginsService>();
          services.AddSingleton<IPluginsViewModel, PluginsViewModel>();
        })
        .ConfigureLogging((context, logging) =>
        {
          logging.ClearProviders();
          logging.AddConsole();
          logging.AddDebug();
          logging.AddEventSourceLogger();          
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) logging.AddEventLog();
#if DEBUG
          logging.SetMinimumLevel(LogLevel.Trace);  //enable full logging in debug mode
#endif
        })
        .Build();
      
      //run the service host for whatever reason!!!
      m_host.RunAsync();
    }

    private void shutdownServices()
    {
      m_host.Dispose();
    }

    private void loadCachedData()
    {
      //start caching crucial data
      var holidayViewModel = (IHolidayViewModel)IApplication.Current.Services.GetService(typeof(IHolidayViewModel));
      holidayViewModel.RefreshCommand.Execute(null);
      var countryViewModel = (ICountryViewModel)IApplication.Current.Services.GetService(typeof(ICountryViewModel));
      countryViewModel.RefreshCommand.Execute(null);
      var exchangeViewModel = (IExchangeViewModel)IApplication.Current.Services.GetService(typeof(IExchangeViewModel));
      exchangeViewModel.RefreshCommand.Execute(null);
      var instrumentViewModel = (IInstrumentViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentViewModel));
      instrumentViewModel.RefreshCommand.Execute(null);
      var instrumentGroupViewModel = (IInstrumentGroupViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentGroupViewModel));
      instrumentGroupViewModel.RefreshCommand.Execute(null);

      //setup dispatcher queue for UI thread
      var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
      var instrumentBarDataViewModel = (IInstrumentBarDataViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentBarDataViewModel));
      ((WinCoreUI.ViewModels.InstrumentBarDataViewModel)instrumentBarDataViewModel).UIDispatcherQueue = dispatcherQueue;
      ((WinCoreUI.ViewModels.InstrumentGroupViewModel)instrumentGroupViewModel).UIDispatcherQueue = dispatcherQueue;
      var dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      ((DialogService)dialogService).UIDispatcherQueue = dispatcherQueue;

      //setup plugin service host and start caching plugins
      var pluginsService = m_host.Services.GetService<IPluginsService>();
      pluginsService.Host = m_host;
      pluginsService.Refresh();
      var pluginsViewModel = m_host.Services.GetService<IPluginsViewModel>();
      pluginsViewModel.RefreshCommandAsync.ExecuteAsync(null);    //this is the only refresh that should be called since during run-time these services are not expected to change
    }

    /// <summary>
    /// Generic exception handler for application - investigate exceptions raised since it might indicate a deeper issue.
    /// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.unhandledexceptioneventhandler?view=windows-app-sdk-1.5
    /// </summary>
    protected void OnAppUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
      //log the exception
      var logger = m_host.Services.GetService<ILogger<App>>();
      logger.LogError(e.Exception, "Unhandled exception occurred in the application.");

      //display the exception to the user
      var dialogService = m_host.Services.GetService<IDialogService>();
      dialogService.ShowPopupMessageAsync($"An unhandled exception occurred in the application - {e.Message}");
    }

  }
}
