using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for the instrument service.
  /// </summary>
  public interface IInstrumentService : IListService<Instrument> 
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    int GetCount();
    int GetCount(InstrumentType instrumentType);
    int GetCount(string tickerFilter, string nameFilter, string descriptionFilter); //no filter if filters are blank, filters support wildcards * and ?
    int GetCount(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter); //no filter if filters are blank, filters support wildcards * and ?
    IList<Instrument> GetItems(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
    IList<Instrument> GetItems(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
  }
}
