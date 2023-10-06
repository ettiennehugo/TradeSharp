using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.WinCoreUI.Views;
using TradeSharp.WinDataManager.Services;
using TradeSharp.WinDataManager.Views;

namespace TradeSharp.WinDataManager.ViewModels
{
  /// <summary>
  /// View model class to facilitate the navigation between the different views of the data manager application.
  /// </summary>
  public class MainWindowViewModel : ViewModelBase
  {
    //constants


    //enums


    //types


    //attributes
    private readonly InitNavigationService m_initNavigationService;

    /// <summary>
    /// Page configuration used by the navigation service.
    /// </summary>
    private readonly Dictionary<string, Type> m_pages = new Dictionary<string, Type>()
    {
      { INavigationService.DataManager.Brokers, typeof(BlankView) },
      { INavigationService.DataManager.DataProviders, typeof(BlankView) },
      { INavigationService.DataManager.Countries, typeof(WinCoreUI.Views.CountriesView) },
      { INavigationService.DataManager.Exchanges, typeof(WinCoreUI.Views.ExchangesView) },
      { INavigationService.DataManager.Fundamentals, typeof(BlankView) },
      { INavigationService.DataManager.Instruments, typeof(BlankView) },
      { INavigationService.DataManager.InstrumentGroups, typeof(BlankView) },
      { INavigationService.DataManager.FundamentalData, typeof(BlankView) },
      { INavigationService.DataManager.InstrumentData, typeof(BlankView) },
      { INavigationService.DataManager.Settings, typeof(BlankView) }
    };

    //constructors
    public MainWindowViewModel(InitNavigationService initNavigationService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService) 
    {
      m_initNavigationService = initNavigationService;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void SetNavigationFrame(Frame frame) => m_initNavigationService.Initialize(frame, m_pages);

    public void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
      if (args.SelectedItem is NavigationViewItem navigationItem)
      {
        switch (navigationItem.Tag)
        {
          case INavigationService.DataManager.Brokers:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.DataProviders:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.Countries:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.Exchanges:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.Fundamentals:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.Instruments:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.InstrumentGroups:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.FundamentalData:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.InstrumentData:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          case INavigationService.DataManager.Settings:
            m_navigationService.NavigateToAsync(navigationItem.Tag.ToString());
            break;
          default:
            break;
        }
      }
    }
  }
}
