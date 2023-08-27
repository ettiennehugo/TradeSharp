using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Analysis;

namespace TradeSharp.Analysis
{

  public class SeriesMetricResult : SingleMetricResult
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public SeriesMetricResult(Metric metric) : base(metric)
    {
      Values = new List<(DateTime, Decimal)>();
    }

    //finalizers


    //interface implementations


    //properties
    public IReadOnlyList<(DateTime, Decimal)> Values { get; protected set; }

    //methods


  }
}
