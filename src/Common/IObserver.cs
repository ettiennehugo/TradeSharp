using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Common
{
  /// <summary>
  /// Interface to be implemented by observers to receive change notifications.
  /// </summary>
  public interface IObserver<T>
  {

    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    public int GetHashCode();
    public void OnChange(IEnumerable<T> changes);
  }
}
