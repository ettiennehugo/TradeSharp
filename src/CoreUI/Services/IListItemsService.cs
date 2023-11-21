using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to be implemented by services that allow the manipulation of items in a list fashion. Services are defined to support dependency injection in MVVM.
  /// The service overall supports viewing all the items and then viewing the details of a single item (SelectedItem)
  /// </summary>
  public interface IListItemsService<T>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid ParentId { get; set; }
    T? SelectedItem { get; set; }
    ObservableCollection<T> Items { get; }
    string StatusMessage { get; set; }  //status message for service operations
    double StatusProgressMin { get; set; }  //status progress minimum value for service operations
    double StatusProgressMax { get; set; }  //status progress maximum value for service operations
    double StatusProgressValue { get; set; }  //status progress value for service operations

    //events
    event EventHandler<T>? SelectedItemChanged;

    //methods
    Task RefreshAsync();
    Task<T> AddAsync(T item);
    Task<T> UpdateAsync(T item);
    Task<bool> DeleteAsync(T item);
    Task<T> CopyAsync(T item);
    Task<ImportReplaceResult> ImportAsync(ImportSettings importSettings);
    Task<long> ExportAsync(string filename);
  }
}
