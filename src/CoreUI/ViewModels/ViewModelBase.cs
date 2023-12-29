using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base class for all view models, the view model exists in the UI Thread and is responsible to make sure that the
  /// UI thread correctly represents the state of the model that can run in the UI thread or a background thread. Refresh
  /// operations specifically need to run in the background if they are long running operations while quick refreshes of
  /// only a few items can run in the UI thread to keep things simple.
  /// </summary>
  public abstract partial class ViewModelBase : ObservableObject
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly INavigationService m_navigationService;    //TBD: Remove this service, it is not used.
    protected readonly IDialogService m_dialogService;

    //constructors
    public ViewModelBase(INavigationService navigationService, IDialogService dialogService)
    {
      m_navigationService = navigationService;
      m_dialogService = dialogService;
      //create the set of supported commands, sub-classes need to recreate these commands if the logic changes to control their enabled state
      AddCommand = new RelayCommand(OnAdd);
      UpdateCommand = new RelayCommand(OnUpdate);
      DeleteCommand = new RelayCommand<object?>(OnDelete);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync);
      ClearSelectionCommand = new RelayCommand(OnClearSelection);
      CopyCommandAsync = new AsyncRelayCommand<object?>(OnCopyAsync);
      RefreshCommand = new RelayCommand(OnRefresh);
      RefreshCommandAsync = new AsyncRelayCommand(OnRefreshAsync);
      ImportCommandAsync = new AsyncRelayCommand(OnImportAsync);
      ExportCommandAsync = new AsyncRelayCommand(OnExportAsync);
    }

    //finalizers


    //interface implementations


    //properties
    /// <summary>
    /// Set of supported operations.
    /// </summary>
    public RelayCommand AddCommand { get; internal set; }
    public RelayCommand UpdateCommand { get; internal set; }
    public RelayCommand<object?> DeleteCommand { get; internal set; }
    public AsyncRelayCommand<object?> DeleteCommandAsync { get; internal set; } //use async delete to allow long running deletes to run in the background
    public RelayCommand ClearSelectionCommand { get; internal set; }
    public RelayCommand RefreshCommand { get; internal set; }
    public AsyncRelayCommand RefreshCommandAsync { get; internal set; } //use async refresh to allow long running refreshes to run in the background
    public AsyncRelayCommand<object?> CopyCommandAsync { get; internal set; }
    public AsyncRelayCommand ImportCommandAsync { get; internal set; }
    public AsyncRelayCommand ExportCommandAsync { get; internal set; }

    //methods
    public abstract void OnAdd();
    public abstract void OnUpdate();
    
    public virtual async void OnDelete(object? target)
    {
      await OnDeleteAsync(target);
    }
    public abstract Task OnDeleteAsync(object? target);

    public abstract void OnRefresh();
    public abstract Task OnRefreshAsync();
    
    public abstract void OnClearSelection();
    public abstract Task OnCopyAsync(object? target);
    public abstract Task OnImportAsync();
    public abstract Task OnExportAsync();

    /// <summary>
    /// Run refresh of the command states, call this method during any propery updates to ensure commands
    /// update based on property states.
    /// </summary>
    protected void NotifyCanExecuteChanged()
    {
      AddCommand.NotifyCanExecuteChanged();
      UpdateCommand.NotifyCanExecuteChanged();
      DeleteCommand.NotifyCanExecuteChanged();
      ClearSelectionCommand.NotifyCanExecuteChanged();
      RefreshCommand.NotifyCanExecuteChanged();
      RefreshCommandAsync.NotifyCanExecuteChanged();
      CopyCommandAsync.NotifyCanExecuteChanged();
      ImportCommandAsync.NotifyCanExecuteChanged();
      ExportCommandAsync.NotifyCanExecuteChanged();
    }
  }
}
