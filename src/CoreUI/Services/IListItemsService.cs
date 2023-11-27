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

    //events
    event EventHandler<T?>? SelectedItemChanged;

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
