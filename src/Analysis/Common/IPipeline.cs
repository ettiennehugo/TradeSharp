using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Pipeline in the engine used to represent a flow of data in the engine a specific composition. Each pipeline
  /// has it's own thread in which synchronous filters would execute and runs independently of other pipelines. Asynchronous
  /// filters are started by the pipeline and run in their own threads.
  /// </summary>
  public interface IPipeline
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Name of the pipeline, will be assigned to the thread running the pipeline.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution status of the pipeline, pipeline can execute perpertually until the engine signals termination through the cancellation token or
    /// the pipeline can run until all filters reach a completed status meaning that they will produce no more messages.
    /// </summary>
    ExecutionStatus Status { get; }

    /// <summary>
    /// Logger used by the pipe/filter to log the the engine/pipeline log.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Set of pipes and filtets that make up the pipeline.
    /// </summary>
    IList<IPipeOrFilter> Composition { get; }

    //methods
    /// <summary>
    /// Add a filter to the pipeline, the filter must already be connected using a pipe.
    /// </summary>
    void Add(IFilter filter);

    /// <summary>
    /// Checks the that pipeline composition is correct.
    /// </summary>
    bool Validate();

    /// <summary>
    /// Starts the pipeline running in it's own thread, will terminate if the pipeline composition is not correct.
    /// </summary>
    Task RunAsync();
  }
}
