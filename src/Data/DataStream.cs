using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{
  /// <summary>
  /// Implements the IDataStream interface to allow reverse order access to data in a list
  /// in a controlled manner by the IDataFeed.CurrentBar property.
  /// </summary>
  public class DataStream<T> : IDataStream<T>
  {
    //constants


    //enums


    //types


    //attributes
    protected IList<T>? m_data;
    protected int m_currentBar;

    //constructors
    public DataStream()
    {
      m_data = null;
      m_currentBar = 0;
    }

    //finalizers
    public void Dispose()
    {
      m_data = null;
    }

    //interface implementations


    //properties
    public int CurrentBar
    {
      get => m_currentBar; 
      set 
      {
        //the data stream do not check the current bar is within the bounds of the data since the
        //DataFeed implementation would check it before setting the value
        m_currentBar = value;
      } 
    }

    public IList<T> Data
    {
      get => m_data!;
      set
      {
        //check that current bar is winthin bounds of data buffer if it contains data
        if (value.Count != 0 && CurrentBar > value.Count - 1) 
          throw new ArgumentOutOfRangeException("CurrentBar must be less than the number of bars in the data.");

        //reset current bar to zero if data buffer is empty
        if (value.Count == 0) CurrentBar = 0;

        m_data = value;
      }
    }

    public T this[int index]
    {
      get
      {
        if (index < 0 || index > m_currentBar)
          throw new ArgumentOutOfRangeException("index", "index must be between 0 and " + (m_currentBar - 1).ToString() + ".");
        return m_data![m_currentBar - index];
      }
    } 

    //methods



  }
}
