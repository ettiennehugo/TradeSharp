using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI
{
/// <summary>
/// Interface to update the data store for a specific type using a specific key type. 
/// </summary>
  public interface IUpdateDataStore<T, in TKey>
    where T : class
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    Task<T> AddAsync(T item);
    Task<T> UpdateAsync(T item);
    Task<bool> DeleteAsync(TKey id);
  }
}
