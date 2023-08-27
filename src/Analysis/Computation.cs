using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.Analysis;

namespace TradeSharp.Analysis
{
  public class Computation
  {
    //constants


    //enums


    //types


    //attributes
    protected List<DataFeed> m_dataFeeds;

    //constructors
    public Computation()
    {
      m_dataFeeds = new List<DataFeed>();
    }

    //finalizers


    //interface implementations
    public void OnCalculate()
    {
      throw new NotImplementedException();
    }

    public void OnCreate()
    {
      throw new NotImplementedException();
    }

    public void OnDestroy()
    {
      throw new NotImplementedException();
    }

    public void OnStart()
    {
      throw new NotImplementedException();
    }

    public void OnStop()
    {
      throw new NotImplementedException();
    }

    //properties
    IReadOnlyList<IDataFeed> DataFeeds => m_dataFeeds;

    //methods

  }
}
