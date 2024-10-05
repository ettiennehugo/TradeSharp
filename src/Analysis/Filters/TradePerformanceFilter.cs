using Microsoft.Extensions.Logging;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Trade performance filter used to monitor trading positions and the performance of trades in the pipeline.
  /// </summary>
  public class TradePerformanceFilter : Filter, ITradePerformanceFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public TradePerformanceFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
