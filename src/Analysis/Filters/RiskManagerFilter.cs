using Microsoft.Extensions.Logging;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation for the risk manager filter to manage account risk against posted orders.
  /// </summary>
  public class RiskManagerFilter : Filter, IRiskManagerFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public RiskManagerFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
