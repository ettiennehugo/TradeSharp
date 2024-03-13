using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on instrument running parallel to the UI thread. Incremental loading is supported for both paging and offset loading - this is due to the fact the different UI technologies support different
  /// API's to incrementally load data.
  /// </summary>
  public class InstrumentRepository : IInstrumentRepository
  {
    //constants


    //enums


    //types


    //attributes
    protected IDatabase m_database;
    protected SortedList<string, Guid> m_tickerToId;
    protected SortedList<Guid, string> m_idToTicker;
    protected SortedList<Guid, IList<string>> m_idToTickers;

    //constructors
    public InstrumentRepository(IDatabase database)
    {
      m_database = database;
    }

    //finalizers


    //interface implementations
    public bool Add(Instrument item)
    {
      m_database.CreateInstrument(item);
      return true;
    }

    public bool Delete(Instrument item)
    {
      return m_database.DeleteInstrument(item) != 0;
    }

    public Instrument? GetItem(Guid id)
    {
      return m_database.GetInstrument(id);
    }

    public Instrument? GetItem(string ticker)
    {
      return m_database.GetInstrument(ticker);
    }

    public IList<Instrument> GetItems()
    {
      return m_database.GetInstruments();
    }

    public bool Update(Instrument item)
    {
      m_database.UpdateInstrument(item);
      return true;
    }

    public int GetCount()
    {
      return m_database.GetInstrumentCount();
    }

    public int GetCount(InstrumentType instrumentType)
    {
      return m_database.GetInstrumentCount(instrumentType);
    }

    public int GetCount(string tickerFilter, string nameFilter, string descriptionFilter)
    {
      return m_database.GetInstrumentCount(tickerFilter, nameFilter, descriptionFilter);
    }

    public int GetCount(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter)
    {
      return m_database.GetInstrumentCount(instrumentType, tickerFilter, nameFilter, descriptionFilter);
    }

    public IList<Instrument> GetItems(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      return m_database.GetInstrumentsOffset(tickerFilter, nameFilter, descriptionFilter, offset, count);
    }

    public IList<Instrument> GetItems(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      return m_database.GetInstrumentsOffset(instrumentType, tickerFilter, nameFilter, descriptionFilter, offset, count);
    }

    public IList<Instrument> GetPage(string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize)
    {
      return m_database.GetInstrumentsPage(tickerFilter, nameFilter, descriptionFilter, pageIndex, pageSize);
    }

    public IList<Instrument> GetPage(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize)
    {
      return m_database.GetInstrumentsPage(instrumentType, tickerFilter, nameFilter, descriptionFilter, pageIndex, pageSize);
    }

    public string? TickerFromId(Guid id)
    {
      m_idToTicker.TryGetValue(id, out string? ticker);
      return ticker;
    }

    public IList<string>? TickersFromId(Guid id)
    {
      return m_database.TickersFromId(id);
    }

    public Guid? IdFromTicker(string ticker)
    {
      return m_database.IdFromTicker(ticker);
    }

    public IDictionary<Guid, string> GetInstrumentIdTicker()
    {
      return m_database.GetInstrumentIdTicker();
    }

    public IDictionary<Guid, IList<string>> GetInstrumentIdTickers()
    {
      return m_database.GetInstrumentIdTickers();
    }

    public IDictionary<string, Guid> GetTickerInstrumentId()
    {
      return m_database.GetTickerInstrumentId();
    }

    //properties


    //methods


  }
}
