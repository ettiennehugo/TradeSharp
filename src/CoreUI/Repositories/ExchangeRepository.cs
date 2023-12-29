using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations exchange running parallel to the UI thread.
  /// </summary>
  public class ExchangeRepository : IExchangeRepository
  {
    //constants


    //enums


    //types


    //attributes
    protected IDatabase m_database;

    //constructors
    public ExchangeRepository(IDatabase database)
    {
      m_database = database;
    }

    //finalizers


    //interface implementations
    public bool Add(Exchange item)
    {
      m_database.CreateExchange(item);
      return true;
    }

    public bool Delete(Exchange item)
    {
      return m_database.DeleteExchange(item.Id) != 0;
    }

    public Exchange? GetItem(Guid id)
    {
      return m_database.GetExchange(id);
    }

    public IList<Exchange> GetItems()
    {
      return m_database.GetExchanges();
    }

    public bool Update(Exchange item)
    {
      m_database.UpdateExchange(item);
      return true;
    }

    //properties


    //methods


  }
}
