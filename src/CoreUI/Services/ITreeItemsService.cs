using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    event EventHandler<TItem>? SelectedNodeChanged;

    //methods
    Task RefreshAsync();
    Task RefreshAsync(TKey parentKey);
    Task RefreshAsync(ITreeNodeType<TKey, TItem> parentNode);
    Task<ITreeNodeType<TKey, TItem>> AddAsync(ITreeNodeType<TKey, TItem> item);
    Task<ITreeNodeType<TKey, TItem>> UpdateAsync(ITreeNodeType<TKey, TItem> item);
    Task<bool> DeleteAsync(ITreeNodeType<TKey, TItem> item);
    Task<ITreeNodeType<TKey, TItem>> CopyAsync(ITreeNodeType<TKey, TItem> item);
    Task<ImportReplaceResult> ImportAsync(string filename, ImportReplaceBehavior importReplaceBehavior);
    Task<int> ExportAsync(string filename);
  }
}
