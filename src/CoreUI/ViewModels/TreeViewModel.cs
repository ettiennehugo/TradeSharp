using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Base calss for models that support viewing of items in a tree fashion driven by an ITreeItemService.
  /// </summary>
  public abstract class TreeViewModel<TKey, TItem> : ViewModelBase, ITreeViewModel<TKey, TItem> where TItem : class
  {
    //constants


    //enums


    //types


    //attributes
    protected ITreeItemsService<TKey, TItem> m_itemsService;
    protected string m_findText;

    //constructors
    public TreeViewModel(ITreeItemsService<TKey, TItem> itemService, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_itemsService = itemService;
      m_itemsService.RefreshEvent += onServiceRefresh;
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedNode != null || SelectedNodes.Count > 0);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedNode != null || SelectedNodes.Count > 0);
      ExpandNodeCommand = new RelayCommand<object?>(OnExpandNode);
      CollapseNodeCommand = new RelayCommand<object?>(OnCollapseNode);
      ClearFilterCommand = new RelayCommand(OnClearFilter, () => FindText.Length > 0);
      FindFirstCommand = new RelayCommand(OnFindFirst, () => FindText.Length > 0);
      FindNextCommand = new RelayCommand(OnFindNext, () => FindText.Length > 0);
      FindPreviousCommand = new RelayCommand(OnFindPrevious, () => FindText.Length > 0);
      m_findText = string.Empty;
      ClearSelectionCommand = new RelayCommand(OnClearSelection, () => SelectedNode != null || SelectedNodes.Count > 0);
      m_itemsService.RefreshEvent += onServiceRefresh;
    }

    //finalizers
    ~TreeViewModel()
    {
      m_itemsService.RefreshEvent -= onServiceRefresh;
    }

    //interface implementations


    //properties
    /// <summary>
    /// Commands to fire when a node is expanded or collapsed.
    /// </summary>
    public RelayCommand<object?> ExpandNodeCommand { get; set; }
    public RelayCommand<object?> CollapseNodeCommand { get; set; }

    /// <summary>
    /// Find the first/next node in the tree that matches the find text.
    /// </summary>
    public RelayCommand ClearFilterCommand { get; set; }
    public RelayCommand FindFirstCommand { get; set; }
    public RelayCommand FindNextCommand { get; set; }
    public RelayCommand FindPreviousCommand { get; set; }
    public virtual string FindText
    {
      get => m_findText;
      set
      {
        m_findText = value;
        OnPropertyChanged(PropertyName.FindText);
        NotifyCanExecuteChanged();
      }
    }

    /// <summary>
    /// Get/set single selected node for the view model and associated item service.
    /// </summary>
    public virtual ITreeNodeType<TKey, TItem>? SelectedNode 
    { 
      get => m_itemsService.SelectedNode;
      set
      {
        m_itemsService.SelectedNode = value;
        OnPropertyChanged(PropertyName.SelectedNode);
        NotifyCanExecuteChanged();
      }
    }

    /// <summary>
    /// Get/set selected set of nodes for the view model and associated items service. 
    /// </summary>
    public virtual ObservableCollection<ITreeNodeType<TKey, TItem>> SelectedNodes
    { 
      get => m_itemsService.SelectedNodes;
      set
      {
        m_itemsService.SelectedNodes = value;
        OnPropertyChanged(PropertyName.SelectedNodes);
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
    public override Task OnRefreshAsync()
    {
      return Task.Run(() => m_itemsService.Refresh());
    }

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

        if (count > 0)
        {
          raiseRefreshEvent();
          SelectedNode = Nodes.FirstOrDefault();
          await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "Success", $"Deleted {count} nodes with it's children");
        }
        else
          await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "Failure", $"Deleted {count} nodes");
      });
    }

    public override async Task OnImportAsync()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentGroupsAsync();
      if (importSettings != null) _ = Task.Run(() => m_itemsService.Import(importSettings));
    }

    public override async Task OnExportAsync()
    {
      ExportSettings? exportSettings = await m_dialogService.ShowExportInstrumentGroupsAsync();
      if (exportSettings != null) _ = Task.Run(() => m_itemsService.Export(exportSettings));
    }

    /// <summary>
    /// Abstract definitions for search commands.
    /// </summary>
    public abstract void OnClearFilter();
    public abstract void OnFindFirst();
    public abstract void OnFindNext();
    public abstract void OnFindPrevious();

    /// <summary>
    /// Default implementations for the expand and collapse node commands.
    /// </summary>
    public virtual void OnExpandNode(object? node) { }
    public virtual void OnCollapseNode(object? node) { }

    ///Generic handler to re-raise the service refresh event as a view model refresh event.
    protected virtual void onServiceRefresh(object? sender, Common.RefreshEventArgs e)
    {
      raiseRefreshEvent(e);
    }

    protected override void NotifyCanExecuteChanged()
    {
      base.NotifyCanExecuteChanged();
      FindFirstCommand.NotifyCanExecuteChanged();
      FindNextCommand.NotifyCanExecuteChanged();
      FindPreviousCommand.NotifyCanExecuteChanged();
    }
  }
}
