using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// List service that supports incremental loading of items. A generic filter is declared that implementations can use to filter the items returned by declaring names for filter fields and
  /// allowing the filter to be changed. Two modes of incremental loading is supported, Page and Offset - use only one or the other but do not mix them.
  /// </summary>
  public interface IIncrementalListService<TItem> : IListService<TItem>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Return the number of items in the database that would match the current filter - if filter is empty it will return all the items available.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Starting offset of the next offset to return, in general when the filter is changed this should be reset to zero.
    /// </summary>
    int OffsetIndex { get; set; }

    /// <summary>
    /// Number of items to return in the NextPage/PeekPage retrieval.
    /// </summary>
    int OffsetCount { get; set; }

    /// <summary>
    /// Returns true if the PageIndex/Offset is less than the Count. 
    /// </summary>
    bool HasMoreItems { get; }
    
    /// <summary>
    /// Name/value pairs for the filter fields that can be used to filter the items returned by the service.
    /// </summary>
    IDictionary<string, object> Filter { get; set; }

    //methods
    /// <summary>
    /// Return the number of items using the Offset, Count and Filter properties to retrieve the items and moves the OffsetIndex forward by the OffsetCount.
    /// </summary>
    IList<TItem> Next();

    /// <summary>
    /// Return the number of items using the Offset, Count and Filter properties to retrieve the items but keeps the Offset the same.
    /// </summary>
    IList<TItem> Peek();
  }
}
