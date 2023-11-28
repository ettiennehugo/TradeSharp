using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    string Ticker { get; set; }
    DateTime Start { get; set; }
    DateTime End { get; set; }
    Resolution Resolution { get; set; }
    PriceDataType PriceDataType { get; set; }

    //methods


  }
}
