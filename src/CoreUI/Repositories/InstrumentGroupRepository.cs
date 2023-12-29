using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on instrument group data running in parallel to the UI thread.
  /// </summary>
  public class InstrumentGroupRepository : IInstrumentGroupRepository
  {
    //constants


    //enums


    //types


    //attributes
    protected IDatabase m_database;

    //constructors
    public InstrumentGroupRepository(IDatabase database)
    {
      m_database = database;
    }

    //finalizers


    //interface implementations
    public bool Add(InstrumentGroup item)
    {
      m_database.CreateInstrumentGroup(item);
      return true;
    }

    public bool Delete(InstrumentGroup item)
    {
      return m_database.DeleteInstrumentGroup(item.Id) != 0;
    }

    public InstrumentGroup? GetItem(Guid id)
    {
      return m_database.GetInstrumentGroup(id);
    }

    public IList<InstrumentGroup> GetItems()
    {
      return m_database.GetInstrumentGroups();
    }

    public bool Update(InstrumentGroup item)
    {
      m_database.UpdateInstrumentGroup(item);
      return true;
    }


    //properties


    //methods


  }
}
