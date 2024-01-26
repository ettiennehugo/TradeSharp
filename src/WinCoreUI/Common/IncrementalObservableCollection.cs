using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Common;
using System;
using System.Linq;

namespace TradeSharp.WinCoreUI.Common
{
  /// <summary>
  /// Observable collection that supports incremental loading of items.
  /// </summary>
  public class IncrementalObservableCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
  {
    //constants
    public const int DefaultPageSize = 20;

    //enums


    //types


    //attributes
    protected IIncrementalSource<T> m_incrementalSource;
    protected int m_defaultPageSize;

    //constructors
    public IncrementalObservableCollection(IIncrementalSource<T> incrementalSource, int defaultPageSize = DefaultPageSize) : base()
    {
      m_incrementalSource = incrementalSource;
      m_defaultPageSize = defaultPageSize;
      IsLoading = false;
    }

    public IncrementalObservableCollection(IIncrementalSource<T> incrementalSource, IEnumerable<T> collection, int defaultPageSize = DefaultPageSize) : base(collection)
    {
      m_incrementalSource = incrementalSource;
      m_defaultPageSize = defaultPageSize;
      IsLoading = false;
    }

    public IncrementalObservableCollection(IIncrementalSource<T> incrementalSource, IList<T> collection, int defaultPageSize = DefaultPageSize) : base(collection)
    {
      m_incrementalSource = incrementalSource;
      m_defaultPageSize = defaultPageSize;
      IsLoading = false;
    }

    //finalizers


    //interface implementations
    public virtual async void RefreshAsync()
    {
      ClearItems();
      await InternalLoadMoreItemsAsync(DefaultPageSize);
    }

    //properties
    public bool HasMoreItems => m_incrementalSource.HasMoreItems;
    public bool IsLoading { get; private set; }

    //methods
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => InternalLoadMoreItemsAsync(count).AsAsyncOperation();

    private async Task<LoadMoreItemsResult> InternalLoadMoreItemsAsync(uint count)
    {
      IList<T> items = await m_incrementalSource.LoadMoreItemsAsync((int)count);
      int baseIndex = Count;
      for (int index = 0; index < items.Count; index++) Add(items[index]);
      return new LoadMoreItemsResult((uint)items.Count());
    }
  }
}
