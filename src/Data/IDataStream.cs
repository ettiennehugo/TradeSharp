using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Data stream used with the data feed. The implementation would access the underlying and
  /// clip the index passed to operator[] between 0 and a given current bar to avoid any lookahead
  /// of the data. The data feed will reverse the order of the data according to date/time to
  /// make implementing indicators, signals, backtesters, portfolio's and charts easier.
  /// </summary>
  public interface IDataStream<T> : IDisposable
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    T this[int index] { get; }

    //methods


  }
}
