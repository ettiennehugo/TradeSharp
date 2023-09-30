using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Common
{
/// <summary>
/// Interface to allow editibility of the data store for a specific type using a specific key type. 
/// </summary>
  public interface IEditableRepository<T, in TKey>
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
