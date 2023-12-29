using System.Collections.ObjectModel;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Interface to be implemented by services that allow manipulation of items in a tree fashion. 
  /// </summary>
  public interface ITreeItemsService<TKey, TItem>
    where TItem : class
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    TKey RootNodeId { get; }
    Guid ParentId { get; set; }
    ITreeNodeType<TKey, TItem>? SelectedNode { get; set; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> SelectedNodes { get; set; }
    ObservableCollection<ITreeNodeType<TKey, TItem>> Nodes { get; } //nodes collection under the RootNodeId
    ObservableCollection<TItem> Items { get; }    //flat list of all items

    //events
    event EventHandler<ITreeNodeType<TKey, TItem>?>? SelectedNodeChanged;

    //methods
    void Refresh();
    void Refresh(TKey parentKey);
    void Refresh(ITreeNodeType<TKey, TItem> parentNode);
    bool Add(ITreeNodeType<TKey, TItem> item);
    bool Update(ITreeNodeType<TKey, TItem> item);
    bool Delete(ITreeNodeType<TKey, TItem> item);
    bool Copy(ITreeNodeType<TKey, TItem> item);
    ImportResult Import(ImportSettings importSettings);
    ExportResult Export(string filename);
  }
}
