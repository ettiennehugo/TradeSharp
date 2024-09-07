using Microsoft.Extensions.Logging;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis
{
  /// <summary>
  /// Generic time series analysis engine, uses a pipe and filter architecture to run elements in the pipe line.
  /// See POSA Volume 4, p. 200, core aspects we'll try to implement:
  /// - Each engine component should correspond to a discrete and distinguishable action/computation.
  /// - Some usage scenarions could require access to intermediate yet meaningful results.
  /// - The pipe used to buffer data should allow components to read, process and write data streams incrementally.
  /// - Long duration operations should not become a performance bottleneck to the engine.
  /// - Each filter can implement concurrency to allow parallel processing of data streams.
  /// - Pipes are a medium for data exchange between filters, they should be able to handle data streams of varying sizes.
  /// - Pipes decouples filters from each other, allowing for easy reconfiguration of the pipe line.
  /// The following is important to note about the specific implementation:
  ///   - Takes an engine composition object that defines the engine's components, the engine can then be
  ///     executed to produce outputs.
  ///   - Threading model:
  ///     - The engine will contain a number of defined pipelines each with it's own execution thread where synchronous filters
  ///       will be executed as fast as possible.
  ///     - The pipeline will start asynchronous filters in their own threads where the filter will determine the execution speed/frequency.
  /// - The each engine pipeline will typically start with some data source that injects data into the pipe line
  ///   and then the data will be processed by a series of computations, indicators, signals, and functions to
  ///   yield outputs.
  /// </summary>
  public class Engine : IEngine
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public Engine(ILogger logger, IEngineConfiguration configuration)
    {
      Logger = logger;
      Configuration = configuration;
      Status = RunStatus.None;
      Pipelines = new List<IPipeline>();
      CancellationTokenSource = new CancellationTokenSource();
    }

    //finalizers


    //interface implementations


    //properties
    public IEngineConfiguration Configuration { get; protected set; }
    public IList<IPipeline> Pipelines { get; protected set; }
    public CancellationTokenSource CancellationTokenSource { get; protected set; }
    public RunStatus Status { get; protected set; }
    public ILogger Logger { get; protected set; }

    //methods
    public IPipeline AddPipeline(string name)
    {
      var pipeline = new Pipeline(name, Logger, CancellationTokenSource.Token);
      Pipelines.Add(pipeline);
      return pipeline;
    }

    public bool Start()
    {
      lock (this)
      {
        //can only start the engine once
        if ((Status & RunStatus.Init) == RunStatus.Init) return false;
        Status |= RunStatus.Init;
        Configuration.Compose(this);
        Status |= RunStatus.Composed;
        //validate the set of defined pipelines
        foreach (var pipeline in Pipelines)
          if (!pipeline.Validate())
          {
            Status |= RunStatus.Error;
            return false;
          }

        Status |= RunStatus.Validated;
        //start the defined set of pipelines
        foreach (var pipeline in Pipelines) pipeline.RunAsync();

        Status |= RunStatus.Running;
        return true;
      }
    }

    public bool Stop()
    {
      if (CancellationTokenSource.Token.CanBeCanceled)
      {
        CancellationTokenSource.Cancel(); //this will stop the set of pipelines
        Status |= RunStatus.Stopped;
        return true;
      }
      return false;
    }
  }
}
