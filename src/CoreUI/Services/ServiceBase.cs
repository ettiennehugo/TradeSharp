using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Common base class for services.
  /// </summary>
  public partial class ServiceBase : ObservableObject
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
      LoadedState = LoadedState.NotLoaded;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] LoadedState m_loadedState;

    //events
    public event RefreshEventHandler? RefreshEvent;

    //methods
    public virtual void Import(ImportSettings importSettings) => throw new NotImplementedException();
    public virtual void Export(ExportSettings exportSettings) => throw new NotImplementedException();

    protected virtual void raiseRefreshEvent()
    {
      RefreshEvent?.Invoke(this, RefreshEventArgs.Empty);
    }

    protected virtual void raiseRefreshEvent(RefreshEventArgs e)
    {
      RefreshEvent?.Invoke(this, e);
    }
  }
}
