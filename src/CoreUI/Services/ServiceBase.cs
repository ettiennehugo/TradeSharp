using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Common base class for services.
  /// </summary>
  public class ServiceBase : ObservableObject, IRefreshable
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;

    //constructors
    public ServiceBase(IDialogService dialogService)
    {
      m_dialogService = dialogService;
    }

    //finalizers


    //interface implementations


    //properties


    //events
    public event IRefreshable.RefreshEventHandler? RefreshEvent;

    //methods
    protected virtual void RaiseRefreshEvent()
    {
      RefreshEvent?.Invoke(this, RefreshEventArgs.Empty);
    }

    protected virtual void RaiseRefreshEvent(RefreshEventArgs e)
    {
      RefreshEvent?.Invoke(this, e);
    }
  }
}
