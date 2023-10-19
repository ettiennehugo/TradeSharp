using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base class for models that support the viewing of items in a list supplied by an items service, commands are exposed to crete/update/delete items from the list.
  /// The model optionally uses a parent Id when lists of items needs to be displayed that are dependent on a specific parent Id from the IDataSourceService.
  /// </summary>
  public abstract class ListViewModel <TItem> : ViewModelBase
    where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly IItemsService<TItem> m_itemsService;

    //constructors
    public ListViewModel(IItemsService<TItem> itemsService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_itemsService = itemsService;
      AddCommand = new RelayCommand(OnAdd);
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      RefreshCommand = new RelayCommand(OnRefresh);
      RefreshCommandAsync = new AsyncRelayCommand(OnRefreshAsync);
    }

    //finalizers


    //interface implementations


    //properties
    public RelayCommand AddCommand { get; internal set; }
    public RelayCommand UpdateCommand { get; internal set; }
    public RelayCommand<object?> DeleteCommand { get; internal set; }
    public RelayCommand RefreshCommand { get; internal set; }
    public AsyncRelayCommand RefreshCommandAsync { get; internal set; }

    /// <summary>
    /// ParentId used when items displayed are dependent on a specific parent object.
    /// </summary>
    public Guid ParentId
    {
      get => m_itemsService.ParentId;
      set
      {
        if (m_itemsService.ParentId != value)
        {
          m_itemsService.ParentId = value;
          AddCommand.NotifyCanExecuteChanged();
          UpdateCommand.NotifyCanExecuteChanged();
          DeleteCommand.NotifyCanExecuteChanged();
          RefreshCommand.NotifyCanExecuteChanged();
          RefreshCommandAsync.NotifyCanExecuteChanged();
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Selected item in the list view model.
    /// </summary>
    public virtual TItem? SelectedItem
    {
      get => m_itemsService.SelectedItem;
      set
      {
        if (!EqualityComparer<TItem>.Default.Equals(m_itemsService.SelectedItem, value))
        {
          m_itemsService.SelectedItem = value;
          OnPropertyChanged();
          AddCommand.NotifyCanExecuteChanged();
          UpdateCommand.NotifyCanExecuteChanged();
          DeleteCommand.NotifyCanExecuteChanged();
          RefreshCommand.NotifyCanExecuteChanged();
          RefreshCommandAsync.NotifyCanExecuteChanged();
        }
      }
    }

    /// <summary>
    /// List of items maintained by the view model.
    /// </summary>
    public ObservableCollection<TItem> Items => m_itemsService.Items;

    //methods
    public async void OnRefresh()
    {
      using (StartInProgress())
      {
        await OnRefreshAsync();
      }
    }

    protected async Task OnRefreshAsync()
    {
      StartInProgress();
      await m_itemsService.RefreshAsync();
    }

    public abstract void OnAdd();
    public abstract void OnUpdate();

    public async void OnDelete(object? target)
    {
      int count = 0;
      if (target is TItem)
      {
        TItem item = (TItem)target;
        Items.Remove(item);
        await m_itemsService.DeleteAsync(item);
        SelectedItem = Items.FirstOrDefault();
        count++;
      }
      else if (target is IList)
      {
        IList items = (IList)target;
        foreach (TItem item in items)
        {
          await m_itemsService.DeleteAsync(item);
          count++;
        }

        await OnRefreshAsync();
        SelectedItem = Items.FirstOrDefault();
      }

      IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
      await dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "Success", $"Deleted {count} items");
    }
  }
}
