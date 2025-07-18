﻿using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Null filter used during pipeline composition.
  /// </summary>
  public class NullFilter : PipeOrFilter, IFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public string Name { get; }
    public ExecutionStatus Status { get; }
    public FilterMode Mode => FilterMode.Synchronous;
    public IPipe Input { get; set; }
    public IPipe Output { get; set; }

    //constructors
    public NullFilter(string name, ILogger logger) : base(logger)
    {
      Name = name;
      Status = ExecutionStatus.Init;
      Input = new NullPipe(logger, this);
      Output = Input;
    }

    //finalizers


    //interface implementations


    //methods
    public bool IsStart(IPipeline pipeline)
    {
      throw new NotImplementedException();    //should never be called once the pipeline is running
    }

    public bool Evaluate()
    {
      throw new NotImplementedException();    //should never be called once the pipeline is running
    }

    public Task EvaluateAsync()
    {
      throw new NotImplementedException();
    }
  }
}
