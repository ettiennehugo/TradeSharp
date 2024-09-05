using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Null pipe used as a placeholder during pipeline composition, by default feedsback to a specific filter.
  /// </summary>
  public class NullPipe : PipeOrFilter, IPipe
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public int Count => 0;
    public IFilter Source { get; set; }
    public IFilter End { get; set; }

    //constructors
    public NullPipe(ILogger logger, IFilter filter) : base(logger)
    {
      Source = filter;
      End = filter;
    }

    public IMessage Dequeue()
    {
      throw new NotImplementedException();    //should never be called once the pipeline is running
    }

    public void Enqueue(IMessage message)
    {
      throw new NotImplementedException();    //should never be called once the pipeline is running
    }

    public IMessage Peek()
    {
      throw new NotImplementedException();    //should never be called once the pipeline is running
    }

    //finalizers


    //interface implementations


    //methods



  }
}
