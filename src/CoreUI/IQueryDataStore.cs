using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI
{
  /// <summary>
  /// Interface for classes that facilitate querying the data store for specific object types using a specific key type. 
  /// </summary>
  public interface IQueryDataStore<T, in TKey>
    where T : class
  {

    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    Task<T?> GetItemAsync(TKey id);
    Task<IEnumerable<T>> GetItemsAsync();
  }
}
