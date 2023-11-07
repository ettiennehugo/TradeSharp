using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
      AddCommand = new RelayCommand(OnAdd);
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedNode != null);
      DeleteCommand = new RelayCommand(OnDelete, () => SelectedNode != null || SelectedNodes.Count > 0);
      RefreshCommand = new RelayCommand(OnRefresh);
      RefreshCommandAsync = new AsyncRelayCommand(OnRefreshAsync);
      CopyCommand = new RelayCommand(OnCopy);
      ImportCommand = new RelayCommand(OnImport);
      ExportCommand = new RelayCommand(OnExport);
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
        AddCommand.NotifyCanExecuteChanged();
        UpdateCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        RefreshCommandAsync.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
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
        AddCommand.NotifyCanExecuteChanged();
        UpdateCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        RefreshCommandAsync.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
      }
    }

    /// <summary>
    /// Returns the set of defined nodes.
    /// </summary>
    public ObservableCollection<ITreeNodeType<TKey, TItem>> Nodes => m_itemsService.Nodes;

    /// <summary>
    /// Set of supported operation.
    /// </summary>
    public RelayCommand AddCommand { get; internal set; }
    public RelayCommand UpdateCommand { get; internal set; }
    public RelayCommand DeleteCommand { get; internal set; }
    public RelayCommand RefreshCommand { get; internal set; }
    public AsyncRelayCommand RefreshCommandAsync { get; internal set; }
    public RelayCommand CopyCommand { get; internal set; }
    public RelayCommand ImportCommand { get; internal set; }
    public RelayCommand ExportCommand { get; internal set; }

    //methods
    public async void OnRefresh()
    {
      await OnRefreshAsync();
    }

    protected async Task OnRefreshAsync()
    {
      StartInProgress();
      await m_itemsService.RefreshAsync();
    }

    public abstract void OnAdd();
    public abstract void OnUpdate();
    public abstract void OnCopy();

    public async void OnDelete()
    {
      int count = 0;

      if (SelectedNodes.Count > 0)
      {
        foreach (ITreeNodeType<TKey, TItem> node in SelectedNodes)
        {
          await m_itemsService.DeleteAsync(node);
          count++;
        }
      }
      else if (SelectedNode != null)
      {
        await m_itemsService.DeleteAsync(SelectedNode);
        count++;
      }

      await OnRefreshAsync();
      SelectedNode = Nodes.FirstOrDefault();

      IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
      if (count > 0)
        await dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Success, "Success", $"Deleted {count} nodes with it's children");
      else
        await dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "Failure", $"Deleted {count} nodes");
    }

    public async void OnImport()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentGroupsAsync();

      if (importSettings != null)
      {
        int importCount = await m_itemsService.ImportAsync(importSettings.Filename, importSettings.ImportReplaceBehavior);
        await m_dialogService.ShowStatusMessageAsync(importCount == 0 ? IDialogService.StatusMessageSeverity.Warning : IDialogService.StatusMessageSeverity.Success, "", $"Imported {importCount} instrument groups");
        await OnRefreshAsync();
      }
    }

    public async void OnExport()
    {
      string? filename = await m_dialogService.ShowExportInstrumentGroupsAsync();

      if (filename != null)
      {
        int exportCount = await m_itemsService.ExportAsync(filename);
        await m_dialogService.ShowStatusMessageAsync(exportCount == 0 ? IDialogService.StatusMessageSeverity.Warning : IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} instrument groups");
      }
    }
  }
}
