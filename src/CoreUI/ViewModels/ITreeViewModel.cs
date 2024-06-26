using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
    /// <summary>
    /// Interface for view models that allow tree type access. 
    /// </summary>
    public interface ITreeViewModel<TKey, TItem>: INotifyPropertyChanged, INotifyPropertyChanging where TItem : class
  {
    //constants

    //enums


    //types


    //attributes


    //events
    public event Events.RefreshEventHandler? RefreshEvent;

    //properties
    LoadedState LoadedState { get; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> Nodes { get; }
    ITreeNodeType<TKey, TItem>? SelectedNode { get; set; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> SelectedNodes { get; set; }
    string FindText { get; set; }

    RelayCommand AddCommand { get; set; }
    RelayCommand UpdateCommand { get; set; }
    RelayCommand<object?> DeleteCommand { get; set; }
    AsyncRelayCommand<object?> DeleteCommandAsync { get; set; } //use async delete to allow long running deletes to run in the background
    RelayCommand ClearSelectionCommand { get; set; }
    RelayCommand RefreshCommand { get; set; }
    AsyncRelayCommand RefreshCommandAsync { get; set; } //use async refresh to allow long running refreshes to run in the background
    AsyncRelayCommand<object?> CopyCommandAsync { get; set; }
    AsyncRelayCommand ImportCommandAsync { get; set; }
    AsyncRelayCommand ExportCommandAsync { get; set; }
    RelayCommand<object?> ExpandNodeCommand { get; set; }
    RelayCommand<object?> CollapseNodeCommand { get; set; }
    RelayCommand ClearFilterCommand { get; set; }
    RelayCommand FindFirstCommand { get; set; }
    RelayCommand FindNextCommand { get; set; }
    RelayCommand FindPreviousCommand { get; set; }

    //methods
    void OnAdd();
    void OnUpdate();
    void OnDelete(object? target);
    Task OnDeleteAsync(object? target);
    void OnClearSelection();
    Task OnCopyAsync(object? target);
    Task OnExportAsync();
    Task OnImportAsync();
    void OnRefresh();
    Task OnRefreshAsync();
    void OnClearFilter();
    void OnFindFirst();
    void OnFindNext();
    void OnFindPrevious();
  }
}