using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Generic interface for instrument data repositories, in general controls the key settings to retrieve instrument data from the data base.
  /// </summary>
  public interface IInstrumentDataRepository<T>
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
    Task<long> UpdateAsync(IList<T> items);   //more performant mass update of items
    Task<IEnumerable<T>> GetItemsAsync(DateTime start, DateTime end); 
  }
}
