using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Analysis;

namespace TradeSharp.Analysis
{
  public class SingleMetricResult : Function
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public SingleMetricResult(Metric metric)
    {
      Metric = metric;
    }

    //finalizers


    //interface implementations


    //properties
    public Metric Metric { get; }
    public decimal Value => throw new NotImplementedException();

    //methods


  }
}
