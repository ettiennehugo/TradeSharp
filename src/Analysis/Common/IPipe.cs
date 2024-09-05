namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Interface for the pipe implementation for the analysis engine.
  /// </summary>
  public interface IPipe : IPipeOrFilter
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Number of messages in the pipe.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Source and end filters of the pipe.
    /// </summary>
    IFilter Source { get; }
    IFilter End { get; }

    //methods
    /// <summary>
    /// Queueing methods for the pipe, methods return null if there is nothing on the queue.
    /// </summary>
    IMessage? Peek();
    void Enqueue(IMessage message);
    IMessage? Dequeue();
  }
}
