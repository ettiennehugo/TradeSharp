using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base class for all view models.
  /// </summary>
  public abstract partial class ViewModelBase : ObservableObject
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly INavigationService m_navigationService;
    protected readonly IDialogService m_dialogService;

    //constructors
    public ViewModelBase(INavigationService navigationService, IDialogService dialogService)
    {
      m_navigationService = navigationService;
      m_dialogService = dialogService;
      StatusMessage = "";
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] string m_statusMessage;

    //methods


  }
}
