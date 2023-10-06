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
  public abstract class MasterDetailViewModel<TItemViewModel, TItem> : ListViewModel<TItem>
    where TItem : class
    where TItemViewModel : IItemViewModel<TItem>
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public MasterDetailViewModel(IItemsService<TItem> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {

      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItemViewModel != null);
      DeleteCommand = new RelayCommand(OnDelete, () => SelectedItemViewModel != null);

      m_itemsService.Items.CollectionChanged += (sender, e) =>
      {
        OnPropertyChanged(nameof(ItemsViewModels));
      };
    }

    //finalizers


    //interface implementations


    //properties
    /// <summary>
    /// Returns the selected item view model for the detailed view.
    /// </summary>
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

    /// <summary>
    /// Returns the list of item view models for the detailed item views.
    /// </summary>
    public virtual IEnumerable<TItemViewModel> ItemsViewModels => Items.Select(item => ToViewModel(item));

    //methods
    protected abstract TItemViewModel ToViewModel(TItem item);
  }
}
