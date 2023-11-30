using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Generic interface for instrument data repositories, in general controls the key settings to retrieve instrument data from the data base.
  /// </summary>
  public interface IInstrumentDataRepository
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    Instrument? Instrument { get; set; }
    DateTime Start { get; set; }
    DateTime End { get; set; }
    Resolution Resolution { get; set; }
    PriceDataType PriceDataType {  get; set; }

    //methods



  }
}
