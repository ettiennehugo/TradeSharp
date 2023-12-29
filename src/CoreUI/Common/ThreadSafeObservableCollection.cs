using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Defines a thread-safe version of the ObservableCollection class that exposes a lock that can be used to synchronize access to the collection
  /// with other components that access it.
  /// See https://learn.microsoft.com/en-us/dotnet/api/system.windows.data.bindingoperations.enablecollectionsynchronization?view=windowsdesktop-8.0 on
  /// how to use this collection with data binding.
  /// </summary>
  public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
  {
    //constants


    //enums


    //types
    internal static class EventArgsCache
    {
      internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
      internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
      internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }

    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties
    public object Lock { get; } = new object();
    [field: NonSerialized]
    public override event NotifyCollectionChangedEventHandler? CollectionChanged;

    //methods
    protected override void ClearItems()
    {
      lock(Lock) { base.ClearItems(); }
      OnCountPropertyChanged();
      OnIndexerPropertyChanged();
      OnCollectionReset();
    }

    protected override void RemoveItem(int index)
    {
      T removedItem = this[index];
      lock (Lock) { base.RemoveItem(index); }
      OnCountPropertyChanged();
      OnIndexerPropertyChanged();
      OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
    }

    protected override void InsertItem(int index, T item)
    {
      lock (Lock) { base.InsertItem(index, item); }
      OnCountPropertyChanged();
      OnIndexerPropertyChanged();
      OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    protected override void SetItem(int index, T item)
    {
      T originalItem = this[index];
      lock (Lock) { base.SetItem(index, item); }
      OnIndexerPropertyChanged();
      OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item, index);
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
      T removedItem = this[oldIndex];
      lock(Lock)
      {
        base.RemoveItem(oldIndex);
        base.InsertItem(newIndex, removedItem);
      }
      OnIndexerPropertyChanged();
      OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
    }

    /// <summary>
    /// Helper to raise CollectionChanged event to any listeners
    /// </summary>
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index)
    {
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
    }

    /// <summary>
    /// Helper to raise CollectionChanged event to any listeners
    /// </summary>
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? item, int index, int oldIndex)
    {
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
    }

    /// <summary>
    /// Helper to raise CollectionChanged event to any listeners
    /// </summary>
    private void OnCollectionChanged(NotifyCollectionChangedAction action, object? oldItem, object? newItem, int index)
    {
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
      NotifyCollectionChangedEventHandler? handler = CollectionChanged;
      if (handler != null) handler(this, e);
    }

    private void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);
    private void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
    private void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
  }
}
