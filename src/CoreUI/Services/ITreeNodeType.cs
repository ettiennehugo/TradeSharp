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
    ITreeNodeType<TKey, TItem>? Parent { get; set; }    //null when parent is the root node
    TKey Id { get; set; }
    TItem Item { get; set; }
    bool Expanded { get; set; }
    bool InstrumentsVisible { get; set; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> Children { get; }

    //methods
    void Refresh();
  }
}
