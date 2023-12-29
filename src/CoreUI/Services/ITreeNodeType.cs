using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface for tree nodes used by the tree items service and view model.
  /// </summary>
  public interface ITreeNodeType<TKey, TItem>
    where TItem : class
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    TKey ParentId { get; set; }
    TKey Id { get; set; }
    TItem Item { get; set; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> Children { get; }

    //methods
    void Refresh();
  }
}
