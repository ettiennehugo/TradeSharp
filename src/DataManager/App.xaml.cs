using Microsoft.UI.Xaml;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using TradeSharp.WinDataManager.Services;
using TradeSharp.WinDataManager.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.CoreUI.Repositories;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinDataManager
{
  /// <summary>
  /// Creates the default application with the required services for the data manager.
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
      initServiceCaches();
      m_window = new MainWindow();
      m_window.Activate();
    }

    //properties


    //methods
    private void registerServices()
    {
      Ioc.Default.ConfigureServices(
        new ServiceCollection()  
          .AddSingleton<ILoggerFactory, NullLoggerFactory>()            //TODO: Implement or add a proper logger - C# book p.422 and https://blog.stephencleary.com/2018/05/microsoft-extensions-logging-part-1-introduction.html
          .AddSingleton<IConfigurationService, ConfigurationService>()
          .AddSingleton<IDataStoreService, SqliteDataStoreService>()    //Sqlite is currently the only supported data store, if this changes we need to base this off configuration and add the services dynamically
          .AddSingleton<IDialogService, DialogService>()
          .AddSingleton<INavigationService, NavigationService>()
          .AddSingleton<InitNavigationService>()
          .AddSingleton<MainWindowViewModel>()
          .AddSingleton<IDataProviderRepository, DataProviderRepository>()
          .AddSingleton<ICountryRepository, CountryRepository>()
          .AddSingleton<IHolidayRepository, HolidayRepository>()
          .AddSingleton<IExchangeRepository, ExchangeRepository>()
          .AddSingleton<ISessionRepository, SessionRepository>()
          .AddSingleton<IInstrumentRepository, InstrumentRepository>()
          .AddSingleton<IInstrumentGroupRepository, InstrumentGroupRepository>()
          .AddSingleton<IInstrumentBarDataRepository, InstrumentBarDataRepository>()
          .AddSingleton<ICountryService, CountryService>()
          .AddSingleton<IHolidayService, HolidayService>()
          .AddSingleton<IExchangeService, ExchangeService>()
          .AddSingleton<ISessionService, SessionService>()
          .AddSingleton<IInstrumentService, InstrumentService>()
          .AddSingleton<IInstrumentGroupService, InstrumentGroupService>()
          .AddSingleton<IInstrumentBarDataService, InstrumentBarDataService>()
          .AddScoped<CountryViewModel>()
          .AddScoped<CountryItemViewModel>()
          .AddScoped<HolidayViewModel>()
          .AddScoped<ExchangeViewModel>()
          .AddScoped<ExchangeItemViewModel>()
          .AddScoped<SessionViewModel>()
          .AddScoped<InstrumentViewModel>()
          .AddScoped<InstrumentGroupViewModel>()
          .AddScoped<InstrumentBarDataViewModel>()
          .BuildServiceProvider()
      );
    }

    /// <summary>
    /// Fires off loading of services that support large data sets to cache data where possible.
    /// NOTE: All methods called here should be async to avoid hanging up the UI thread.
    /// </summary>
    private void initServiceCaches() 
    {
      var instrumentService = Ioc.Default.GetRequiredService<IInstrumentService>();
      instrumentService.RefreshAsync();
    }
  }
}
