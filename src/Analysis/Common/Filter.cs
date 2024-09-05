using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Base class for filters in the analysis pipeline, the base class default implementation simply passes messages along the pipe.
  /// </summary>
  public class Filter : PipeOrFilter, IFilter
  {
    //constants


    //enums


    //types


    //attributes
    /// <summary>
    /// Cancellation token from the engine to control filter execution.
    /// </summary>
    protected CancellationToken m_cancellationToken;

    //properties
    public FilterMode Mode { get; }
    public IPipe Input { get; set; }
    public IPipe Output { get; set; }

    //constructors
    public Filter(ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(logger) {
      Mode = mode;
      m_cancellationToken = cancellationToken;
      //pipeline configuration should set the input and output
      Input = new NullPipe(logger, this);
      Output = Input;
    }

    //finalizers


    //interface implementations


    //methods
    public bool IsStart(IPipeline pipeline)
    {
      return pipeline.Composition.Count > 0 && pipeline.Composition[0] == this;
    }

    public bool Evaluate()
    {
      if (Input.Count == 0) return false;
      var message = Input.Dequeue();
      if (message != null) Output.Enqueue(message);
      return true;
    }

    public Task EvaluateAsync()
    {
      return Task.Run(() =>
      {
        while (!m_cancellationToken.IsCancellationRequested) Evaluate();
      });
    }
  }
}
