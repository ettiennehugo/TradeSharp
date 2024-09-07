namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Execution mode of the filter.
  /// </summary>
  public enum FilterMode
  {
    Synchronous,
    Asynchronous
  }  

  /// <summary>
  /// Interface for the filter implementation for the analysis engine.
  /// </summary>
  public interface IFilter : IPipeOrFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Name of the filter, mainly used for debugging to see where in the pipe certain components are.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Status of the filter, used to signal the engine to either continue processing or terminate.
    /// </summary>
    ExecutionStatus Status { get; }

    /// <summary>
    /// Execution mode of the filter, synchronous or asynchronous. Synchronous filters run on the pipeline execution thread while
    /// asynchronous filters run on a separate thread using thread safe pipes to post messages along.
    /// </summary>
    FilterMode Mode { get; }

    /// <summary>
    /// Input/output pipes for the filter.
    /// </summary>
    IPipe Input { get; set; }
    IPipe Output { get; set; }

    //methods
    /// <summary>
    /// Is this the first filter in the given pipeline?
    /// </summary>
    bool IsStart(IPipeline pipeline);

    /// <summary>
    /// Evaluate the state of the filter to produce output. Returns whether the filter
    /// produced output to be passed to the next filter in the pipeline.
    /// </summary>
    bool Evaluate();
    Task EvaluateAsync();
  }
}
