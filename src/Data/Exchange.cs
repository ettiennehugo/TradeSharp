using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using TradeSharp.Common;
using System.Net.Http.Headers;
using static TradeSharp.Common.IConfigurationService;
using System.Globalization;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class implementation for exchanges.
  /// </summary>
  public class Exchange : NameObject, IExchange
  {
    //constants


    //enums


    //types


    //attributes
    protected Dictionary<DayOfWeek, IList<ISession>> m_sessions;
    protected List<IExchangeHoliday> m_holidays;
    protected Dictionary<string, IInstrument> m_instruments;

    //constructors
    public Exchange(IDataStoreService dataStore, IDataManagerService dataManager, string name) : base(dataStore, dataManager, name)
    {
      Country = CountryInternational.Instance;
      TimeZone = TimeZoneInfo.Utc;
      m_sessions = new Dictionary<DayOfWeek, IList<ISession>>();
      foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        m_sessions.Add(day, new List<ISession>());
      m_holidays = new List<IExchangeHoliday>();
      m_instruments = new Dictionary<string, IInstrument>();
    }

    public Exchange(IDataStoreService dataStore, IDataManagerService dataManager, ICountry country, string name, TimeZoneInfo timeZone) : this(dataStore, dataManager, name)
    {
      Country = country;
      TimeZone = timeZone;
      //NOTE: The exchange is added to the country by the DataManager.
    }

    public Exchange(IDataStoreService dataStore, IDataManagerService dataManager, IDataStoreService.Exchange exchange) : this(dataStore, dataManager, exchange.Name)
    {
      Id = exchange.Id;
      NameTextId = exchange.NameTextId;
      Name = DataStore.GetText(NameTextId);
      Country = CountryInternational.Instance;
      TimeZone = exchange.TimeZone;
      //NOTE: DataManager relinks the Country, Holidays, Sessions and Instruments.
    }

    //finalizers


    //interface implementations


    //properties
    public ICountry Country { get; set; }
    public TimeZoneInfo TimeZone { get; }
    public IDictionary<DayOfWeek, IList<ISession>> Sessions => m_sessions;
    public IList<IExchangeHoliday> Holidays { get { return m_holidays; } }
    public IDictionary<string, IInstrument> Instruments { get { return m_instruments; } }

    //methods
    internal void ClearHolidays() { m_holidays.Clear(); }
    internal void Add(IExchangeHoliday holiday)
    {
      if (m_holidays.Contains(holiday)) throw new DuplicateNameException("Duplicate exchange holiday.");
      m_holidays.Add(holiday);
    }
    internal void Remove(IExchangeHoliday holiday) { m_holidays.Remove(holiday); }

    internal void ClearSessions() 
    { 
      m_sessions.Clear();
      foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        m_sessions.Add(day, new List<ISession>());
    }

    internal void Add(ISession session)
    {
      if (m_sessions[session.Day].Contains(session)) throw new DuplicateNameException("Duplicate session added to day.");
      m_sessions[session.Day].Add(session);
    }

    internal void Remove(ISession session)
    {
      m_sessions[session.Day].Remove(session);
    }

    internal void ClearInstruments() { m_instruments.Clear(); } 
    internal void Add(IInstrument instrument)
    {
      if (m_instruments.ContainsKey(instrument.Ticker)) throw new DuplicateNameException(string.Format("Exchange {0} alreay contains instrment {1}", Name, instrument.Ticker));
      m_instruments.Add(instrument.Ticker, instrument);
    }

    internal void Remove(IInstrument instrument)
    {
      m_instruments.Remove(instrument.Ticker);
    }

    public override bool Equals(object? obj)
    {
      //exchanges are name equivalent
      return obj is Exchange exchange &&
             EqualityComparer<ICountry>.Default.Equals(Country, exchange.Country) &&
             Name.ToUpper() == exchange.Name.ToUpper();
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Id, Country, Name.ToUpper());
    }
  }

  /// <summary>
  /// Special null object exchange where needed.
  /// </summary>
  public class ExchangeNone : IExchange
  {
    //constants


    //enums


    //types


    //attributes
    static private ExchangeNone m_instance;
    private IDictionary<string, IInstrument> m_instruments;
    private IDictionary<DayOfWeek, IList<ISession>> m_sessions;

    //constructors
    static ExchangeNone()
    {
      m_instance = new ExchangeNone();
    }

    private ExchangeNone() 
    {
      m_instruments = new Dictionary<string, IInstrument>();
      m_sessions = new Dictionary<DayOfWeek, IList<ISession>>();
      foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        m_sessions.Add(day, Array.Empty<ISession>());
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public static ExchangeNone Instance { get => m_instance; }
    public Guid Id => Guid.Empty;
    public string Name { get => Common.Resources.ExchangeNoneName; set { /* nothing to do */ } }
    public Guid NameTextId { get => Guid.Empty; set { /* nothing to do */ } }
    public ICountry Country { get => CountryInternational.Instance; set { /* nothing to do */ } }
    public IList<IExchangeHoliday> Holidays { get => Array.Empty<IExchangeHoliday>(); }
    public IDictionary<string, IInstrument> Instruments { get => m_instruments; }
    public IDictionary<DayOfWeek, IList<ISession>> Sessions { get => m_sessions; }
    public TimeZoneInfo TimeZone { get => TimeZoneInfo.Utc; }

    public string NameInLanguage(string threeLetterLanguageCode)
    {
      return Common.Resources.ResourceManager.GetString("ExchangeNoneName", CultureInfo.GetCultureInfo(threeLetterLanguageCode)) ?? Common.Resources.ExchangeNoneName;
    }

    public string NameInLanguage(CultureInfo cultureInfo)
    {
      return Common.Resources.ResourceManager.GetString("ExchangeNoneName", CultureInfo.GetCultureInfo(cultureInfo.ThreeLetterISOLanguageName)) ?? Common.Resources.ExchangeNoneName;
    }
  }



}
