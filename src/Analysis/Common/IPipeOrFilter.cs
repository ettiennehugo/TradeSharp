using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Base interaface for a pipe or filter in the analysis engine.
  /// </summary>
  public interface IPipeOrFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Logger used by the pipe/filter to log the the engine/pipeline log.
    /// </summary>
    ILogger Logger { get; }

    //methods


  }
}
