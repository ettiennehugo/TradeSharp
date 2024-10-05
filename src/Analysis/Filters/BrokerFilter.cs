using Microsoft.Extensions.Logging;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation for the broker filter, this will be a wrapper facade for any broker plugins to receive orders and send.
  /// </summary>
  public class BrokerFilter : Filter, IBrokerFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public BrokerFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
