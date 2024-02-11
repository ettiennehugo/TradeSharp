using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to be implemented by services that allow the manipulation of items in a list fashion. Services are defined to support dependency injection in MVVM.
  /// The service overall supports viewing all the items and then viewing the details of a single item (SelectedItem)
  /// </summary>
  public interface IListService<T> : IRefreshable
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Guid ParentId { get; set; }
    T? SelectedItem { get; set; }
    IList<T> Items { get; set; }

    //events
    event EventHandler<T?>? SelectedItemChanged;


    //methods
    void Refresh();
    bool Add(T item);
    bool Update(T item);
    bool Delete(T item);
    bool Copy(T item);
    void Import(ImportSettings importSettings);
    void Export(ExportSettings exportSettings);
  }
}
