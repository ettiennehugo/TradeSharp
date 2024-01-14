namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Source of incremental loading of items.
  /// </summary>
  public interface IIncrementalSource<T>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Returns true if there are more items to be loaded, false otherwise.
    /// </summary>
    bool HasMoreItems { get; }

    //methods    
    /// <summary>
    /// Request to load count more items into the given list collection and returns the number of items actually loaded.
    /// </summary>
    Task<IList<T>> LoadMoreItemsAsync(int count);
  }
}
