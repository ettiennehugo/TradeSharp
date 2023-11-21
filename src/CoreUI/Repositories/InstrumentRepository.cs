using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on instrument running parallel to the UI thread.
  /// </summary>
  public class InstrumentRepository : IInstrumentRepository
  {
    //constants


    //enums


    //types


    //attributes
    protected IDataStoreService m_dataStore;

    //constructors
    public InstrumentRepository(IDataStoreService dataStore)
    {
      m_dataStore = dataStore;
    }

    //finalizers


    //interface implementations
    public Task<Instrument> AddAsync(Instrument item)
    {
      return Task.Run(() => { m_dataStore.CreateInstrument(item); return item; });
    }

    public Task<bool> DeleteAsync(Instrument item)
    {
      return Task.FromResult(m_dataStore.DeleteInstrument(item) != 0);
    }

    public Task<Instrument?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Instrument?>(m_dataStore.GetInstrument(id));
    }

    public Task<IEnumerable<Instrument>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<Instrument>>(m_dataStore.GetInstruments());
    }

    public Task<Instrument> UpdateAsync(Instrument item)
    {
      return Task.Run(() => { m_dataStore.UpdateInstrument(item); return item; });
    }

    //properties


    //methods


  }
}
