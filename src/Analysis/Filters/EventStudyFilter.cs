using Microsoft.Extensions.Logging;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Event study filter implementation to add event studies to a pipeline.
  /// </summary>
  public class EventStudyFilter : Filter, IEventStudyFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public EventStudyFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
