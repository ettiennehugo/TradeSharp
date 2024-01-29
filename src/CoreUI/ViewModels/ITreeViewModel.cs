using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Interface for view models that allow tree type access. 
  /// </summary>
  public interface ITreeViewModel<TKey, TItem>: IRefreshable where TItem : class
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    ObservableCollection<ITreeNodeType<TKey, TItem>> Nodes { get; }
    ITreeNodeType<TKey, TItem>? SelectedNode { get; set; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> SelectedNodes { get; set; }

    public RelayCommand AddCommand { get; set; }
    public RelayCommand UpdateCommand { get; set; }
    public RelayCommand<object?> DeleteCommand { get; set; }
    public AsyncRelayCommand<object?> DeleteCommandAsync { get; set; } //use async delete to allow long running deletes to run in the background
    public RelayCommand ClearSelectionCommand { get; set; }
    public RelayCommand RefreshCommand { get; set; }
    public AsyncRelayCommand RefreshCommandAsync { get; set; } //use async refresh to allow long running refreshes to run in the background
    public AsyncRelayCommand<object?> CopyCommandAsync { get; set; }
    public AsyncRelayCommand ImportCommandAsync { get; set; }
    public AsyncRelayCommand ExportCommandAsync { get; set; }

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
  }
}