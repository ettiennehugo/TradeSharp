using Microsoft.Extensions.Logging;
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
  [Flags]
  public enum RunStatus
  {
    None = 0,           // 0: no state
    Init = 1 << 0,      // 1: pre-run initialization state
    Composed = 1 << 1,  // 2: engine has been composed and is going to be validated
    Validated = 1 << 2, // 4: engine was validated and ready to run
    Running = 1 << 3,   // 8: engine is running
    Stopped = 1 << 4,   // 16: engine was stopped (completed)
    Error = 1 << 5      // 32: engine has encountered an error
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
    /// Logger used to log entries to the engine log.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Configuration used to setup the analysis engine.
    /// </summary>
    IEngineConfiguration Configuration { get; }

    /// <summary>
    /// Set of data pipelines defined in the engine.
    /// </summary>
    IList<IPipeline> Pipelines { get; }

    /// <summary>
    /// Cancellation token source used to stop the engine execution.
    /// </summary>
    CancellationTokenSource CancellationTokenSource { get; }

    /// <summary>
    /// State of the engine.
    /// </summary>
    RunStatus Status { get; }

    //methods
    /// <summary>
    /// Adds a pipeline to the engine for processing data.
    /// </summary>
    IPipeline AddPipeline(string name);

    /// <summary>
    /// Start/stop the engine execution. Engine can enter the stopped state
    /// if it does not require perpetual execution.
    /// </summary>
    bool Start();
    bool Stop();
  }
}
