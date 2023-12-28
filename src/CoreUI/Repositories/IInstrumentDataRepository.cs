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
    bool HasMoreItems { get; }

    //methods
    Task<long> UpdateAsync(IList<T> items);   //more performant mass update of items
    Task<IEnumerable<T>> GetItemsAsync(DateTime start, DateTime end);   //loads all the data within a given date/time range (can be slow with big windows!!!)
    Task<IEnumerable<T>> GetItemsAsync(int index, int count);   //loads a spesific number of items from the database from a specific index (used for paged loading of large amounts of data)
  }
}
