using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TradeSharp.CoreUI
{
  /// <summary>
  /// Class to implement editability around specific item data types. 
  /// </summary>
  public abstract class EditableItemViewModel<TItem> : ItemViewModel<TItem>, IEditableObject
      where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    private readonly IItemsService<TItem> m_itemsService;
    private bool m_isEditMode;
    private TItem? m_editItem;

    //constructors
    public EditableItemViewModel(IItemsService<TItem> itemsService)
        : base(itemsService.SelectedItem)
    {
      m_itemsService = itemsService;

      PropertyChanged += (sender, e) =>
      {
        if (e.PropertyName == nameof(Item))
        {
          OnPropertyChanged(nameof(EditItem));
        }
      };

      EditCommand = new RelayCommand(BeginEdit, () => IsReadMode);
      CancelCommand = new RelayCommand(CancelEdit, () => IsEditMode);
      SaveCommand = new RelayCommand(EndEdit, () => IsEditMode);
      AddCommand = new RelayCommand(OnAdd, () => IsReadMode);
    }

    //finalizers


    //interface implementations


    //properties
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SaveCommand { get; }

    public bool IsReadMode => !IsEditMode;
    public bool IsEditMode
    {
      get => m_isEditMode;
      set
      {
        if (SetProperty(ref m_isEditMode, value))
        {
          OnPropertyChanged(nameof(IsReadMode));
          CancelCommand.NotifyCanExecuteChanged();
          SaveCommand.NotifyCanExecuteChanged();
          EditCommand.NotifyCanExecuteChanged();
        }
      }
    }

    public TItem? EditItem
    {
      get => m_editItem ?? Item;
      set => SetProperty(ref m_editItem, value);
    }

    //methods
    public abstract TItem CreateCopy(TItem item);

    public abstract Task OnSaveAsync();
    public virtual Task OnEndEditAsync() => Task.CompletedTask;
    protected abstract void OnAdd();

    public virtual void BeginEdit()
    {
      if (Item is null) throw new InvalidOperationException("Item is null");

      IsEditMode = true;
      TItem itemCopy = CreateCopy(Item);
      if (itemCopy != null)
      {
        EditItem = itemCopy;
      }
    }

    public async virtual void CancelEdit()
    {
      IsEditMode = false;
      EditItem = default;
      await m_itemsService.RefreshAsync();
      await OnEndEditAsync();
    }

    public async virtual void EndEdit()
    {
      using var _ = StartInProgress();
      await OnSaveAsync();
      EditItem = default;
      IsEditMode = false;
      await m_itemsService.RefreshAsync();
      await OnEndEditAsync();
    }
  }
}
