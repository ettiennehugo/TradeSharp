using TradeSharp.Analysis.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation for the computation filter to integrate indicators and strategies into the pipeline.
  /// </summary>
  public class ComputationFilter : Filter, IComputationFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public ComputationFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
