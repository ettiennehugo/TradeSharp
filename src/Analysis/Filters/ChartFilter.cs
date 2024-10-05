using TradeSharp.Analysis.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation to hook in charts into the pipeline to visualize the data in the pipeline.
  /// </summary>
  public class ChartFilter : Filter, IChartFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public ChartFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
