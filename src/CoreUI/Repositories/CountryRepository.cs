using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
  /// <summary>
  /// Decorator for the database to facilitate operations on countries running parallel to the UI thread.
  /// </summary>
  public class CountryRepository : ICountryRepository
  {
    //constants


    //enums


    //types


    //attributes
    private List<CountryInfo> s_countryList = new List<CountryInfo>();
    protected IDatabase m_database;

    //constructors
    public CountryRepository(IDatabase database)
    {
      m_database = database;
    }

    //finalizers


    //interface implementations
    public bool Add(Country item)
    {
      m_database.CreateCountry(item);
      return true;
    }

    public bool Delete(Country item)
    {
      return m_database.DeleteCountry(item.Id) != 0;
    }

    public Country? GetItem(Guid id)
    {
      return m_database.GetCountry(id);
    }

    public IList<Country> GetItems()
    {
      return m_database.GetCountries();
    }

    public bool Update(Country item)
    {
      throw new NotImplementedException("Countries can only be added or removed but not updated since the data is static.");
    }

    //properties


    //methods


  }
}
