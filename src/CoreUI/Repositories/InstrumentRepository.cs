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

    public IList<Instrument> GetItems()
    {
      return m_database.GetInstruments();
    }

    public bool Update(Instrument item)
    {
      m_database.UpdateInstrument(item);
      return true;
    }

    //properties


    //methods


  }
}
