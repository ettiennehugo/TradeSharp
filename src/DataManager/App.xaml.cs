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
          services.AddSingleton<CountryViewModel>();
          services.AddSingleton<HolidayViewModel>();
          services.AddSingleton<ExchangeViewModel>();
          services.AddSingleton<SessionViewModel>();
          services.AddSingleton<InstrumentViewModel>();
          services.AddSingleton<InstrumentGroupViewModel>();

          services.AddTransient<IInstrumentBarDataRepository, InstrumentBarDataRepository>(); //this repository must be transient as it requires keying around the data provider, instrument and resolution passed from the view model which is also transient
          services.AddTransient<IInstrumentBarDataService, InstrumentBarDataService>(); //this service must be transient as it requires keying around the data provider, instrument and resolution passed from the view model which is also transient
          services.AddTransient<InstrumentBarDataViewModel>();
          services.AddTransient<WinInstrumentBarDataViewModel>();
        })
        .ConfigureLogging((context, logging) =>
        {
          logging.ClearProviders();
          logging.AddConsole();
          logging.AddDebug();
          logging.AddEventSourceLogger();
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) logging.AddEventLog();
        })
        .Build();
    }

    private void loadCachedData()
    {
      //start caching the instrument data
      var instrumentViewModel = (InstrumentViewModel)((IApplication)Application.Current).Services.GetService(typeof(InstrumentViewModel));
      instrumentViewModel.RefreshCommandAsync.Execute(null);
    }
  }
}
