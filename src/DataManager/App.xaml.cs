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
using Microsoft.UI.Xaml.Controls;

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
      m_window = new MainWindow();
      m_window.Activate();
    }

    //properties


    //methods
    private void registerServices()
    {
      Ioc.Default.ConfigureServices(
        new ServiceCollection()
          .AddSingleton<IConfigurationService, ConfigurationService>()
          .AddSingleton<IDataStoreService, SqliteDataStoreService>()    //Sqlite is currently the only supported data store, if this changes we need to base this off configuration and add the services dynamically`
          .AddSingleton<IDialogService, DialogService>()
          .AddSingleton<INavigationService, NavigationService>()
          .AddSingleton<InitNavigationService>()
          .AddScoped<MainWindowViewModel>()
          .AddScoped<ICountryRepository, CountryRepository>()
          .AddScoped<IHolidayRepository, HolidayRepository>()
          .AddScoped<IItemsService<Country>, CountryService>()
          .AddScoped<IItemsService<Holiday>, HolidayService>()
          .AddScoped<CountryViewModel>()
          .AddScoped<CountryItemViewModel>()
          .AddScoped<HolidayViewModel>()
          .AddScoped<HolidayItemViewModel>()
          .BuildServiceProvider()
      );
    }
  }
}
