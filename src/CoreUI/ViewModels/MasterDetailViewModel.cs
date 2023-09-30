using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
    /// <summary>
    /// Base class for models that support viewing items in a master view list and then selecting an item for a detailed view using the specific item view model. Commands are
    /// exposed to add/delete items from the master list and refresh the list.
    /// </summary>
    public abstract class MasterDetailViewModel<TItemViewModel, TItem> : ViewModelBase
    where TItemViewModel : IItemViewModel<TItem>
    where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly IItemsService<TItem> m_itemsService;
    protected TItemViewModel? m_selectedItemViewModel;

    //constructors
    public MasterDetailViewModel(IItemsService<TItem> itemsService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_itemsService = itemsService;
      m_itemsService.Items.CollectionChanged += (sender, e) =>
      {
        OnPropertyChanged(nameof(ItemsViewModels));
      };

      AddCommand = new RelayCommand(OnAdd);
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand(OnDelete, () => SelectedItem != null);
      RefreshCommand = new RelayCommand(OnRefresh);
      RefreshCommandAsync = new AsyncRelayCommand(OnRefreshAsync);
    }

    //finalizers


    //interface implementations


    //properties
    public RelayCommand AddCommand { get; internal set; }
    public RelayCommand UpdateCommand { get; internal set; }
    public RelayCommand DeleteCommand { get; internal set; }
    public RelayCommand RefreshCommand { get; internal set; }
    public AsyncRelayCommand RefreshCommandAsync { get; internal set; }

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
    
    public ObservableCollection<TItem> Items => m_itemsService.Items;
    public virtual IEnumerable<TItemViewModel> ItemsViewModels => Items.Select(item => ToViewModel(item));

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

    public virtual TItemViewModel? SelectedItemViewModel
    {
      get
      {
        var selectedItem = m_itemsService.SelectedItem;
        if (selectedItem is null) return default;
        return ToViewModel(selectedItem);
      }

      set
      {
        if (!EqualityComparer<TItem>.Default.Equals(SelectedItem, value?.Item))
        {
          SelectedItem = value?.Item;
          OnPropertyChanged();
        }
      }
    }

    //methods
    protected abstract TItemViewModel ToViewModel(TItem item);

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
    public abstract void OnDelete();
  }
}
