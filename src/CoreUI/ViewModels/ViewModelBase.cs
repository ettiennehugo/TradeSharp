using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base class for all view models, sets up the general interface through which view models operate with the
  /// UI components using bindings.
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
      //create the set of supported commands, sub-classes need to recreate these commands if the logic changes to control their enabled state
      AddCommand = new RelayCommand(OnAdd);
      UpdateCommand = new RelayCommand(OnUpdate);
      DeleteCommand = new RelayCommand<object?>(OnDelete);
      ClearSelectionCommand = new RelayCommand(OnClearSelection);
      CopyCommand = new RelayCommand<object?>(OnCopy);
      RefreshCommand = new RelayCommand(OnRefresh);
      RefreshCommandAsync = new AsyncRelayCommand(OnRefreshAsync);
      ImportCommand = new RelayCommand(OnImport);
      ExportCommand = new RelayCommand(OnExport);
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
    public RelayCommand ClearSelectionCommand { get; internal set; }
    public RelayCommand RefreshCommand { get; internal set; }
    public AsyncRelayCommand RefreshCommandAsync { get; internal set; }
    public RelayCommand<object?> CopyCommand { get; internal set; }
    public RelayCommand ImportCommand { get; internal set; }
    public RelayCommand ExportCommand { get; internal set; }

    //methods
    public abstract void OnAdd();
    public abstract void OnUpdate();
    public abstract void OnDelete(object? target);
    
    public virtual async void OnRefresh()
    {
      await OnRefreshAsync();
    }
    
    protected abstract Task OnRefreshAsync();
    public abstract void OnClearSelection();
    public abstract void OnCopy(object? target);
    public abstract void OnImport();
    public abstract void OnExport();

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
      CopyCommand.NotifyCanExecuteChanged();
      ImportCommand.NotifyCanExecuteChanged();
      ExportCommand.NotifyCanExecuteChanged();
    }
  }
}
