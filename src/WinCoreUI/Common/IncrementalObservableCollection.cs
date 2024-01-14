using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using TradeSharp.CoreUI.Common;
using System;
using System.Linq;
using System.Threading;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Observable collection that supports incremental loading of items.
  /// </summary>
  public class IncrementalObservableCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
  {
    //constants


    //enums


    //types


    //attributes
    protected IIncrementalSource<T> m_incrementalSource;

    //constructors
    public IncrementalObservableCollection(IIncrementalSource<T> incrementalSource): base()
    {
      m_incrementalSource = incrementalSource;
    }

    public IncrementalObservableCollection(IIncrementalSource<T> incrementalSource, IEnumerable<T> collection): base(collection)
    {
      m_incrementalSource = incrementalSource;
    }

    public IncrementalObservableCollection(IIncrementalSource<T> incrementalSource, IList<T> collection) : base(collection)
    {
      m_incrementalSource = incrementalSource;
    }

    //finalizers


    //interface implementations


    //properties
    public bool HasMoreItems => m_incrementalSource.HasMoreItems;

    //methods
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => InternalLoadMoreItemsAsync(count).AsAsyncOperation();

    private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(uint count)
    {
      IList<T> items = await m_incrementalSource.LoadMoreItemsAsync((int)count);
      int baseIndex = Count;
      for (int index = 0; index < items.Count; index++) Add(items[index]);
      for (int index = 0; index < items.Count; index++) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this[baseIndex + index], index));
      return new LoadMoreItemsResult((uint)items.Count());
    }
  }
}
