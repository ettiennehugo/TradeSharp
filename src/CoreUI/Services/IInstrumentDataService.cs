using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Instrument data service interface, mainly encapsulating the key information used to populate the underlying repository.
  /// </summary>
  public interface IInstrumentDataService<TItem>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    Instrument? Instrument { get; set; }
    Resolution Resolution { get; set; }
    bool MassOperation { get; set; }   //sets when the service is used for mass operations on the data

    //methods
    int Delete(IList<TItem> items);   //mass delete of items
    int GetCount();
    int GetCount(DateTime from, DateTime to);
    IList<TItem> GetItems(DateTime from, DateTime to);
    IList<TItem> GetItems(int index, int count);
    IList<TItem> GetItems(DateTime from, DateTime to, int index, int count);
  }
}
