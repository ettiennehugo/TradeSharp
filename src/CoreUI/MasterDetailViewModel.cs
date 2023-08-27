using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI;

namespace CoreUI
{
  /// <summary>
  /// Base class for models that support viewing items in a master view vs detail view setup.
  /// </summary>
  public abstract class MasterDetailViewModel<TItemViewModel, TItem> : ViewModelBase
    where TItemViewModel : IItemViewModel<TItem>
    where TItem : class
  {

    //constants


    //enums


    //types


    //attributes
    private readonly IItemsService<TItem> _itemsService;
    protected TItem? _selectedItem;
    protected TItemViewModel? _selectedItemViewModel;

    //constructors
    public MasterDetailViewModel(IItemsService<TItem> itemsService)
    {
      _itemsService = itemsService;

      _itemsService.Items.CollectionChanged += (sender, e) =>
      {
        OnPropertyChanged(nameof(ItemsViewModels));
      };

      RefreshCommand = new RelayCommand(OnRefresh);
      AddCommand = new RelayCommand(OnAdd);
    }


    //finalizers


    //interface implementations


    //properties
    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }

    public ObservableCollection<TItem> Items => _itemsService.Items;
    public virtual IEnumerable<TItemViewModel> ItemsViewModels => Items.Select(item => ToViewModel(item));

    public virtual TItem? SelectedItem
    {
      get => _itemsService.SelectedItem;
      set
      {
        if (!EqualityComparer<TItem>.Default.Equals(_itemsService.SelectedItem, value))
        {
          _itemsService.SelectedItem = value;
          OnPropertyChanged();
        }
      }
    }

    public virtual TItemViewModel? SelectedItemViewModel
    {
      get
      {
        var selectedItem = _itemsService.SelectedItem;
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
      await _itemsService.RefreshAsync();
    }

    public abstract void OnAdd();
  }
}
