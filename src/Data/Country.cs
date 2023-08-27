using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for Countries.
  /// </summary>
  public class Country : DataManagerObject, ICountry
  {
    //constants


    //enums


    //types


    //attributes
    protected CultureInfo m_cultureInfo;
    protected RegionInfo m_regionInfo;
    protected List<IHoliday> m_holidays;
    protected List<ICountryFundamental> m_fundamentals;
    protected List<IExchange> m_exchanges;

    //constructors
    public Country(IDataStoreService dataStore, IDataManagerService dataManager, string isoCode) : base(dataStore, dataManager)
    {
      m_cultureInfo = new CultureInfo(isoCode);

      if (m_cultureInfo.IsNeutralCulture)
        m_regionInfo = new RegionInfo(m_cultureInfo.Name);
      else
        m_regionInfo = new RegionInfo(m_cultureInfo.LCID);
      
      m_holidays = new List<IHoliday>();
      m_fundamentals = new List<ICountryFundamental>();
      m_exchanges = new List<IExchange>();
    }

    public Country(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Country country) : this(dataStore, dataManager, country.IsoCode) 
    {
      Id = country.Id;
      //NOTE: DataManager relinks the Exchanges, Holidays and CountryFundamentals on load.
    }

    //finalizers


    //interface implementations
    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return m_cultureInfo.Name;  //currently we return the culture only in it's native langauge
    }

    public string NameInLanguage(CultureInfo cultureInfo)
    {
      return m_cultureInfo.Name;  //currently we return the culture only in it's native langauge
    }

    //properties
    public Guid NameTextId { get => Guid.Empty; set { /* nothing to do */ } } //country names are retrieved from the CultureInfo class and we do not store them
    public string Name { get => m_regionInfo.DisplayName; set { /* nothing to do */ } }
    public string IsoCode { get => m_cultureInfo.Name; }
    public string LanguageCode { get { return m_cultureInfo.ThreeLetterISOLanguageName; } }
    public string RegionCode { get { return m_regionInfo.ThreeLetterISORegionName; } }
    
    public string Currency { get { return m_regionInfo.ISOCurrencySymbol; } }
    public string CurrencySymbol { get { return m_regionInfo.CurrencySymbol; } }
    public IList<ICountryFundamental> Fundamentals => m_fundamentals;
    public IList<IHoliday> Holidays => m_holidays;
    public IList<IExchange> Exchanges => m_exchanges;

    //methods
    internal void ClearHolidays() { m_holidays.Clear(); }
    internal void Add(IHoliday holiday)
    {
      if (m_holidays.Contains(holiday)) throw new DuplicateNameException("Duplicate country holiday.");
      m_holidays.Add(holiday);
    }
    internal void Remove(IHoliday holiday) { m_holidays.Remove(holiday); }

    internal void ClearFundamentals() { m_fundamentals.Clear(); }
    internal void Add(ICountryFundamental fundamental) { m_fundamentals.Add(fundamental); }
    internal void Remove(ICountryFundamental fundamental) { m_fundamentals.Remove(fundamental); }

    internal void ClearExchanges() { m_exchanges.Clear(); }
    internal void Add(IExchange exchange) { m_exchanges.Add(exchange); }
    internal void Remove(IExchange exchange) { m_exchanges.Remove(exchange); }
  }

  /// <summary>
  /// Special international/invariant country used for empty country assignments.  
  /// </summary>
  public class CountryInternational : ICountry
  {
    //constants


    //enums


    //types


    //attributes
    private static CountryInternational m_instance;

    //constructors
    static CountryInternational()
    {
      m_instance = new CountryInternational();
    }

    //finalizers


    //interface implementations
    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("CountryInternationalName", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.CountryInternationalName;
    }

    public string NameInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("CountryInternationalName", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.CountryInternationalName;
    }

    //properties
    public static CountryInternational Instance { get { return m_instance; } }
    public string Name { get => CultureInfo.InvariantCulture.Name; set { /* nothing to do */ } }
    public Guid NameTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public Guid Id => Guid.Empty;
    public string Currency => string.Empty;
    public string CurrencySymbol => string.Empty;
    public string IsoCode => CultureInfo.InvariantCulture.Name;
    public string LanguageCode => CultureInfo.InvariantCulture.ThreeLetterISOLanguageName;
    public string RegionCode => "";
    public IList<IExchange> Exchanges => Array.Empty<IExchange>();
    public IList<ICountryFundamental> Fundamentals => Array.Empty<ICountryFundamental>();
    public IList<IHoliday> Holidays => Array.Empty<IHoliday>();

    //methods

  }
}
