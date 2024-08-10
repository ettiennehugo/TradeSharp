using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.WinCoreUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.WinDataManager.Services;
using TradeSharp.CoreUI.Commands;

namespace TradeSharp.WinCoreUI.Common
{
    /// <summary>
    /// Common base class for all TradeSharp applications.
    /// </summary>
    public class ApplicationBase: Application, IApplication
  {
    //constants


    //enums


    //types


    //attributes
    protected IHost m_host;

    //properties
    public IServiceProvider Services { get => m_host.Services; }

    //constructors
    public ApplicationBase()
    {
      IApplication.Current = this;
    }

    //finalizers


    //interface implementations
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
      registerServices();
      loadCachedData();

      //setup dispatcher queue for UI thread in the dialog service
      var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
      var dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      ((DialogService)dialogService).UIDispatcherQueue = dispatcherQueue;

      UnhandledException += OnUnhandledException;
    }

    public void Shutdown()
    {
      shutdownServices();
    }

    //methods
    /// <summary>
    /// Define the set of standard services, sub-classes can override this method to add additional services.
    /// </summary>
    protected virtual void configureServices(IServiceCollection services)
    {
      services.AddSingleton<IConfigurationService, ConfigurationService>();
      services.AddSingleton<IDatabase, SqliteDatabase>();    //Sqlite is currently the only supported data store, if this changes we need to base this off configuration and add the services dynamically
      services.AddSingleton<IDialogService, DialogService>();
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
      services.AddSingleton<IInstrumentCacheService, InstrumentCacheService>();
      services.AddTransient<IInstrumentService, InstrumentService>();
      services.AddSingleton<IInstrumentGroupService, InstrumentGroupService>();
      services.AddSingleton<ICountryViewModel, CountryViewModel>();
      services.AddSingleton<IHolidayViewModel, HolidayViewModel>();
      services.AddSingleton<IExchangeViewModel, ExchangeViewModel>();
      services.AddSingleton<ISessionViewModel, SessionViewModel>();
      services.AddTransient<IInstrumentViewModel, InstrumentViewModel>();
      services.AddSingleton<IInstrumentGroupViewModel, WinCoreUI.ViewModels.InstrumentGroupViewModel>();
      services.AddTransient<IInstrumentBarDataRepository, InstrumentBarDataRepository>(); //this repository must be transient as it requires keying around the data provider, instrument and resolution passed from the view model which is also transient
      services.AddTransient<IInstrumentBarDataService, InstrumentBarDataService>(); //this service must be transient as it requires keying around the data provider, instrument and resolution passed from the view model which is also transient
      services.AddTransient<IInstrumentBarDataViewModel, WinCoreUI.ViewModels.InstrumentBarDataViewModel>();  //windows implementation is used in order to support incremental loading
                                                                                                              //NOTE: Plugins needs to be loaded second to last of all the services/view models since the base repositories/services/view models need to be in place to support the plugins.
      services.AddSingleton<IPluginsService, PluginsService>();
      services.AddSingleton<IPluginsViewModel, PluginsViewModel>();
      //NOTES:
      // - Broker accounts view model is last since it requires the broker plugins to be loaded first - no broker plugins should try to load this server and/or view model
      // - Broker accounts view model can be manipulated by different views to each view should have it's own copy, hence transient.
      services.AddTransient<IBrokerAccountsService, BrokerAccountsService>();
      services.AddTransient<IBrokerAccountsViewModel, BrokerAccountsViewModel>();
    }

    protected virtual void configureLogging(HostBuilderContext context, ILoggingBuilder logging)
    {
      logging.ClearProviders();
      logging.AddConsole();
      logging.AddDebug();
      logging.AddEventSourceLogger();
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) logging.AddEventLog();
#if DEBUG
      logging.SetMinimumLevel(LogLevel.Trace);  //enable full logging in debug mode
#endif
    }

    private void registerServices()
    {
      //https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines
      m_host = Host.CreateDefaultBuilder()
        .ConfigureServices(services => configureServices(services))
        .ConfigureLogging((context, logging) => configureLogging(context, logging))
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
      Task.Run(() =>
      {
        //start caching crucial data in the background
        var holidayViewModel = (IHolidayViewModel)IApplication.Current.Services.GetService(typeof(IHolidayViewModel));
        holidayViewModel.RefreshCommand.Execute(null);
        var countryViewModel = (ICountryViewModel)IApplication.Current.Services.GetService(typeof(ICountryViewModel));
        countryViewModel.RefreshCommand.Execute(null);
        var exchangeViewModel = (IExchangeViewModel)IApplication.Current.Services.GetService(typeof(IExchangeViewModel));
        exchangeViewModel.RefreshCommand.Execute(null);
        var instrumentCacheService = (IInstrumentCacheService)IApplication.Current.Services.GetService(typeof(IInstrumentCacheService));
        instrumentCacheService.Refresh();
        var instrumentGroupViewModel = (IInstrumentGroupViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentGroupViewModel));
        instrumentGroupViewModel.RefreshCommandAsync.Execute(null);

        //setup plugin service host and start caching plugins
        var pluginsService = m_host.Services.GetService<IPluginsService>();
        pluginsService.Host = m_host;
        var pluginsViewModel = m_host.Services.GetService<IPluginsViewModel>();
        pluginsViewModel.RefreshCommand.Execute(null);    //this is the only refresh that should be called since during run-time these services are not expected to change
      });
    }

    /// <summary>
    /// Generic exception handler for application - investigate exceptions raised since it might indicate a deeper issue.
    /// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.unhandledexceptioneventhandler?view=windows-app-sdk-1.5
    /// </summary>
    protected void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
      //log the exception
      var logger = m_host.Services.GetService<ILogger<ApplicationBase>>();
      logger.LogError(e.Exception, "Unhandled exception occurred in the application.");

      //display the exception to the user
      var dialogService = m_host.Services.GetService<IDialogService>();
      dialogService.ShowPopupMessageAsync($"An unhandled exception occurred in the application - {e.Message}");
    }
  }
}
