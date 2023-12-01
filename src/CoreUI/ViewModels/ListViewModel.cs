using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base class for models that support the viewing of items in a list supplied by an items service, commands are exposed to crete/update/delete items from the list.
  /// The model optionally uses a parent Id when lists of items needs to be displayed that are dependent on a specific parent Id from the IDataSourceService.
  /// </summary>
  public abstract partial class ListViewModel <TItem> : ViewModelBase
    where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly IListItemsService<TItem> m_itemsService;

    //constructors
    public ListViewModel(IListItemsService<TItem> itemsService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_itemsService = itemsService;
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      CopyCommand = new RelayCommand<object?>(OnCopy, (object? x) => SelectedItem != null);
    }

    //finalizers


    //interface implementations


    //properties


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
          NotifyCanExecuteChanged();
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
        //NOTE: You can not just change this item when it differs from value since the UI calls this method sometimes
        //      before the toolbar is ready to use and thus you end up with stale command button states.
        m_itemsService.SelectedItem = value;
        OnPropertyChanged();
        NotifyCanExecuteChanged();
      }
    }

    /// <summary>
    /// List of items maintained by the view model.
    /// </summary>
    public ObservableCollection<TItem> Items => m_itemsService.Items;

    //methods
    protected override Task OnRefreshAsync()
    {
      return m_itemsService.RefreshAsync();
    }

    public async override void OnCopy(object? target)
    {
      int count = 0;
      if (target is TItem)
      {
        TItem item = (TItem)target;
        Items.Remove(item);
        await m_itemsService.CopyAsync(item);
        SelectedItem = Items.FirstOrDefault();
        count++;
      }
      else if (target is IList)
      {
        IList items = (IList)target;
        foreach (TItem item in items)
        {
          await m_itemsService.CopyAsync(item);
          count++;
        }

        await OnRefreshAsync();
        SelectedItem = Items.FirstOrDefault();
      }
    }

    public async override void OnDelete(object? target)
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
    }

    public override void OnClearSelection()
    {
      SelectedItem = null;
    }

    //sub-classes can override these methods if the support import/export behavior
    public override void OnImport() => throw new NotImplementedException();
    public override void OnExport() => throw new NotImplementedException();
    
  }
}
