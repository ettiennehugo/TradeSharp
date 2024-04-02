using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Inteface of view models that support viewing of items in a list fashion driven by an IListService.
  /// </summary>
  public interface IListViewModel<TItem>: INotifyPropertyChanged, INotifyPropertyChanging, IRefreshable
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    IList<TItem> Items { get; set; }
    Guid ParentId { get; set; }
    TItem? SelectedItem { get; set; }

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