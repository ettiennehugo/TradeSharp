using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Common
{
  /// <summary>
  /// Observable object for a specific change type.
  /// </summary>
  public interface IObservable<T>
  {

    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer); 
  }
}
