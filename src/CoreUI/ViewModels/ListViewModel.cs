using CommunityToolkit.Mvvm.Input;
using System.Collections;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;
using TradeSharp.CoreUI.Events;
using System.Diagnostics;

namespace TradeSharp.CoreUI.ViewModels
{
    /// <summary>
    /// Base class for models that support the viewing of items in a list supplied by an items service, commands are exposed to crete/update/delete items from the list.
    /// The model optionally uses a parent Id when lists of items needs to be displayed that are dependent on a specific parent Id from the IDataSourceService.
    /// IMPORTANT: The model assumes synchronous operation in the UI thread so it does not do anything fancy to support background worker threads, for services that require
    ///            long running background processes the structure of the refresh, refresh async and Items properties.
    /// </summary>
    public abstract partial class ListViewModel<TItem> : ViewModelBase, IListViewModel<TItem> where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected readonly IListService<TItem> m_itemsService;

    //constructors
    public ListViewModel(IListService<TItem> itemsService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_itemsService = itemsService;
      m_itemsService.RefreshEvent += onServiceRefresh;
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null);
      CopyCommandAsync = new AsyncRelayCommand<object?>(OnCopyAsync, (object? x) => SelectedItem != null);
      m_itemsService.RefreshEvent += onServiceRefresh;
    }

    //finalizers
    ~ListViewModel()
    {
      m_itemsService.RefreshEvent -= onServiceRefresh;
    }

    //interface implementations


    //events


    //properties
    public LoadedState LoadedState { get => m_itemsService.LoadedState; }
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
          OnPropertyChanged(PropertyName.ParentId);
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
        if (m_itemsService.SelectedItem != value)
        {
          m_itemsService.SelectedItem = value;
          OnPropertyChanged(PropertyName.SelectedItem);
          NotifyCanExecuteChanged();
        }
      }
    }

    /// <summary>
    /// List of items maintained by the view model, do note the following:
    /// * for large collections this would need to be virtualized, e.g. minute bar data.
    /// * if the m_itemsService is refreshed in a background worker thread you need to transfer the data in a synchronous manner to the view model which executes in the UI thread
    ///   and the view model would most likely contain a deep copy of the data currently "in view" of the user with other data paged out (deleted) from the view model.
    /// * the below definition assumes synchronous operation with the UI thread, needs to be redefined in large view models.
    /// </summary>
    public virtual IList<TItem> Items { get => m_itemsService.Items; set => m_itemsService.Items = value; }

    //methods
    /// <summary>
    /// Default list view model only supports synchronous refresh.
    /// </summary>
    public override void OnRefresh()
    {
      m_itemsService.Refresh();
    }

    /// <summary>
    /// Default list view model only support synchronous refresh.
    /// </summary>
    /// <returns></returns>
    public override Task OnRefreshAsync()
    {
      return Task.Run(OnRefresh);
    }

    public override Task OnCopyAsync(object? target)
    {
      return Task.Run(() =>
      {
        int count = 0;
        if (target is TItem)
        {
          TItem item = (TItem)target;
          Items.Remove(item);
          m_itemsService.Copy(item);
          SelectedItem = Items.FirstOrDefault();
          count++;
        }
        else if (target is IList)
        {
          IList items = (IList)target;
          foreach (TItem item in items)
          {
            m_itemsService.Copy(item);
            count++;
          }

          OnRefresh();
          SelectedItem = Items.FirstOrDefault();
        }
      });
    }

    public override Task OnDeleteAsync(object? target)
    {
      return Task.Run(() =>
      {
        int count = 0;
        if (target is TItem item)
        {
          Items.Remove(item);
          m_itemsService.Delete(item);
          count++;
        }
        else if (target is IList<TItem> items)
        {
          foreach (TItem it in items)
          {
            m_itemsService.Delete(it);
            count++;
          }

          OnRefresh();
        }
        else if (SelectedItem != null)
        {
          m_dialogService.PostUIUpdate(() => {
            m_itemsService.Delete(SelectedItem);
            Items.Remove(SelectedItem);
            SelectedItem = Items.FirstOrDefault();
            count++;
          });
        }
        else
          //selected item is bound to target that can not be converted to the object selection
          Debugger.Break();
      });
    }

    public override void OnClearSelection()
    {
      SelectedItem = null;
    }

    //sub-classes can override these methods if they support import/export behavior
    public override Task OnImportAsync() => throw new NotImplementedException();
    public override Task OnExportAsync() => throw new NotImplementedException();

    ///Generic handler to re-raise the service refresh event as a view model refresh event.
    protected virtual void onServiceRefresh(object? sender, RefreshEventArgs e)
    {
      raiseRefreshEvent(e);
    }
  }
}
