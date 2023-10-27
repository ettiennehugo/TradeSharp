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
    protected IDataStoreService m_dataStore;

    //constructors
    public ExchangeRepository(IDataStoreService dataStore)
    {
      m_dataStore = dataStore;
    }

    //finalizers


    //interface implementations
    public Task<Exchange> AddAsync(Exchange item)
    {
      return Task.Run(() => { m_dataStore.CreateExchange(item); return item; });
    }

    public Task<bool> DeleteAsync(Exchange item)
    {
      return Task.FromResult(m_dataStore.DeleteExchange(item.Id) != 0);
    }

    public Task<Exchange?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Exchange?>(m_dataStore.GetExchange(id));
    }

    public Task<IEnumerable<Exchange>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<Exchange>>(m_dataStore.GetExchanges());
    }

    public Task<Exchange> UpdateAsync(Exchange item)
    {
      return Task.Run(() => { m_dataStore.UpdateExchange(item); return item; });
    }

    //properties


    //methods


  }
}
