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
    protected IDatabase m_database;


    //constructors
    public HolidayRepository(IDatabase database)
    {
      ParentId = Guid.Empty;
      m_database = database;
    }

    //finalizers


    //interface implementations
    public Task<Holiday> AddAsync(Holiday item)
    {
      return Task.Run(() => { m_database.CreateHoliday(item); return item; });
    }

    public Task<bool> DeleteAsync(Holiday item)
    {
      return Task.FromResult(m_database.DeleteHoliday(item.Id) != 0);
    }

    public Task<Holiday?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Holiday?>(m_database.GetHoliday(id));
    }

    public Task<IEnumerable<Holiday>> GetItemsAsync()
    {
      //only select data if we have a valid parent otherwise return zero result
      if (ParentId != Guid.Empty)
        return Task.FromResult<IEnumerable<Holiday>>(m_database.GetHolidays(ParentId));
      return Task.FromResult<IEnumerable<Holiday>>(Array.Empty<Holiday>());
    }

    public Task<Holiday> UpdateAsync(Holiday item)
    {
      return Task.Run(() => { m_database.UpdateHoliday(item); return item; });
    }

    //properties
    public Guid ParentId { get; set; }

    //methods


  }
}
