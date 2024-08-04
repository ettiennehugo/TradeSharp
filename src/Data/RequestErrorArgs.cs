using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Event arguments for request data download errors.
  /// </summary>
  public class DataDownloadErrorArgs : Common.RequestErrorArgs
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public Instrument Instrument { get; }
    public Resolution Resolution { get; }

    //constructors
    public DataDownloadErrorArgs(Instrument instrument, Resolution resolution, string message, Exception? exception = null): base(message, exception)
    {
      Instrument = instrument;
      Resolution = resolution;
    }

    //finalizers


    //interface implementations


    //methods



  }
}
