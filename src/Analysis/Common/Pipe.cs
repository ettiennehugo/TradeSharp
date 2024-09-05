using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Cached thread safe pipe, the pipes operations run within the thread of the calling code it does not have
  /// it's own thread, this would typically be either the engine, pipeline or filter thread.
  /// </summary>
  public class Pipe : PipeOrFilter, IPipe
  {
    //constants


    //enums


    //types


    //attributes
    protected ConcurrentQueue<IMessage> m_messages => new ConcurrentQueue<IMessage>();

    //properties
    public int Count => m_messages.Count;
    public IFilter Source { get; set; }
    public IFilter End { get; set; }

    //constructors
    public Pipe(ILogger logger) : base(logger) { }

    //finalizers


    //interface implementations


    //methods
    public IMessage? Peek()
    {
      IMessage? message = null;
      m_messages.TryPeek(out message);
      return message;
    }

    public void Enqueue(IMessage message)
    {
      m_messages.Enqueue(message);
    }

    public IMessage? Dequeue()
    {
      IMessage? message = null;
      m_messages.TryDequeue(out message);
      return message;
    }
  }
}
