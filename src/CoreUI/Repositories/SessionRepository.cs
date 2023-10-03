using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    protected IDataStoreService m_dataStore;

    //constructors
    public SessionRepository(IDataStoreService dataStore)
    {
      ParentId = Guid.Empty;
      m_dataStore = dataStore;
    }


    //finalizers


    //interface implementations
    public Task<Session> AddAsync(Session item)
    {
      return Task.Run(() => { m_dataStore.CreateSession(item); return item; });
    }

    public Task<bool> DeleteAsync(Guid id)
    {
      return Task.FromResult(m_dataStore.DeleteSession(id) != 0);
    }

    public Task<Session?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Session?>(m_dataStore.GetSession(id));
    }

    public Task<IEnumerable<Session>> GetItemsAsync()
    {
      //only select data if we have a valid parent otherwise return zero result
      if (ParentId != Guid.Empty)
        return Task.FromResult<IEnumerable<Session>>(m_dataStore.GetSessions(ParentId));
      return Task.FromResult<IEnumerable<Session>>(Array.Empty<Session>());
    }

    public Task<Session> UpdateAsync(Session item)
    {
      return Task.Run(() => { m_dataStore.UpdateSession(item); return item; });
    }

    //properties
    public Guid ParentId { get; set; }

    //methods


  }
}
