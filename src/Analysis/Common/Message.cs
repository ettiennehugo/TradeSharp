namespace TradeSharp.Analysis.Common
{
  /// <summary>
  /// Message passed along a pipe in the engine.
  /// </summary>
  public class Message : IMessage
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public object Data { get; set; }

    //constructors
    public Message(object data)
    {
      Data = data;
    }

    //finalizers


    //interface implementations


    //methods

  }
}
