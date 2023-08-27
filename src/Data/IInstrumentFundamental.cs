using System.Security.Cryptography;

namespace TradeSharp.Data
{
  /// <summary>
  /// Fundamental factor associated with a specific instrument.
  /// </summary>
  public interface IInstrumentFundamental : IFundamentalValues
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    /// <summary>
    /// Returns the instrument id for this association.
    /// </summary>
    Guid InstrumentId { get; }

    /// <summary>
    /// Instrument to which the fundamental factor is bound.
    /// </summary>
    IInstrument Instrument { get; internal set; }

    //methods


  }
}