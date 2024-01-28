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
    int Update(IList<T> items);   //mass update of items
    int Delete(IList<T> items);   //mass delete of items
    int GetCount();   //gets the total number of items in the database
    int GetCount(DateTime from, DateTime to);   //gets the total number of items in the database within a given date/time range
    IList<T> GetItems(DateTime from, DateTime to);   //loads all the data within a given date/time range (can be slow with big windows!!!)
    IList<T> GetItems(int index, int count);   //loads a spesific number of items from the database from a specific index (used for paged loading of large amounts of data)
    IList<T> GetItems(DateTime from, DateTime to, int index, int count);   //loads a spesific number of items from the database from a specific index (used for paged loading of large amounts of data)
  }
}
