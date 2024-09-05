using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Pipeline implementation for the analysis engine.
  /// </summary>
  public class Pipeline : IPipeline
  {
    //constants


    //enums


    //types


    //attributes
    protected CancellationToken m_cancellationToken;

    //properties
    public ILogger Logger { get; }
    public IList<IPipeOrFilter> Composition => new List<IPipeOrFilter>();

    //constructors
    public Pipeline(ILogger logger, CancellationToken cancellationToken)
    {
      Logger = logger;
      m_cancellationToken = cancellationToken;
    }

    //finalizers


    //interface implementations


    //methods
    public void Add(IFilter filter)
    {
      //duplicates of filters are not allowed
      if (Composition.Contains(filter))
      {
        Logger.LogWarning($"Tried to insert filter of type {filter.GetType().ToString()} at index {Composition.Count - 1} that already exists in the pipeline.");
        return;
      }

      IFilter? previous = Composition.Count > 0 ? Composition[Composition.Count - 1] as IFilter : null;
  
      //NOTE: We hook in the pipes but they never form part of the actual pipeline.
      if (previous != null)
      {
        previous.Output = new Pipe(Logger);
        filter.Input = previous.Output;
      }

      Composition.Add(filter);
    }

    public bool Validate()
    {
      bool valid = true;

      for (int i = 0; i < Composition.Count; i++)
      {
        IPipeOrFilter component = Composition[i];
        if (i == 0 && component is IFilter startFilter)
        {
          //TBD: It might be useful to allow non-NullPipe inputs for testing.
          //if (startFilter.Input is not NullPipe)
          //{
          //  Logger.LogError("Start filter should have a null input pipe.");
          //  valid = false;
          //}

          if (Composition.Count > 1 && startFilter.Output is NullPipe)
          {
            Logger.LogError("Start filter has a null output pipe.");
            valid = false;
          }
        }
        else if (i == Composition.Count - 1 && component is IFilter endFilter)
        {
          if (Composition.Count > 1 && endFilter.Input is NullPipe)
          {
            Logger.LogError("End filter should not have a null input pipe.");
            valid = false;
          }

          //TBD: It might be useful to allow non-NullPipe outputs for testing.
          //if (endFilter.Output is NullPipe == false)
          //{
          //  Logger.LogError("End filter should have a null output pipe.");
          //  valid = false;
          //}
        }
        else
        {
          //other filters in the pipe must always have non-NullPipe inputs and outputs
          if (component is IFilter filter)
          {
            if (filter.Input is NullPipe)
            {
              Logger.LogError($"Filter at index {i} has a null input pipe.");
              valid = false;
            }

            if (filter.Output is NullPipe)
            {
              Logger.LogError($"Filter at index {i} has a null output pipe.");
              valid = false;
            }
          }
        }

        if (component is NullFilter)
        {
          Logger.LogError($"Filter at index {i} is a null filter.");
          valid = false;
        }

        if (!valid) break;
      }

      return valid;
    }

    public Task RunAsync() 
    {
      if (!Validate()) throw new InvalidOperationException("Pipeline is not valid.");
      return Task.Run(() =>
      {
        //start asynchronous filters
        foreach (var filter in Composition)
          if (filter is IFilter f && f.Mode == FilterMode.Asynchronous) f.EvaluateAsync();

        //run synchronous filters
        while (!m_cancellationToken.IsCancellationRequested)
          foreach (var component in Composition)
          {
            if (m_cancellationToken.IsCancellationRequested) break;
            if (component is IFilter filter && filter.Mode == FilterMode.Synchronous)
            {
              filter.Evaluate();
            }
          }
      });
    }
  }
}
