using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using TradeSharp.WinCoreUI.Services;
using TradeSharp.WinDataManager.Views;

namespace TradeSharp.WinDataManager.ViewModels
{
  /// <summary>
  /// View model class to facilitate the navigation between the different views of the data manager application.
  /// </summary>
  public partial class MainWindowViewModel : ViewModelBase
  {
    //constants


    //enums


    //types
    

    //attributes
    private readonly IInitNavigationService m_initNavigationService;

    /// <summary>
    /// Page configuration used by the navigation service.
    /// </summary>
    private readonly Dictionary<string, Type> m_pages = new Dictionary<string, Type>()
    {
      { INavigationService.DataManager.Brokers, typeof(WinCoreUI.Views.BrokersView) },
      { INavigationService.DataManager.DataProviders, typeof(WinCoreUI.Views.DataProvidersView) },
      { INavigationService.DataManager.Extensions, typeof(WinCoreUI.Views.ExtensionsView) },
      { INavigationService.DataManager.Countries, typeof(WinCoreUI.Views.CountriesView) },
      { INavigationService.DataManager.Exchanges, typeof(WinCoreUI.Views.ExchangesView) },
      //{ INavigationService.DataManager.Fundamentals, typeof(BlankView) },
      { INavigationService.DataManager.Instruments, typeof(WinCoreUI.Views.InstrumentsView) },
      { INavigationService.DataManager.InstrumentGroups, typeof(WinCoreUI.Views.InstrumentGroupsView) },
      //{ INavigationService.DataManager.FundamentalData, typeof(BlankView) },
      { INavigationService.DataManager.InstrumentData, typeof(WinCoreUI.Views.InstrumentDataView) },
      { INavigationService.DataManager.TaskScheduling,  typeof(WinCoreUI.Views.TaskSchedulingView) },
      { INavigationService.DataManager.Settings, typeof(WinDataManager.Views.SettingsView) }
    };

    //constructors
    public MainWindowViewModel(IInitNavigationService initNavigationService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService) 
    {
      m_initNavigationService = initNavigationService;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] string m_statusMessage;

    //methods
    public void SetNavigationFrame(Frame frame) => m_initNavigationService.Initialize(frame, m_pages);

    public void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
      if (args.SelectedItem is NavigationViewItem navigationItem)
      {
        //clear any status information displayed
        StatusMessage = "";
        if (!string.IsNullOrEmpty(navigationItem.Tag.ToString()))
          m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
      }
    }

    public override void OnRefresh() => throw new NotImplementedException();
    public override void OnAdd() => throw new NotImplementedException();
    public override void OnUpdate() => throw new NotImplementedException();
    public override Task OnDeleteAsync(object target) => throw new NotImplementedException();
    public override Task OnRefreshAsync() => throw new NotImplementedException();
    public override void OnClearSelection() => throw new NotImplementedException();
    public override Task OnCopyAsync(object target) => throw new NotImplementedException();
    public override Task OnImportAsync() => throw new NotImplementedException();
    public override Task OnExportAsync() => throw new NotImplementedException();
  }
}
