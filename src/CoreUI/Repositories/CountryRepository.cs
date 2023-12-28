using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Repositories
{
    /// <summary>
    /// Decorator for the database to facilitate operations on countries running parallel to the UI thread.
    /// </summary>
    public class CountryRepository : ObservableObject, ICountryRepository
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
    public Task<Country> AddAsync(Country item)
    {
      return Task.Run(() => { m_database.CreateCountry(item); return item; });
    }

    public Task<bool> DeleteAsync(Country item)
    {
      return Task.FromResult(m_database.DeleteCountry(item.Id) != 0);
    }

    public Task<Country?> GetItemAsync(Guid id)
    {
      return Task.FromResult<Country?>(m_database.GetCountry(id));
    }

    public Task<IEnumerable<Country>> GetItemsAsync()
    {
      return Task.FromResult<IEnumerable<Country>>(m_database.GetCountries());
    }

    public Task<Country> UpdateAsync(Country item)
    {
      throw new NotImplementedException("Countries can only be added or removed but not updated since the data is static.");
    }

    //properties


    //methods


  }
}
