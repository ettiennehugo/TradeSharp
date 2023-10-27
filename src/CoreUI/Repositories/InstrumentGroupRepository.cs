using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    protected IDataStoreService m_dataStore;

    //constructors
    public InstrumentGroupRepository(IDataStoreService dataStoreService)
    {
      m_dataStore = dataStoreService;
    }

    //finalizers


    //interface implementations
    public Task<InstrumentGroup> AddAsync(InstrumentGroup item)
    {
      return Task.Run(() => { m_dataStore.CreateInstrumentGroup(item); return item; });
    }

    public Task<bool> DeleteAsync(InstrumentGroup item)
    {
      return Task.FromResult(m_dataStore.DeleteInstrumentGroup(item.Id) != 0);
    }

    public Task<InstrumentGroup?> GetItemAsync(Guid id)
    {
      return Task.FromResult<InstrumentGroup?>(m_dataStore.GetInstrumentGroup(id));
    }

    public Task<IEnumerable<InstrumentGroup>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<InstrumentGroup>>(m_dataStore.GetInstrumentGroups());
    }

    public Task<InstrumentGroup> UpdateAsync(InstrumentGroup item)
    {
      return Task.Run(() => { m_dataStore.UpdateInstrumentGroup(item); return item; });
    }


    //properties


    //methods


  }
}
