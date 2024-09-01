using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Analysis.Common;

namespace TradeSharp.Analysis
{
    /// <summary>
    /// Engine run status.
    /// </summary>
    public enum RunStatus
  {
    Init,     //pre-run initialization state
    Composed, //engine has been composed and ready to run
    Running,  //engine is running
    Stopped   //engine was stopped (completed)
  }

  /// <summary>
  /// Interface for the analysis engine service. 
  /// </summary>
  public interface IEngine
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Configuration used to setup the analysis engine.
    /// </summary>
    IEngineConfiguration Configuration { get; }

    /// <summary>
    /// Composition of the engine once it has been composed.
    /// </summary>
    IList<IPipeOrFilter> Composition { get; }

    /// <summary>
    /// Token used to stop the engine execution.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// State of the engine.
    /// </summary>
    RunStatus Status { get; }

    //methods
    /// <summary>
    /// Sets the start filter for the engine. This would typically be a filter that
    /// produces data.
    /// </summary>
    void SetStart(IFilter filter);

    /// <summary>
    /// Add a filter to the engine.
    /// </summary>
    void Add(IPipe pipe, IFilter filter);

    /// <summary>
    /// Run the engine asynchronously.
    /// </summary>
    void RunAsync();
  }
}
