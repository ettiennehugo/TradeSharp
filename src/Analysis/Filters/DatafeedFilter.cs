using TradeSharp.Analysis.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation for the datafeed filter that would push data into the pipeline for analysis.
  /// </summary>
  public class DatafeedFilter : Filter, IDataFeedFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public DatafeedFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
