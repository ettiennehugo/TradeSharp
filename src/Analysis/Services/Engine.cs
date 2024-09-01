using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// - Takes an engine composition object that defines the engine's components, the engine can then be
    /// executed to produce outputs.
    /// - The engine pipe line will typically start with some data source that injects data into the pipe line
    /// and then the data will be processed by a series of computations, indicators, signals, and functions to
    /// yield outputs.
    /// </summary>
    public class Engine: IEngine
  {
    //constants


    //enums


    //types


    //attributes
    protected Task? m_task;

    //constructors
    public Engine(IEngineConfiguration configuration)
    {
      Configuration = configuration;
      Status = RunStatus.Init;
    }

    //finalizers


    //interface implementations

    //properties
    public IEngineConfiguration Configuration { get; protected set; }
    public IList<IPipeOrFilter> Composition { get; protected set; }
    public CancellationToken CancellationToken { get; protected set; }
    public RunStatus Status { get; protected set; }
    public ILogger Logger { get; protected set; }

    //methods
    public void SetStart(IFilter filter)
    {
      throw new NotImplementedException();
    }

    public void Add(IPipe pipe, IFilter filter)
    {
      throw new NotImplementedException();
    }

    public void RunAsync()
    {
      Configuration.Compose(this);
      Status = RunStatus.Composed;
      m_task = Task.Run(() =>
      {
        Status = RunStatus.Running;
        while (Status == RunStatus.Running)
        {
          //run the engine

        }
        Status = RunStatus.Stopped;
      });

      

    }

  }

}
