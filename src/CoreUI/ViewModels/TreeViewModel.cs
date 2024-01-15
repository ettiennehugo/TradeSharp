using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base calss for models that support viewing of items in a tree fashion driven by an ITreeItemService.
  /// </summary>
  public abstract class TreeViewModel<TKey, TItem> : ViewModelBase
    where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected ITreeItemsService<TKey, TItem> m_itemsService;

    //constructors
    public TreeViewModel(ITreeItemsService<TKey, TItem> itemService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_itemsService = itemService;
      m_itemsService.RefreshEvent += onServiceRefresh;
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedNode != null || SelectedNodes.Count > 0);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedNode != null || SelectedNodes.Count > 0);
      ClearSelectionCommand = new RelayCommand(OnClearSelection, () => SelectedNode != null | SelectedNodes.Count > 0);
    }

    //finalizers


    //interface implementations


    //properties
    /// <summary>
    /// Get/set single selected node for the view model and associated item service.
    /// </summary>
    public virtual ITreeNodeType<TKey, TItem>? SelectedNode
    {
      get => m_itemsService.SelectedNode;
      set
      {
        //NOTE: You can not just change this item when it differs from value since the UI calls this method sometimes
        //      before the toolbar is ready to use and thus you end up with stale command button states.
        m_itemsService.SelectedNode = value;
        OnPropertyChanged();
        NotifyCanExecuteChanged();
      }
    }

    /// <summary>
    /// Get/set selected set of nodes for the view model and associated items service. 
    /// </summary>
    public ObservableCollection<ITreeNodeType<TKey, TItem>> SelectedNodes
    {
      get => m_itemsService.SelectedNodes;
      set
      {
        //NOTE: You can not just change this item when it differs from value since the UI calls this method sometimes
        //      before the toolbar is ready to use and thus you end up with stale command button states.
        m_itemsService.SelectedNodes = value;
        OnPropertyChanged();
        NotifyCanExecuteChanged();
      }
    }

    /// <summary>
    /// Returns the set of defined nodes in the tree.
    /// </summary>
    public ObservableCollection<ITreeNodeType<TKey, TItem>> Nodes => m_itemsService.Nodes;

    //methods
    public override void OnClearSelection()
    {
      SelectedNode = null;
      SelectedNodes.Clear();
    }

    //methods
    /// <summary>
    /// Default tree view model only supports synchronous refresh.
    /// </summary>
    public override void OnRefresh()
    {
      m_itemsService.Refresh();
    }

    /// <summary>
    /// Default tree view model only support synchronous refresh.
    /// </summary>
    /// <returns></returns>
    public override Task OnRefreshAsync() => throw new NotImplementedException($"{GetType().ToString()} view model only supports synchronous refresh.");

    public override Task OnDeleteAsync(object? target)
    {
      return Task.Run(async () =>
      {
        int count = 0;

        if (SelectedNodes.Count > 0)
        {
          foreach (ITreeNodeType<TKey, TItem> node in SelectedNodes)
          {
            m_itemsService.Delete(node);
            count++;
          }
        }
        else if (SelectedNode != null)
        {
          m_itemsService.Delete(SelectedNode);
          count++;
        }

        OnRefresh();
        SelectedNode = Nodes.FirstOrDefault();

        IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
        if (count > 0)
          await dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "Success", $"Deleted {count} nodes with it's children");
        else
          await dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "Failure", $"Deleted {count} nodes");
      });
    }

    public override async Task OnImportAsync()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentGroupsAsync();
      if (importSettings != null) _ = Task.Run(() => m_itemsService.Import(importSettings));
    }

    public override async Task OnExportAsync()
    {
        string? filename = await m_dialogService.ShowExportInstrumentGroupsAsync();
        if (filename != null) _ = Task.Run(() => m_itemsService.Export(filename));
    }

    ///Generic handler to re-raise the service refresh event as a view model refresh event.
    protected virtual void onServiceRefresh(object? sender, Common.RefreshEventArgs e)
    {
      RaiseRefreshEvent(e);
    }
  }
}
