using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on exchange sessions running parallel to the UI thread.
  /// </summary>
  public class SessionRepository : ISessionRepository
  {
    //constants


    //enums


    //types


    //attributes
    protected IDatabase m_database;

    //constructors
    public SessionRepository(IDatabase database)
    {
      ParentId = Guid.Empty;
      m_database = database;
    }

    //finalizers


    //interface implementations
    public bool Add(Session item)
    {
      m_database.CreateSession(item);
      return true;
    }

    public bool Delete(Session item)
    {
      return m_database.DeleteSession(item.Id) != 0;
    }

    public Session? GetItem(Guid id)
    {
      return m_database.GetSession(id);
    }

    public IList<Session> GetItems()
    {
      //only select data if we have a valid parent otherwise return zero result
      if (ParentId != Guid.Empty)
        return m_database.GetSessions(ParentId);
      return Array.Empty<Session>();
    }

    public bool Update(Session item)
    {
      m_database.UpdateSession(item);
      return true;
    }

    //properties
    public Guid ParentId { get; set; }

    //methods


  }
}
