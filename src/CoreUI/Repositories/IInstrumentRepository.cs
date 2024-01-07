using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Concreate data store interface to query and edit instrument data in an asychronous fashion so as to not tie up the UI thread.
  /// </summary>
  public interface IInstrumentRepository: IReadOnlyRepository<Instrument, Guid>, IEditableRepository<Instrument, Guid> 
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
    IList<Instrument> GetOffset(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
    IList<Instrument> GetOffset(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
  }
}
