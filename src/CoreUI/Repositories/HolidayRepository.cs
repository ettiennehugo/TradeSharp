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
    public bool Add(Holiday item)
    {
      m_database.CreateHoliday(item);
      return true;
    }

    public bool Delete(Holiday item)
    {
      return m_database.DeleteHoliday(item.Id) != 0;
    }

    public Holiday? GetItem(Guid id)
    {
      return m_database.GetHoliday(id);
    }

    public IList<Holiday> GetItems()
    {
      //only select data if we have a valid parent otherwise return zero result
      if (ParentId != Guid.Empty)
        return m_database.GetHolidays(ParentId);
      return Array.Empty<Holiday>();
    }

    public bool Update(Holiday item)
    {
      m_database.UpdateHoliday(item);
      return true;
    }

    //properties
    public Guid ParentId { get; set; }

    //methods


  }
}
