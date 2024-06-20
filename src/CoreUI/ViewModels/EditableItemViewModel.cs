using TradeSharp.Common;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Extension of the read-only ItemViewModel that allows editing of the item, makes a deep copy of the item (into EditItem) when user selects to edit the item, allows modification and then
  /// saves the item or cancels editing.
  /// </summary>
  public abstract class EditableItemViewModel<TItem> : ItemViewModel<TItem>, IEditableObject
      where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly IListService<TItem> m_itemsService;
    private bool m_isEditMode;
    private TItem? m_editItem;

    //constructors
    public EditableItemViewModel(IListService<TItem> itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService.SelectedItem, navigationService, dialogService)
    {
      m_itemsService = itemsService;

      PropertyChanged += (sender, e) =>
      {
        if (e.PropertyName == PropertyName.Item)
        {
          OnPropertyChanged(PropertyName.EditItem);
        }
      };

      AddCommand = new RelayCommand(OnAdd, () => IsReadMode);
      EditCommand = new RelayCommand(BeginEdit, () => IsReadMode);
      CancelCommand = new RelayCommand(CancelEdit, () => IsEditMode);
      SaveCommand = new RelayCommand(EndEdit, () => IsEditMode);
    }

    //finalizers


    //interface implementations


    //properties
    public RelayCommand EditCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public bool IsReadMode => !IsEditMode;
    public bool IsEditMode
    {
      get => m_isEditMode;
      set
      {
        if (SetProperty(ref m_isEditMode, value))
        {
          OnPropertyChanged(PropertyName.IsReadMode);
          EditCommand.NotifyCanExecuteChanged();
          SaveCommand.NotifyCanExecuteChanged();
          CancelCommand.NotifyCanExecuteChanged();
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
      m_itemsService.Refresh();
      await OnEndEditAsync();
    }

    public async virtual void EndEdit()
    {
      await OnSaveAsync();
      EditItem = default;
      IsEditMode = false;
      m_itemsService.Refresh();
      await OnEndEditAsync();
    }
  }
}
