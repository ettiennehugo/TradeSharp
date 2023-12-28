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
    protected IDatabase m_database;

    //constructors
    public InstrumentGroupRepository(IDatabase database)
    {
      m_database = database;
    }

    //finalizers


    //interface implementations
    public Task<InstrumentGroup> AddAsync(InstrumentGroup item)
    {
      return Task.Run(() => { m_database.CreateInstrumentGroup(item); return item; });
    }

    public Task<bool> DeleteAsync(InstrumentGroup item)
    {
      return Task.FromResult(m_database.DeleteInstrumentGroup(item.Id) != 0);
    }

    public Task<InstrumentGroup?> GetItemAsync(Guid id)
    {
      return Task.FromResult<InstrumentGroup?>(m_database.GetInstrumentGroup(id));
    }

    public Task<IEnumerable<InstrumentGroup>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<InstrumentGroup>>(m_database.GetInstrumentGroups());
    }

    public Task<InstrumentGroup> UpdateAsync(InstrumentGroup item)
    {
      return Task.Run(() => { m_database.UpdateInstrumentGroup(item); return item; });
    }


    //properties


    //methods


  }
}
