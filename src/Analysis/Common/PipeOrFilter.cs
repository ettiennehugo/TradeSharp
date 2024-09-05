using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Base class of the pipe or filter in the analysis engine.
  /// </summary>
  public class PipeOrFilter : IPipeOrFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public ILogger Logger { get; protected set; }

    //constructors
    public PipeOrFilter(ILogger logger)
    {
      Logger = logger;
    }

    //finalizers


    //interface implementations


    //methods



  }
}
