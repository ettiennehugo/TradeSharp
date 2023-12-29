using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Instrument data service interface, mainly encapsulating the key information used to populate the underlying repository.
  /// </summary>
  public interface IInstrumentDataService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    Instrument? Instrument { get; set; }
    Resolution Resolution { get; set; }

    //methods


  }
}
