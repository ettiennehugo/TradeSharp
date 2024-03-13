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
    Instrument? GetItem(Guid id);
    Instrument? GetItem(string ticker);
    IList<Instrument> GetItems(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
    IList<Instrument> GetItems(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count);  //paged loading of instruments
    string? TickerFromId(Guid id);           //returns the main ticker associated with the instrument
    IList<string>? TickersFromId(Guid id);   //returns all the tickers associated with the instrument
    Guid? IdFromTicker(string ticker);       //returns the id associated with the ticker
    IDictionary<Guid, string> GetInstrumentIdTicker();  //get the full map of instrument id to ticker
    IDictionary<Guid, IList<string>> GetInstrumentIdTickers();  //get the full map of instrument id to tickers mapping that includes alternate tickers
    IDictionary<string, Guid> GetTickerInstrumentId();  //get the full map of ticker to instrument id
  }
}
