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
    protected IDatabase m_database;

    //constructors
    public InstrumentRepository(IDatabase database)
    {
      m_database = database;
    }

    //finalizers


    //interface implementations
    public Task<Instrument> AddAsync(Instrument item)
    {
      return Task.Run(() => { m_database.CreateInstrument(item); return item; });
    }

    public Task<bool> DeleteAsync(Instrument item)
    {
      return Task.FromResult(m_database.DeleteInstrument(item) != 0);
    }

    public Task<Instrument?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Instrument?>(m_database.GetInstrument(id));
    }

    public Task<IEnumerable<Instrument>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<Instrument>>(m_database.GetInstruments());
    }

    public Task<Instrument> UpdateAsync(Instrument item)
    {
      return Task.Run(() => { m_database.UpdateInstrument(item); return item; });
    }

    //properties


    //methods


  }
}
