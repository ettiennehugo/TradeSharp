using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on holidays associated with countries and exchanges.
  /// </summary>
  public class HolidayRepository : IHolidayRepository
  {

    //constants


    //enums


    //types


    //attributes
    protected IDataStoreService m_dataStore;


    //constructors
    public HolidayRepository(IDataStoreService dataStore)
    {
      ParentId = Guid.Empty;
      m_dataStore = dataStore;
    }

    //finalizers


    //interface implementations


    //properties
    public Guid ParentId { get; set; }

    //methods
    public Task<Holiday> AddAsync(Holiday item)
    {
      return Task.Run(() => { m_dataStore.CreateHoliday(item); return item; });
    }

    public Task<bool> DeleteAsync(Guid id)
    {
      return Task.FromResult(m_dataStore.DeleteHoliday(id) != 0);
    }

    public Task<Holiday?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Holiday?>(m_dataStore.GetHoliday(id));
    }

    public Task<IEnumerable<Holiday>> GetItemsAsync()
    {
      //only select data if we have a valid parent otherwise return zero result
      if (ParentId != Guid.Empty)
        return Task.FromResult<IEnumerable<Holiday>>(m_dataStore.GetHolidays(ParentId));
      return Task.FromResult<IEnumerable<Holiday>>(Array.Empty<Holiday>());
    }

    public Task<Holiday> UpdateAsync(Holiday item)
    {
      return Task.Run(() => { m_dataStore.UpdateHoliday(item); return item; });
    }
  }
}
