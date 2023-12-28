using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public Task<Exchange> AddAsync(Exchange item)
    {
      return Task.Run(() => { m_database.CreateExchange(item); return item; });
    }

    public Task<bool> DeleteAsync(Exchange item)
    {
      return Task.FromResult(m_database.DeleteExchange(item.Id) != 0);
    }

    public Task<Exchange?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Exchange?>(m_database.GetExchange(id));
    }

    public Task<IEnumerable<Exchange>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<Exchange>>(m_database.GetExchanges());
    }

    public Task<Exchange> UpdateAsync(Exchange item)
    {
      return Task.Run(() => { m_database.UpdateExchange(item); return item; });
    }

    //properties


    //methods


  }
}
