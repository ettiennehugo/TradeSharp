using Microsoft.Extensions.Logging;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis.Filters
{
  /// <summary>
  /// Implementation for the tooling filter that would allow adding analysis tools to a pipeline.
  /// </summary>
  public class ToolingFilter : Filter, IToolingFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //constructors
    public ToolingFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
    }

    //finalizers


    //interface implementations


    //methods


  }
}
