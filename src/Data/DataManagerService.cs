using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TradeSharp.Common;
using static TradeSharp.Data.IDataStoreService;

namespace TradeSharp.Data
{
  //TODO: Determine how to use the observer pattern in C#.
  // * Strategies and indicators are observable as well so that charts and portfolio's can observe them and update the chart when the strategy or indicator changes.
  // * TBD: How will playback work on a chart??? Maybe introduce a playback layer that records object states over time and allows playback, this playback layer would be observable by the charts/portfolios/screeners to update their associated state. 

  /// <summary>
  /// DataManager to support the data model, it's main purpose is to efficiently load the data, store it in memory where appropriate and make sure the data model stays intact when it's modified.
  /// IMPORTANT: Do NOT keep references of objects in the data manager if you're going to call Refresh on the data manager, the Refresh method will replace all objects in the data manager with new objects
  ///            thus leading to stale object references in your code.
  /// </summary>
  public class DataManagerService : IDataManagerService
  {
    //constants


    //enums


    //types


    //attributes
    protected ILogger<DataManagerService> m_logger;
    protected IDataStoreService m_dataStore;
    protected IDataProvider m_dataProvider;
    protected IList<IDataProvider> m_dataProviders;
    protected IConfigurationService m_configuration;
    protected Dictionary<Guid, Country> m_countries;
    protected Dictionary<Guid, Holiday> m_holidays;
    protected Dictionary<Guid, Exchange> m_exchanges;
    protected Dictionary<Guid, Session> m_sessions;
    protected Dictionary<Guid, InstrumentGroup> m_instrumentGroup;
    protected Dictionary<Guid, Instrument> m_instruments;
    protected Dictionary<Guid, Fundamental> m_fundamentals;
    protected ConcurrentDictionary<int, WeakReference<Common.IObserver<ModelChange>>> m_modelChangeObservers;
    protected ConcurrentDictionary<int, WeakReference<Common.IObserver<FundamentalChange>>> m_fundamentalChangeObservers;
    protected ConcurrentDictionary<int, WeakReference<Common.IObserver<PriceChange>>> m_priceChangeObservers;
    protected ConcurrentQueue<ModelChange> m_modelChanges;
    protected ConcurrentQueue<FundamentalChange> m_fundamentalChanges;
    protected ConcurrentQueue<PriceChange> m_priceChanges;
    protected long m_modelChangePauseCount;
    protected long m_fundamentalChangePauseCount;
    protected long m_priceChangePauseCount;
    protected bool m_refreshModel;

    //constructors
    /// <summary>
    /// Constructor used for testing.
    /// </summary>
    public DataManagerService(IConfigurationService configuration, ILoggerFactory loggerFactory, IDataStoreService dataStore)
    {
      m_configuration = configuration;
      m_logger = loggerFactory.CreateLogger<DataManagerService>();
      m_countries = new Dictionary<Guid, Country>();
      m_holidays = new Dictionary<Guid, Holiday>();
      m_exchanges = new Dictionary<Guid, Exchange>();
      m_sessions = new Dictionary<Guid, Session>();
      m_instruments = new Dictionary<Guid, Instrument>();
      m_fundamentals = new Dictionary<Guid, Fundamental>();
      m_instrumentGroup = new Dictionary<Guid, InstrumentGroup>();

      m_modelChangeObservers = new ConcurrentDictionary<int, WeakReference<Common.IObserver<ModelChange>>>();
      m_fundamentalChangeObservers = new ConcurrentDictionary<int, WeakReference<Common.IObserver<FundamentalChange>>>();
      m_priceChangeObservers = new ConcurrentDictionary<int, WeakReference<Common.IObserver<PriceChange>>>();
      m_modelChanges = new ConcurrentQueue<ModelChange>();
      m_fundamentalChanges = new ConcurrentQueue<FundamentalChange>();
      m_priceChanges = new ConcurrentQueue<PriceChange>();
      m_modelChangePauseCount = 0;
      m_fundamentalChangePauseCount = 0;
      m_priceChangePauseCount = 0;

      //instantiate the data providers
      if (m_configuration.DataProviders.Count == 0) throw new ArgumentNullException(Resources.DataProviderRequired);
      m_dataProviders = new List<IDataProvider>();
      foreach (var dataProviderConfig in m_configuration.DataProviders)
      {
        Type dataProviderType = Type.GetType(dataProviderConfig.Key) ?? throw new ArgumentException(string.Format(Resources.DataProviderCreateFail, dataProviderConfig.Key.ToString()));
        IDataProvider dataProvider = (IDataProvider?)Activator.CreateInstance("", dataProviderConfig.Key) ?? throw new ArgumentException(string.Format(Resources.DataProviderCreateFail, dataProviderConfig.Key.ToString()));
        dataProvider.Create(dataProviderConfig.Value);
        m_dataProviders.Add(dataProvider);
      }

      m_dataProvider = m_dataProviders.ElementAt(0);
      m_dataStore = dataStore;

      //NOTE: A refresh is required to load the data from the data store into memory.
    }

    //finalizers
    public void Dispose()
    {
      m_dataStore.Dispose();
      m_countries.Clear();
      m_holidays.Clear();
      m_exchanges.Clear();
      m_sessions.Clear();
      m_instruments.Clear();
      m_fundamentals.Clear();
      m_instrumentGroup.Clear();
      m_modelChangeObservers.Clear();
      m_fundamentalChangeObservers.Clear();
      m_priceChangeObservers.Clear();
    }

    //interface implementations
    public void Subscribe(Common.IObserver<ModelChange> observer)
    {
      m_modelChangeObservers.TryAdd(observer.GetHashCode(), new WeakReference<Common.IObserver<ModelChange>>(observer));
    }

    public void Unsubscribe(Common.IObserver<ModelChange> observer)
    {
      m_modelChangeObservers.TryRemove(observer.GetHashCode(), out _);
    }

    public void Subscribe(Common.IObserver<FundamentalChange> observer)
    {
      m_fundamentalChangeObservers.TryAdd(observer.GetHashCode(), new WeakReference<Common.IObserver<FundamentalChange>>(observer));
    }

    public void Unsubscribe(Common.IObserver<FundamentalChange> observer)
    {
      m_fundamentalChangeObservers.TryRemove(observer.GetHashCode(), out _);
    }

    public void Subscribe(Common.IObserver<PriceChange> observer)
    {
      m_priceChangeObservers.TryAdd(observer.GetHashCode(), new WeakReference<Common.IObserver<PriceChange>>(observer));
    }

    public void Unsubscribe(Common.IObserver<PriceChange> observer)
    {
      m_priceChangeObservers.TryRemove(observer.GetHashCode(), out _);
    }

    public long PauseModelChangeNotifications()
    {
      return Interlocked.Increment(ref m_modelChangePauseCount);
    }

    public long ResumeModelChangeNotifications()
    {
      long modelChangePauseCount = Interlocked.Decrement(ref m_modelChangePauseCount);

      //change notification pause count should never be less than zero if calling code is written correctly, log a error and callstack to debug
      if (modelChangePauseCount < 0)
        m_logger.LogDebug(string.Format("ResumeModelChangeNotifications called with m_modelChangePauseCount == 0 (Callstack: {0})", Environment.StackTrace));

      notifyModelObservers(modelChangePauseCount);
      return modelChangePauseCount;
    }

    public long PauseFundamentalChangeNotifications()
    { 
      return Interlocked.Increment(ref m_fundamentalChangePauseCount);
    }

    public long ResumeFundamentalChangeNotifications()
    {
      long fundamentalChangePauseCount = Interlocked.Decrement(ref m_fundamentalChangePauseCount);

      //change notification pause count should never be less than zero if calling code is written correctly, log a error and callstack to debug
      if (fundamentalChangePauseCount < 0)
        m_logger.LogDebug(string.Format("ResumeFundamentalChangeNotifications called with m_fundamentalChangePauseCount == 0 (Callstack: {0})", Environment.StackTrace));

      notifyFundamentalObservers(fundamentalChangePauseCount);
      return fundamentalChangePauseCount;
    }

    public long PausePriceChangeNotifications()
    {
      return Interlocked.Increment(ref m_priceChangePauseCount);
    }

    public long ResumePriceChangeNotifications()
    {
      long priceChangePauseCount = Interlocked.Decrement(ref m_priceChangePauseCount);

      //change notification pause count should never be less than zero if calling code is written correctly, log a error and callstack to debug
      if (priceChangePauseCount < 0)
        m_logger.LogDebug(string.Format("ResumePriceChangeNotifications called with m_priceChangePauseCount == 0 (Callstack: {0})", Environment.StackTrace));

      notifyPriceObservers(priceChangePauseCount);
      return priceChangePauseCount;
    }

    public ICountry Create(string isoCode)
    {
      Country country = new Country(m_dataStore, this, isoCode);
      m_dataStore.CreateCountry(new IDataStoreService.Country(country.Id, country.IsoCode));
      m_countries.Add(country.Id, country);
      notifyModelObservers(ModelChangeType.Create, country);
      return country;
    }

    public IHoliday Create(ICountry country, string name, Months month, int dayOfMonth, MoveWeekendHoliday moveWeekendHoliday)
    {
      Holiday holiday = new Holiday(m_dataStore, this, country, name, month, dayOfMonth, moveWeekendHoliday);
      holiday.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, holiday.Name);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, country.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, holiday.DayOfMonth, 0, 0, holiday.MoveWeekendHoliday));

      if (country is Country)
      {
        Country countryImpl = (Country)country;
        countryImpl.Add(holiday);
      }

      m_holidays.Add(holiday.Id, holiday);
      notifyModelObservers(ModelChangeType.Create, holiday);
      return holiday;
    }

    public IHoliday Create(ICountry country, string name, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday)
    {
      Holiday holiday = new Holiday(m_dataStore, this, country, name, month, dayOfWeek, weekOfMonth, moveWeekendHoliday);
      holiday.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, holiday.Name);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, country.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, 0, holiday.DayOfWeek, holiday.WeekOfMonth, holiday.MoveWeekendHoliday));

      if (country is Country)
      {
        Country countryImpl = (Country)country;
        countryImpl.Add(holiday);
      }

      m_holidays.Add(holiday.Id, holiday);
      notifyModelObservers(ModelChangeType.Create, holiday);
      return holiday;
    }

    public IExchange Create(ICountry country, string name, TimeZoneInfo timeZone)
    {
      Exchange exchange = new Exchange(m_dataStore, this, country, name, timeZone);
      exchange.Country = country;
      exchange.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, exchange.Name);
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(exchange.Id, exchange.Country.Id, exchange.NameTextId, exchange.Name, exchange.TimeZone));

      if (country is Country)
      {
        Country countryImpl = (Country)country;
        countryImpl.Add(exchange);
      }

      m_exchanges.Add(exchange.Id, exchange);
      notifyModelObservers(ModelChangeType.Create, exchange);
      return exchange;
    }

    public IHoliday Create(IExchange exchange, string name, Months month, int dayOfMonth, MoveWeekendHoliday moveWeekendHoliday)
    {
      ExchangeHoliday holiday = new ExchangeHoliday(m_dataStore, this, exchange, name, month, dayOfMonth, moveWeekendHoliday);
      holiday.Exchange = exchange;
      holiday.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, holiday.Name);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, exchange.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, holiday.DayOfMonth, 0, 0, holiday.MoveWeekendHoliday));
      m_holidays.Add(holiday.Id, holiday);

      if (exchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)exchange;
        exchangeImpl.Add(holiday);
      }

      notifyModelObservers(ModelChangeType.Create, holiday);
      return holiday;
    }

    public IHoliday Create(IExchange exchange, string name, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday)
    {
      ExchangeHoliday holiday = new ExchangeHoliday(m_dataStore, this, exchange, name, month, dayOfWeek, weekOfMonth, moveWeekendHoliday);
      holiday.Exchange = exchange;
      holiday.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, holiday.Name);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, exchange.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, 0, holiday.DayOfWeek, holiday.WeekOfMonth, holiday.MoveWeekendHoliday));
      m_holidays.Add(holiday.Id, holiday);

      if (exchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)exchange;
        exchangeImpl.Add(holiday);
      }

      notifyModelObservers(ModelChangeType.Create, holiday);
      return holiday;
    }

    public ISession Create(IExchange exchange, DayOfWeek day, string name, TimeOnly start, TimeOnly end)
    {
      Session session = new Session(m_dataStore, this, exchange, day, name, start, end);
      session.Exchange = exchange;
      session.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, session.Name);
      m_dataStore.CreateSession(new IDataStoreService.Session(session.Id, session.NameTextId, session.Name, session.Exchange.Id, session.Day, session.Start, session.End));

      if (exchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)exchange;
        exchangeImpl.Add(session);
      }

      m_sessions.Add(session.Id, session);
      notifyModelObservers(ModelChangeType.Create, session);
      return session;
    }

    public IInstrument Create(IExchange exchange, InstrumentType type, string ticker, string name, string description, DateTime inceptionDate)
    {
      Instrument instrument = new Instrument(m_dataStore, this, exchange, type, ticker, name, description, inceptionDate);
      instrument.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, instrument.Name);
      instrument.DescriptionTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, instrument.Description);
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(instrument.Id, instrument.Type, instrument.Ticker, instrument.NameTextId, instrument.Name, instrument.DescriptionTextId, instrument.Description, instrument.InceptionDate, new List<Guid>(), instrument.PrimaryExchange.Id, new List<Guid>()));

      if (exchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)exchange;
        exchangeImpl.Add(instrument);
      }

      m_instruments.Add(instrument.Id, instrument);
      notifyModelObservers(ModelChangeType.Create, instrument);
      return instrument;
    }

    public void CreateSecondaryExchange(IInstrument instrument, IExchange exchange)
    {
      m_dataStore.CreateInstrument(instrument.Id, exchange.Id);

      if (instrument is Instrument)
      {
        Instrument instrumentImpl = (Instrument)instrument;
        instrumentImpl.Add(exchange);
      }

      if (exchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)exchange;
        exchangeImpl.Add(instrument);
      }

      notifyModelObservers(ModelChangeType.Update, instrument);
    }

    public IFundamental Create(string name, string description, FundamentalCategory category, FundamentalReleaseInterval releaseInterval)
    {
      Fundamental fundamental = new Fundamental(m_dataStore, this, name, description, category, releaseInterval);
      fundamental.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, fundamental.Name);
      fundamental.DescriptionTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, fundamental.Description);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      m_fundamentals.Add(fundamental.Id, fundamental);
      notifyModelObservers(ModelChangeType.Create, fundamental);
      return fundamental;
    }

    public ICountryFundamental Create(IFundamental fundamental, ICountry country)
    {
      CountryFundamental countryFundamental = new CountryFundamental(fundamental, country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataProvider.Name, fundamental.Id, country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      if (country is Country)
      {
        Country countryImpl = (Country)country;
        countryImpl.Add(countryFundamental);
      }

      notifyModelObservers(ModelChangeType.Create, countryFundamental);
      return countryFundamental;
    }

    public IInstrumentFundamental Create(IFundamental fundamental, IInstrument instrument)
    {
      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataProvider.Name, fundamental.Id, instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      if (instrument is Instrument)
      {
        Instrument instrumentImpl = (Instrument)instrument;
        instrumentImpl.Add(instrumentFundamental);
      }

      notifyModelObservers(ModelChangeType.Create, instrumentFundamental);
      return instrumentFundamental;
    }

    public IInstrumentGroup Create(string name, string description, IInstrumentGroup? parent = null)
    {
      InstrumentGroup instrumentGroup = new InstrumentGroup(m_dataStore, this, name, description, parent ?? InstrumentGroupRoot);
      instrumentGroup.NameTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, instrumentGroup.Name);
      instrumentGroup.DescriptionTextId = m_dataStore.CreateText(Configuration.CultureInfo.ThreeLetterISOLanguageName, instrumentGroup.Description);
      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup.Id, instrumentGroup.Parent.Id, instrumentGroup.NameTextId, instrumentGroup.Name, instrumentGroup.DescriptionTextId, instrumentGroup.Description, Array.Empty<Guid>()));

      if (parent is InstrumentGroup)
      {
        InstrumentGroup instrumentGroupImpl = (InstrumentGroup)parent;
        instrumentGroupImpl.Add(instrumentGroup);
      }

      m_instrumentGroup.Add(instrumentGroup.Id, instrumentGroup);
      notifyModelObservers(ModelChangeType.Create, instrumentGroup);
      return instrumentGroup;
    }

    public void Update(IName namedObject, string name)
    {
      m_dataStore.UpdateText(namedObject.NameTextId, m_configuration.CultureInfo.ThreeLetterISOLanguageName, name);
      namedObject.Name = name;
      notifyModelObservers(ModelChangeType.Update, namedObject);
    }

    public void Update(IName namedObject, string name, CultureInfo culture)
    {
      m_dataStore.UpdateText(namedObject.NameTextId, culture.ThreeLetterISOLanguageName, name);
      if (m_configuration.CultureInfo == culture) namedObject.Name = name;
      notifyModelObservers(ModelChangeType.Update, namedObject);
    }

    public void Update(IDescription descriptionObject, string description)
    {
      m_dataStore.UpdateText(descriptionObject.DescriptionTextId, m_configuration.CultureInfo.ThreeLetterISOLanguageName, description);
      descriptionObject.Description = description;
      notifyModelObservers(ModelChangeType.Update, descriptionObject);
    }

    public void Update(IDescription descriptionObject, string description, CultureInfo culture)
    {
      m_dataStore.UpdateText(descriptionObject.DescriptionTextId, culture.ThreeLetterISOLanguageName, description);
      if (m_configuration.CultureInfo == culture) descriptionObject.Description = description;
      notifyModelObservers(ModelChangeType.Update, descriptionObject);
    }

    public void Update(ISession session, DayOfWeek day, TimeOnly start, TimeOnly end)
    {
      m_dataStore.UpdateSession(session.Id, day, start, end);

      if (session is Session)
      {
        Session sessionImpl = (Session)session;

        if (sessionImpl.Day != day && session.Exchange is Exchange)
        {
          Exchange exchange = (Exchange)session.Exchange;
          exchange.Sessions[sessionImpl.Day].Remove(sessionImpl);
          exchange.Sessions[day].Add(sessionImpl);
        }
        
        sessionImpl.Day = day;
        sessionImpl.Start = start;
        sessionImpl.End = end;
      }

      notifyModelObservers(ModelChangeType.Update, session);
    }

    public void Update(IInstrument instrument, Guid primaryExchangeId, string ticker, DateTime inceptionDate)
    {
      m_dataStore.UpdateInstrument(instrument.Id, primaryExchangeId, ticker.ToUpper(), inceptionDate);

      if (instrument is Instrument)
      {
        Instrument instrumentImpl = (Instrument)instrument;
        instrumentImpl.PrimaryExchange = m_exchanges[primaryExchangeId];
        instrumentImpl.Ticker = ticker.ToUpper();
        instrumentImpl.InceptionDate = inceptionDate;
      }

      notifyModelObservers(ModelChangeType.Update, instrument);
    }

    public void Update(IInstrumentGroup instrumentGroup, IInstrumentGroup parent)
    {
      m_dataStore.UpdateInstrumentGroup(instrumentGroup.Id, parent.Id);

      if (instrumentGroup is InstrumentGroup)
      {
        InstrumentGroup instrumentGroupImpl = (InstrumentGroup)instrumentGroup;

        if (instrumentGroupImpl.Parent is InstrumentGroup)
        {
          InstrumentGroup parentImpl = (InstrumentGroup)instrumentGroupImpl.Parent;
          parentImpl.Remove(instrumentGroup);
        }

        if (parent is InstrumentGroup)
        {
          InstrumentGroup parentImpl = (InstrumentGroup)parent;
          parentImpl.Add(instrumentGroup);
        }

        instrumentGroupImpl.Parent = parent;
      }

      notifyModelObservers(ModelChangeType.Update, instrumentGroup);
    }

    public void Update(IInstrumentGroup instrumentGroup, IInstrument instrument)
    {
      m_dataStore.CreateInstrumentGroupInstrument(instrumentGroup.Id, instrument.Id);

      if (instrumentGroup is InstrumentGroup)
      {
        InstrumentGroup instrumentGroupImpl = (InstrumentGroup)instrumentGroup;
        instrumentGroupImpl.Add(instrument);
      }

      if (instrument is Instrument)
      {
        Instrument instrumentImpl = (Instrument)instrument;
        instrumentImpl.Add(instrumentGroup);
      }

      notifyModelObservers(ModelChangeType.Update, instrumentGroup);
    }

    public void Update(ICountryFundamental fundamental, DateTime dateTime, double value)
    {
      m_dataStore.UpdateCountryFundamental(m_dataProvider.Name, fundamental.FundamentalId, fundamental.CountryId, dateTime, value);

      if (fundamental is CountryFundamental)
      {
        CountryFundamental countryFundamental = (CountryFundamental)fundamental;
        countryFundamental.Add(dateTime, (decimal)value);
      }

      notifyFundamentalObservers(FundamentalChangeType.Update, fundamental, dateTime, value);
    }

    public void Update(IInstrumentFundamental fundamental, DateTime dateTime, double value)
    {
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider.Name, fundamental.FundamentalId, fundamental.InstrumentId, dateTime, value);

      if (fundamental is InstrumentFundamental)
      {
        InstrumentFundamental instrumentFundamental = (InstrumentFundamental)fundamental;
        instrumentFundamental.Add(dateTime, (decimal)value);
      }

      notifyFundamentalObservers(FundamentalChangeType.Update, fundamental, dateTime, value);
    }

    public void Update(IInstrument instrument, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume, bool syntheticData)
    {
      m_dataStore.UpdateData(m_dataProvider.Name, instrument.Id, instrument.Ticker, resolution, dateTime, open, high, low, close, volume, syntheticData);

      PriceChange priceChange = new PriceChange();
      priceChange.ChangeType = PriceChangeType.Update;
      priceChange.Instrument = instrument;
      priceChange.Resolution = resolution;
      priceChange.From = dateTime;
      priceChange.To = dateTime;
      m_priceChanges.Enqueue(priceChange);

      notifyPriceObservers();
    }

    public void Update(IInstrument instrument, Resolution resolution, IDataStoreService.BarData bars)
    {
      m_dataStore.UpdateData(m_dataProvider.Name, instrument.Id, instrument.Ticker, resolution, bars);

      PriceChange priceChange = new PriceChange();
      priceChange.ChangeType = PriceChangeType.Update;
      priceChange.Instrument = instrument;
      priceChange.Resolution = resolution;
      priceChange.From = bars.DateTime[0];
      priceChange.To = bars.DateTime[bars.Count - 1];
      m_priceChanges.Enqueue(priceChange);

      notifyPriceObservers();
    }

    public void Update(IInstrument instrument, IDataStoreService.Level1Data ticks)
    {
      m_dataStore.UpdateData(m_dataProvider.Name, instrument.Id, instrument.Ticker, ticks);

      PriceChange priceChange = new PriceChange();
      priceChange.ChangeType = PriceChangeType.Update;
      priceChange.Instrument = instrument;
      priceChange.Resolution = Resolution.Level1;
      priceChange.From = ticks.DateTime[0];
      priceChange.To = ticks.DateTime[ticks.Count - 1];
      m_priceChanges.Enqueue(priceChange);

      notifyPriceObservers();
    }

    public void Delete(ICountry country)
    {
      m_dataStore.DeleteCountry(country.Id);
      foreach (IHoliday holiday in country.Holidays) m_dataStore.DeleteHoliday(holiday.Id);
      foreach (IExchange exchange in country.Exchanges) m_dataStore.DeleteExchange(exchange.Id);
      foreach (ICountryFundamental countryFundamental in country.Fundamentals) m_dataStore.DeleteCountryFundamental(m_dataProvider.Name, countryFundamental.FundamentalId, countryFundamental.CountryId);

      if (country is Country)
      {
        Country countryImpl = (Country)country;
        countryImpl.ClearHolidays();
        countryImpl.ClearExchanges();
        countryImpl.ClearFundamentals();
      }

      m_countries.Remove(country.Id);
      notifyModelObservers(ModelChangeType.Delete, country);
    }

    public void Delete(IHoliday holiday)
    {
      m_dataStore.DeleteHoliday(holiday.Id);

      if (holiday.Country is Country)
      {
        Country country = (Country)holiday.Country;
        country.Remove(holiday);
      }

      if (holiday is ExchangeHoliday)
      {
        ExchangeHoliday exchangeHoliday = (ExchangeHoliday)holiday;

        if (exchangeHoliday.Exchange is Exchange)
        {
          Exchange exchange = (Exchange)exchangeHoliday.Exchange;
          exchange.Remove(exchangeHoliday);
        }
      }

      m_holidays.Remove(holiday.Id);
      notifyModelObservers(ModelChangeType.Delete, holiday);
    }

    public void Delete(IExchange exchange)
    {
      m_dataStore.DeleteExchange(exchange.Id);

      foreach (var sessionsForDay in exchange.Sessions)
      {
        foreach (ISession session in sessionsForDay.Value) m_dataStore.DeleteSession(session.Id);
        sessionsForDay.Value.Clear();
      }

      if (exchange.Country is Country)
      {
        Country country = (Country)exchange.Country;
        country.Remove(exchange);
      }

      m_exchanges.Remove(exchange.Id);
      notifyModelObservers(ModelChangeType.Delete, exchange);
    }

    public void Delete(ISession session)
    {
      m_dataStore.DeleteSession(session.Id);

      if (session.Exchange is Exchange)
      {
        Exchange exchange = (Exchange)session.Exchange;
        exchange.Remove(session);
      }

      m_sessions.Remove(session.Id);
      notifyModelObservers(ModelChangeType.Delete, session);
    }

    public void Delete(IInstrumentGroup instrumentGroup)
    {
      //root instrument group can not be removed
      if (instrumentGroup == InstrumentGroupRoot) throw new ArgumentException(Common.Resources.InstrumentGroupRootNoRemove);

      //reassign the instrument group children to the root
      foreach (InstrumentGroup child in instrumentGroup.Children) m_dataStore.UpdateInstrumentGroup(child.Id, InstrumentGroupRoot.Id);

      //delete the instrument group
      m_dataStore.DeleteInstrumentGroup(instrumentGroup.Id);
      notifyModelObservers(ModelChangeType.Delete, instrumentGroup);
    }

    public void Delete(IInstrumentGroup parent, IInstrumentGroup child)
    {
      m_dataStore.DeleteInstrumentGroupChild(parent.Id, child.Id);

      if (parent is InstrumentGroup)
      {
        InstrumentGroup parentImpl = (InstrumentGroup)parent;
        parentImpl.Remove(child);
      }

      notifyModelObservers(ModelChangeType.Update, parent);
    }

    public void Delete(IInstrumentGroup instrumentGroup, IInstrument instrument)
    {
      m_dataStore.DeleteInstrumentGroupInstrument(instrumentGroup.Id, instrument.Id);

      if (instrumentGroup is InstrumentGroup)
      {
        InstrumentGroup instrumentGroupImpl = (InstrumentGroup)instrumentGroup;
        instrumentGroupImpl.Remove(instrument);
      }

      if (instrument is Instrument)
      {
        Instrument instrumentImpl = (Instrument)instrument;
        instrumentImpl.Remove(instrumentGroup);
      }

      notifyModelObservers(ModelChangeType.Update, instrumentGroup);
    }

    private void deleteInstrumentFromCaches(IInstrument instrument)
    {
      foreach (var observerKV in m_priceChangeObservers)
        if (observerKV.Value.TryGetTarget(out var observer) && observer is DataFeed)
        { 
          DataFeed dataFeed = (DataFeed)observer;
          if (dataFeed.Instrument == instrument)
          {
            PriceChange priceChange = new PriceChange();
            priceChange.ChangeType = PriceChangeType.Delete;
            priceChange.Instrument = instrument;
            priceChange.Resolution = dataFeed.Resolution;
            priceChange.From = DateTime.MinValue;
            priceChange.To = DateTime.MaxValue;

            m_priceChanges.Enqueue(priceChange);

            notifyPriceObservers();
          }
        }

      notifyModelObservers(ModelChangeType.Delete, instrument);
    }

    public void Delete(IInstrument instrument)
    {
      m_dataStore.DeleteInstrument(instrument.Id, instrument.Ticker);

      foreach (IExchange exchange in instrument.SecondaryExchanges)
        if (exchange is Exchange)
        {
          Exchange exchangeImpl = (Exchange)exchange;
          exchangeImpl.Remove(instrument);
        }

      if (instrument.PrimaryExchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)instrument.PrimaryExchange;
        exchangeImpl.Remove(instrument);
      }

      m_instruments.Remove(instrument.Id);
      deleteInstrumentFromCaches(instrument);
    }

    public void DeleteSecondaryExchange(IInstrument instrument, IExchange exchange)
    {
      m_dataStore.DeleteInstrumentFromExchange(instrument.Id, exchange.Id);

      if (instrument is Instrument)
      {
        Instrument instrumentImpl = (Instrument)instrument;
        instrumentImpl.SecondaryExchanges.Remove(exchange);
      }

      if (exchange is Exchange)
      {
        Exchange exchangeImpl = (Exchange)exchange;
        exchangeImpl.Remove(instrument);
      }

      notifyModelObservers(ModelChangeType.Update, instrument);
    }

    public void Delete(IFundamental fundamental)
    {
      m_dataStore.DeleteFundamental(fundamental.Id);
      m_fundamentals.Remove(fundamental.Id);
      notifyModelObservers(ModelChangeType.Delete, fundamental);
    }

    public void Delete(ICountryFundamental fundamental)
    {
      m_dataStore.DeleteCountryFundamental(m_dataProvider.Name, fundamental.FundamentalId, fundamental.CountryId);

      if (fundamental.Country is Country)
      {
        Country country = (Country)fundamental.Country;
        country.Remove(fundamental);
      }

      notifyModelObservers(ModelChangeType.Delete, fundamental);
    }

    public void Delete(IInstrumentFundamental fundamental)
    {
      m_dataStore.DeleteInstrumentFundamental(m_dataProvider.Name, fundamental.FundamentalId, fundamental.InstrumentId);

      if (fundamental.Instrument is Instrument)
      {
        Instrument instrument = (Instrument)fundamental.Instrument;
        instrument.Remove(fundamental);
      }

      notifyModelObservers(ModelChangeType.Delete, fundamental);
    }

    public void Delete(ICountryFundamental fundamental, DateTime dateTime)
    {
      m_dataStore.DeleteCountryFundamentalValue(m_dataProvider.Name, fundamental.FundamentalId, fundamental.CountryId, dateTime);

      if (fundamental is CountryFundamental)
      {
        CountryFundamental countryFundamental = (CountryFundamental)fundamental;
        countryFundamental.Values.Remove(dateTime);
      }

      notifyFundamentalObservers(FundamentalChangeType.Delete, fundamental, dateTime, 0);
    }

    public void Delete(IInstrumentFundamental fundamental, DateTime dateTime)
    {
      m_dataStore.DeleteInstrumentFundamentalValue(m_dataProvider.Name, fundamental.FundamentalId, fundamental.InstrumentId, dateTime);

      if (fundamental is InstrumentFundamental)
      {
        InstrumentFundamental instrumentFundamental = (InstrumentFundamental)fundamental;
        instrumentFundamental.Values.Remove(dateTime);
      }

      notifyFundamentalObservers(FundamentalChangeType.Delete, fundamental, dateTime, 0);
    }

    public void Delete(IInstrument instrument, Resolution resolution, DateTime dateTime, bool synthetic)
    {
      m_dataStore.DeleteData(m_dataProvider.Name, instrument.Ticker, resolution, dateTime, synthetic);

      PriceChange priceChange = new PriceChange();
      priceChange.ChangeType = PriceChangeType.Delete;
      priceChange.Instrument = instrument;
      priceChange.Resolution = resolution;
      priceChange.From = dateTime;
      priceChange.To = dateTime;

      m_priceChanges.Enqueue(priceChange);

      notifyPriceObservers();
    }

    public void Delete(IInstrument instrument, Resolution resolution, DateTime from, DateTime to, bool synthetic)
    {
      m_dataStore.DeleteData(m_dataProvider.Name, instrument.Ticker, resolution, from, to, synthetic);

      PriceChange priceChange = new PriceChange();
      priceChange.ChangeType = PriceChangeType.Delete;
      priceChange.Instrument = instrument;
      priceChange.Resolution = resolution;
      priceChange.From = from;
      priceChange.To = to;

      m_priceChanges.Enqueue(priceChange);

      notifyPriceObservers();
    }

    public DataFeed GetDataFeed(IInstrument instrument, Resolution resolution, int interval, DateTime from, DateTime to, ToDateMode toDateMode, PriceDataType priceDataType)
    {
      foreach (var observerKV in m_priceChangeObservers)
        if (observerKV.Value.TryGetTarget(out var observer))
        {
          if (observer is DataFeed)
          {
            DataFeed dataFeed = (DataFeed)observer;
            if (instrument == dataFeed.Instrument &&
                from == dataFeed.From && to == dataFeed.To &&   //TODO: Currently we require an exact match, an additional view object could be used to allow for a partial match.
                dataFeed.ToDateMode == toDateMode &&
                resolution == dataFeed.Resolution && interval == dataFeed.Interval)
              return dataFeed;
          }
        }
        else
          m_priceChangeObservers.TryRemove(observerKV.Key, out _);  //observer no longer reachable through weak reference, remove it from the dictionary

      DataFeed newDataFeed = new DataFeed(m_dataStore, this, instrument, resolution, interval, from, to, toDateMode, priceDataType);
      Subscribe(newDataFeed);

      return newDataFeed;
    }

    //properties
    public IConfigurationService Configuration { get { return m_configuration; } }
    public IList<ICountry> Countries { get { refreshModel(); return m_countries.Values.ToList<ICountry>(); } }
    public IList<IExchange> Exchanges { get { refreshModel(); return m_exchanges.Values.ToList<IExchange>(); } }
    public IList<IInstrumentGroup> InstrumentGroups { get { refreshModel(); return m_instrumentGroup.Values.ToList<IInstrumentGroup>(); } }
    public IList<IInstrument> Instruments { get { refreshModel(); return m_instruments.Values.ToList<IInstrument>(); } }
    public IList<IFundamental> Fundamentals { get { refreshModel(); return m_fundamentals.Values.ToList<IFundamental>(); } }
    public IDataProvider DataProvider { get => m_dataProvider; set { m_dataProvider = value; Refresh(); /* TODO Notify observers of model update since ALL data would change. */ } }
    public IList<IDataProvider> DataProviders { get => m_dataProviders; }
    public IInstrumentGroup InstrumentGroupRoot => Data.InstrumentGroupRoot.Instance;
    public IDictionary<int, WeakReference<Common.IObserver<ModelChange>>> ModelChangeObservers { get => m_modelChangeObservers; }
    public IDictionary<int, WeakReference<Common.IObserver<FundamentalChange>>> FundamentalChangeObservers { get => m_fundamentalChangeObservers; }
    public IDictionary<int, WeakReference<Common.IObserver<PriceChange>>> PriceChangeObservers { get => m_priceChangeObservers; }

    //methods
    public void Refresh()
    {
      m_refreshModel = true;
      refreshModel();
    }

    protected void refreshModel()
    {
      //TBD: This code could be more optimal but it would be much more complex and updating of the model will only occur when it is initially created.
      //TBD: Should we perform change notifications here.
      if (!m_refreshModel) return;

      //get relevant data from the data store
      IList<IDataStoreService.Country> countries = m_dataStore.GetCountries();
      IList<IDataStoreService.Holiday> holidays = m_dataStore.GetHolidays();
      IList<IDataStoreService.Exchange> exchanges = m_dataStore.GetExchanges();
      IList<IDataStoreService.Session> sessions = m_dataStore.GetSessions();
      IList<IDataStoreService.InstrumentGroup> instrumentGroups = m_dataStore.GetInstrumentGroups();
      IList<IDataStoreService.Instrument> instruments = m_dataStore.GetInstruments();
      IList<IDataStoreService.Fundamental> fundamentals = m_dataStore.GetFundamentals();
      IList<IDataStoreService.CountryFundamental> countryFundamentals = m_dataStore.GetCountryFundamentals(m_dataProvider.Name);
      IList<IDataStoreService.InstrumentFundamental> instrumentFundamentals = m_dataStore.GetInstrumentFundamentals(m_dataProvider.Name);

      //create all the objects
      m_countries.Clear();
      m_holidays.Clear();
      m_exchanges.Clear();
      m_sessions.Clear();
      m_instruments.Clear();
      m_fundamentals.Clear();
      m_instrumentGroup.Clear();

      foreach (IDataStoreService.Country country in countries) m_countries.Add(country.Id, new Country(m_dataStore, this, country));
      foreach (IDataStoreService.Exchange exchange in exchanges) m_exchanges.Add(exchange.Id, new Exchange(m_dataStore, this, exchange));
      foreach (IDataStoreService.Holiday holiday in holidays)
      {
        if (m_countries.ContainsKey(holiday.ParentId))
          m_holidays.Add(holiday.Id, new Holiday(m_dataStore, this, holiday));
        else
          m_holidays.Add(holiday.Id, new ExchangeHoliday(m_dataStore, this, holiday));
      }

      foreach (IDataStoreService.Session session in sessions) m_sessions.Add(session.Id, new Session(m_dataStore, this, session));
      foreach (IDataStoreService.InstrumentGroup instrumentGroup in instrumentGroups) m_instrumentGroup.Add(instrumentGroup.Id, new InstrumentGroup(m_dataStore, this, instrumentGroup));
      foreach (IDataStoreService.Fundamental fundamental in fundamentals) m_fundamentals.Add(fundamental.Id, new Fundamental(m_dataStore, this, fundamental));
      foreach (IDataStoreService.Instrument instument in instruments) m_instruments.Add(instument.Id, new Instrument(m_dataStore, this, instument));

      //setup model relationships between objects
      //link exchanges to countries
      foreach (IDataStoreService.Exchange cexchange in exchanges)
      {
        if (m_countries.TryGetValue(cexchange.CountryId, out Country? country) && m_exchanges.TryGetValue(cexchange.Id, out Exchange? exchange))
        {
          exchange.Country = country;
          country.Add(exchange);
        }
        else m_logger.LogError($"Failed to find country ({cexchange.CountryId}) or exchange ({cexchange.Id}) to setup country/exchange relationship.");
      }

      //link sessions to exchanges  
      foreach (IDataStoreService.Session csession in sessions)
      {
        if (m_exchanges.TryGetValue(csession.ExchangeId, out Exchange? exchange) && m_sessions.TryGetValue(csession.Id, out Session? session))
        {
          session.Exchange = exchange;
          exchange.Add(session);
        }
        else m_logger.LogError($"Failed to find exchange ({csession.ExchangeId}) or session ({csession.Id}) to setup exchange/session relationship.");
      }

      //link holidays to countries and exchanges - this needs to happen after loading the countries and exchanges!!!
      foreach (IDataStoreService.Holiday choliday in holidays)
      {
        Country? country;
        Exchange? exchange;
        Holiday? holiday;
        m_holidays.TryGetValue(choliday.Id, out holiday);
        m_countries.TryGetValue(choliday.ParentId, out country);
        m_exchanges.TryGetValue(choliday.ParentId, out exchange);

        if (holiday != null)
        {
          if (country != null)
          {
            holiday.Country = country;
            country.Add(holiday);
          }
          else if (exchange != null)
          {
            ExchangeHoliday exchangeHoliday = (ExchangeHoliday)holiday!;
            exchangeHoliday.Country = exchange.Country;
            exchangeHoliday.Exchange = exchange;
            exchange.Add(exchangeHoliday);
          }
          else m_logger.LogError($"Holiday {choliday.Name} ({choliday.Id}) parent {choliday.ParentId} not found.");
        }
        else m_logger.LogError($"Holiday {choliday.Name} ({choliday.Id}) not found.");
      }

      //link instrument groups and instruments in the groups
      foreach (IDataStoreService.InstrumentGroup cinstrumentGroup in instrumentGroups)
      {
        if (m_instrumentGroup.TryGetValue(cinstrumentGroup.Id, out InstrumentGroup? instrumentGroup))
        {
          if (InstrumentGroupRoot.Id != cinstrumentGroup.ParentId)
            if (m_instrumentGroup.TryGetValue(cinstrumentGroup.ParentId, out InstrumentGroup? parentGroup))
            {
              instrumentGroup.Parent = parentGroup;
              parentGroup.Add(instrumentGroup);
            }
            else m_logger.LogError($"Failed to find parent instrument group ({cinstrumentGroup.ParentId}) for instrument group ({cinstrumentGroup.Id} - {instrumentGroup.Name}).");

          //link instruments contained in groups          
          foreach (Guid instrumentId in cinstrumentGroup.Instruments)
            if (m_instruments.TryGetValue(instrumentId, out Instrument? instrument))
            {
              instrumentGroup.Add(instrument);
              instrument.Add(instrumentGroup);
            }
            else
              m_logger.LogError($"Failed to find instrument ({instrumentId}) to add to instrument group ({cinstrumentGroup.Id} - {instrumentGroup.Name}).");
        }
      }

      //link in primary/secondary exchanges into the instrument
      foreach (IDataStoreService.Instrument cinstrument in instruments)
      {
        if (m_instruments.TryGetValue(cinstrument.Id, out Instrument? instrument))
        {
          if (m_exchanges.TryGetValue(cinstrument.PrimaryExchangeId, out Exchange? primaryExchange))
          {
            instrument.PrimaryExchange = primaryExchange;
            primaryExchange.Add(instrument);
          }
          else m_logger.LogError($"Failed to find exchange ({cinstrument.PrimaryExchangeId}) for instrument ({cinstrument.Id} - {instrument.Name}).");

          foreach (Guid secondaryExchangeId in cinstrument.SecondaryExchangeIds)
            if (m_exchanges.TryGetValue(secondaryExchangeId, out Exchange? secondaryExchange))
            {
              instrument.SecondaryExchanges.Add(secondaryExchange);
              secondaryExchange.Add(instrument);
            }
            else m_logger.LogError($"Failed to find exchange ({secondaryExchangeId}) for instrument ({cinstrument.Id} - {instrument.Name}).");
        }
        else m_logger.LogError($"Failed to find instrument ({cinstrument.Id} - {cinstrument.Name}).");
      }

      //link in fundamental factors for countries and instruments
      foreach (IDataStoreService.CountryFundamental scountryFundamental in countryFundamentals)
      {
        if (m_fundamentals.TryGetValue(scountryFundamental.FundamentalId, out Fundamental? fundamental) &&
            m_countries.TryGetValue(scountryFundamental.CountryId, out Country? country))
        {
          CountryFundamental countryFundamental = new CountryFundamental(fundamental, country);
          foreach (Tuple<DateTime, double> value in scountryFundamental.Values) countryFundamental.Add(value.Item1, (decimal)value.Item2);
          country.Add(countryFundamental);
        }
        else m_logger.LogWarning($"Failed to find country ({scountryFundamental.CountryId}) or fundamental ({scountryFundamental.FundamentalId}), skipping value.");
      }

      foreach (IDataStoreService.InstrumentFundamental sinstrumentFundamental in instrumentFundamentals)
      {
        if (m_fundamentals.TryGetValue(sinstrumentFundamental.FundamentalId, out Fundamental? fundamental) &&
            m_instruments.TryGetValue(sinstrumentFundamental.InstrumentId, out Instrument? instrument))
        {
          InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, instrument);
          foreach (Tuple<DateTime, double> value in sinstrumentFundamental.Values) instrumentFundamental.Add(value.Item1, (decimal)value.Item2);
          instrument.Add(instrumentFundamental);
        }
        else m_logger.LogWarning($"Failed to find instrument ({sinstrumentFundamental.InstrumentId}) or fundamental ({sinstrumentFundamental.FundamentalId}), skipping value.");
      }

      //clear refresh
      m_refreshModel = false;
    }

    /// <summary>
    /// Add model change and notify observers if appropriate.
    /// </summary>
    public void notifyModelObservers(ModelChangeType changeType, object changedObject)
    {
      ModelChange modelChange = new ModelChange();
      modelChange.ChangeType = changeType;
      modelChange.Object = changedObject;
      
      m_modelChanges.Enqueue(modelChange);

      notifyModelObservers();
    }

    /// <summary>
    /// Notify model change observers if appropriate and clear model change queue. Also prunes any weak references where the target is no longer available.
    /// </summary>
    public void notifyModelObservers(long? modelChangePauseCount = null)
    {
      if ((modelChangePauseCount.HasValue && modelChangePauseCount == 0) || Interlocked.Read(ref m_modelChangePauseCount) == 0)
      {
        foreach (var observerKV in m_modelChangeObservers)        
           if (observerKV.Value.TryGetTarget(out var observer)) 
             observer.OnChange(m_modelChanges);
           else
             m_modelChangeObservers.TryRemove(observerKV.Key, out _);
        m_modelChanges.Clear();
      }
    }  

    /// <summary>
    /// Add fundamental change and notify observers if appropriate.
    /// </summary>
    public void notifyFundamentalObservers(FundamentalChangeType fundamentalChangeType, IFundamentalValues changedObject, DateTime dateTime, double value)
    {
      FundamentalChange fundamentalChange = new FundamentalChange();
      fundamentalChange.ChangeType = fundamentalChangeType;
      fundamentalChange.FundamentalValues = changedObject;
      fundamentalChange.DateTime = dateTime;
      fundamentalChange.Value = value;
      
      m_fundamentalChanges.Enqueue(fundamentalChange);

      notifyFundamentalObservers();      
    }

    /// <summary>
    /// Notify fundamental change observers if appropriate and clear fundamental change queue.
    /// </summary>
    public void notifyFundamentalObservers(long? fundamentalChangePauseCount = null)
    {
      if ((fundamentalChangePauseCount.HasValue && fundamentalChangePauseCount == 0) || Interlocked.Read(ref m_fundamentalChangePauseCount) == 0)
      {
        foreach (var observerKV in m_fundamentalChangeObservers)
          if (observerKV.Value.TryGetTarget(out var observer))
            observer.OnChange(m_fundamentalChanges);
          else
            m_fundamentalChangeObservers.TryRemove(observerKV.Key, out _);
        m_fundamentalChanges.Clear();
      }
    }

    /// <summary>
    /// Notify price change observers if appropriate and clear price change queue. Also prunes any weak references where the target is no longer available.
    /// </summary>
    public void notifyPriceObservers(long? priceChangePauseCount = null)
    {
      if ((priceChangePauseCount.HasValue && priceChangePauseCount == 0) || Interlocked.Read(ref m_priceChangePauseCount) == 0)
      {
        foreach (var observerKV in m_priceChangeObservers)
          if (observerKV.Value.TryGetTarget(out var observer))
            observer.OnChange(m_priceChanges);
          else
            m_priceChangeObservers.TryRemove(observerKV.Key, out _);
        m_priceChanges.Clear();
      }
    }

  }
}
