using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.Data
{

  public enum PriceDataType
  {
    Actual,
    Synthetic,
    Both
  }


  /// <summary>
  /// Interface to be implemented by objects used to store data. The general design is a Bridge pattern where the implementing class
  /// would serve as a bridge between a DataManager and an underlying storage technology like a database. The API for the data store
  /// focusses on the structure of the underlying data while the data manager layer would add object oriented functionality with the
  /// necessary locking to ensure data consistency.
  /// 
  /// The data store should also take into account specific configuration settings related to data, e.g. data related to timezones
  /// should be correctly stored in UTC time and then returned using the user's desired TimeZone from the IConfiguration interface.
  /// </summary>
  public interface IDataStoreService : IDisposable
  {
    //constants


    //enums


    //types
    /// <summary>
    /// Language specific texts, all ISO language codes are three letter codes.
    /// </summary>
    public struct Text
    {
      public Text(Guid id, string isoLang, string value)
      {
        Id = id;
        IsoLang = isoLang;
        Value = value;
      }

      public Guid Id { get; }
      public string IsoLang { get; }
      public string Value { get; }
    }

    /// <summary>
    /// Storage class for country data.
    /// </summary>
    public struct Country
    {
      public Country(Guid id, string isoCode)
      {
        Id = id;
        IsoCode = isoCode;
      }

      public Guid Id { get; }
      public string IsoCode { get; }
    }

    /// <summary>
    /// Storage class for country/exchange holidays.
    /// </summary>
    public struct Holiday
    {
      public Holiday(Guid id, Guid parentId, Guid nameTextId, string name, HolidayType type, Months month, int dayOfMonth, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday)
      {
        Type = type;
        Id = id;
        ParentId = parentId;
        NameTextId = nameTextId;
        Name = name;
        Month = month;
        DayOfMonth = dayOfMonth;
        DayOfWeek = dayOfWeek;
        WeekOfMonth = weekOfMonth;
        MoveWeekendHoliday = moveWeekendHoliday;
      }

      public Guid Id { get; }
      public Guid ParentId { get; }
      public HolidayType Type { get; }
      public Guid NameTextId { get; }
      public string Name { get; }
      public Months Month { get; }
      public int DayOfMonth { get; }
      public DayOfWeek DayOfWeek { get; }
      public WeekOfMonth WeekOfMonth { get; }
      public MoveWeekendHoliday MoveWeekendHoliday { get; }
    }

    /// <summary>
    /// Storage class for exchange data.
    /// </summary>
    public struct Exchange
    {
      public Exchange(Guid id, Guid countryId, Guid nameTextId, string name, TimeZoneInfo timeZone)
      {
        Id = id;
        CountryId = countryId;
        NameTextId = nameTextId;
        Name = name;
        TimeZone = timeZone;
      }

      public Guid Id { get; }
      public Guid CountryId { get; }
      public Guid NameTextId { get; }
      public string Name { get; }
      public TimeZoneInfo TimeZone { get; }
    }

    /// <summary>
    /// Storage class for exchange session data.
    /// </summary>
    public struct Session
    {
      public Session(Guid id, Guid nameTextId, string name, Guid exchangeId, DayOfWeek dayOfWeek, TimeOnly start, TimeOnly end)
      {
        Id = id;
        NameTextId = nameTextId;
        Name = name;
        ExchangeId = exchangeId;
        DayOfWeek = dayOfWeek;
        Start = start;
        End = end;
      }

      public Guid Id { get; }
      public Guid NameTextId { get; }
      public string Name { get; }
      public Guid ExchangeId { get; }
      public DayOfWeek DayOfWeek { get; }
      public TimeOnly Start { get; }
      public TimeOnly End { get; }
    }

    /// <summary>
    /// Storage base class for instrument data.
    /// </summary>
    public struct Instrument
    {
      public Instrument(Guid id, InstrumentType type, string ticker, Guid nameTextId, string name, Guid descriptionTextId, string description, DateTime inceptionDate, IList<Guid> instrumentGroupId, Guid primaryExhangeId, IList<Guid> secondaryExchangeIds)
      {
        Id = id;
        Type = type;
        Ticker = ticker;
        NameTextId = nameTextId;
        Name = name;
        DescriptionTextId = descriptionTextId;
        Description = description;
        InceptionDate = inceptionDate;
        InstrumentGroupIds = instrumentGroupId;
        PrimaryExchangeId = primaryExhangeId;
        SecondaryExchangeIds = secondaryExchangeIds;
      }

      public Guid Id { get; }
      public InstrumentType Type { get; }
      public string Ticker { get; }
      public Guid NameTextId { get; }
      public string Name { get; }
      public Guid DescriptionTextId { get; }
      public string Description { get; }
      public DateTime InceptionDate { get; }
      public IList<Guid> InstrumentGroupIds { get; }
      public Guid PrimaryExchangeId { get; }
      public IList<Guid> SecondaryExchangeIds { get; }
    }

    /// <summary>
    /// Storage class for general instrument grouping definition.
    /// </summary>
    public struct InstrumentGroup
    {
      public InstrumentGroup(Guid id, Guid parentId, Guid nameTextId, string name, Guid descriptionTextId, string description, IList<Guid> instruments)
      {
        Id = id;
        ParentId = parentId;
        NameTextId = nameTextId;
        Name = name;
        DescriptionTextId = descriptionTextId;
        Description = description;
        Instruments = instruments;
      }

      public Guid Id { get; }
      public Guid ParentId { get; }
      public Guid NameTextId { get; }
      public string Name { get; }
      public Guid DescriptionTextId { get; }
      public string Description { get; }
      public IList<Guid> Instruments { get; }
    }

    /// <summary>
    /// Storage structure for fundamental definitions.
    /// </summary>
    public struct Fundamental
    {
      public Fundamental(Guid id, Guid nameTextId, string name, Guid descriptionTextId, string description, FundamentalCategory category, FundamentalReleaseInterval releaseInterval)
      {
        Id = id;
        Name = name;
        Description = description;
        NameTextId = nameTextId;
        DescriptionTextId = descriptionTextId;
        Category = category;
        ReleaseInterval = releaseInterval;
      }

      public Guid Id { get; }
      public string Name { get; }
      public Guid NameTextId { get; }
      public string Description { get; }
      public Guid DescriptionTextId { get; }
      public FundamentalCategory Category { get; }
      public FundamentalReleaseInterval ReleaseInterval { get; }
    }

    /// <summary>
    /// Storage structure for country fundamental values.
    /// </summary>
    public struct CountryFundamental
    {
      public CountryFundamental(string dataProviderName, Guid associationId, Guid fundamentalId, Guid countryId)
      {
        DataProviderName = dataProviderName;
        AssociationId = associationId;
        FundamentalId = fundamentalId;
        CountryId = countryId;
        Values = new List<Tuple<DateTime, double>>();
      }

      public CountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId) : this(dataProviderName, Guid.Empty, fundamentalId, countryId) { }

      public string DataProviderName { get; }
      public Guid AssociationId { get; set; }
      public Guid FundamentalId { get; }
      public Guid CountryId { get; }
      public IList<Tuple<DateTime, double>> Values { get; }

      public void AddValue(DateTime dateTime, double value) { Values.Add(new Tuple<DateTime, double>(dateTime, value)); }
    }

    /// <summary>
    /// Storage structure for instrument fundamental values.
    /// </summary>
    public struct InstrumentFundamental
    {
      public InstrumentFundamental(string dataProviderName, Guid associationId, Guid fundamentalId, Guid instrumentId)
      {
        DataProviderName = dataProviderName;
        AssociationId = associationId;
        FundamentalId = fundamentalId;
        InstrumentId = instrumentId;
        Values = new List<Tuple<DateTime, double>>();
      }

      public InstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId) : this(dataProviderName, Guid.Empty, fundamentalId, instrumentId) { }

      public string DataProviderName { get; }
      public Guid AssociationId { get; set; }
      public Guid FundamentalId { get; }
      public Guid InstrumentId { get; }
      public IList<Tuple<DateTime, double>> Values { get; }

      public void AddValue(DateTime dateTime, double value) { Values.Add(new Tuple<DateTime, double>(dateTime, value)); }
    }

    /// <summary>
    /// Represents the data cache around price bar data.
    /// </summary>
    public struct BarData
    {
      public BarData(int count)
      {
        Count = count;
        DateTime = new DateTime[count];
        Open = new double[count];
        High = new double[count];
        Low = new double[count];
        Close = new double[count];
        Volume = new long[count];
        Synthetic = new bool[count];
      }

      public int Count;
      public IList<DateTime> DateTime;
      public IList<double> Open;
      public IList<double> High;
      public IList<double> Low;
      public IList<double> Close;
      public IList<long> Volume;
      public IList<bool> Synthetic;
    }

    /// <summary>
    /// Represents the data cache around level 1 market data (tick data).
    /// NOTE: The underlying List type could become a problem if huge amounts of tick data is processed per instrument, in that
    ///       case the underlying storage structure would need to be adjusted - would probably need some custom memory implementation
    ///       that implements the IList interface.
    /// </summary>
    public struct Level1Data

    {
      public Level1Data(int count)
      {
        Count = count;
        DateTime = new DateTime[count];
        Bid = new double[count];
        BidSize = new long[count];
        Ask = new double[count];
        AskSize = new long[count];
        Last = new double[count];
        LastSize = new long[count];
        Synthetic = new bool[count];
      }

      public int Count;
      public IList<DateTime> DateTime;
      public IList<double> Bid;
      public IList<long> BidSize;
      public IList<double> Ask;
      public IList<long> AskSize;
      public IList<double> Last;
      public IList<long> LastSize;
      public IList<bool> Synthetic;
    }

    /// <summary>
    /// Generic cache entry used by the DataManager to store loaded/cached data.
    /// </summary>
    public struct DataCache
    {
      public DataCache(string dataProviderName, Guid instrumentId, Resolution resolution, PriceDataType priceDataType, DateTime from, DateTime to, int count)
      {
        DataProviderName = dataProviderName;
        InstrumentId = instrumentId;
        Resolution = resolution;
        PriceDataType = priceDataType;
        From = from;
        To = to;
        Count = count;
        Resolution = resolution;
        Data = default!;  //null forgiving, will create data below

        switch (Resolution)
        {
          //candlestick data
          case Resolution.Minute:
          case Resolution.Hour:
          case Resolution.Day:
          case Resolution.Week:
          case Resolution.Month:
            Data = new BarData(count);
            break;
          //tick data
          case Resolution.Level1:
            Data = new Level1Data(count);
            break;
        }
      }

      public string DataProviderName { get; }
      public Guid InstrumentId { get; }
      public Resolution Resolution { get; }
      public PriceDataType PriceDataType { get; }
      public DateTime From { get; }
      public DateTime To { get; }
      public int Count { get; set; }
      public object Data { get; set; } //data stored based on the Resolution field
    }

    //attributes


    //properties
    public IList<Resolution> SupportedDataResolutions { get; }

    /// <summary>
    /// Start a transaction if the data store supports transactional updates.
    /// </summary>
    void StartTransaction();

    /// <summary>
    /// End a transaction if the data store supports transactional updates.
    /// </summary>
    void EndTransaction(bool success);

    /// <summary>
    /// Creates a specific text in a given language and returns the guid identifier to identify it uniquely.
    /// </summary>
    public Guid CreateText(string isoLang, string text);

    /// <summary>
    /// Create/update a new country definition.
    /// </summary>
    public void CreateCountry(Country country);

    /// <summary>
    /// Create/update a holiday associated with a given country based on a very specific day within a given month. 
    /// </summary>
    public void CreateHoliday(Holiday holiday);

    /// <summary>
    /// Create/update an PrimaryExchange definition within a given timezone.
    /// </summary>
    public void CreateExchange(Exchange exchange);

    /// <summary>
    /// Create/update a trading session definition on a given PrimaryExchange.
    /// </summary>
    public void CreateSession(Session session);

    /// <summary>
    /// Create an instrument group definition.
    /// </summary>
    public void CreateInstrumentGroup(InstrumentGroup instrumentGroup);

    /// <summary>
    /// Create/update an instrument being traded on a given PrimaryExchange (primary PrimaryExchange).
    /// </summary>
    public void CreateInstrument(Instrument instrument);

    /// <summary>
    /// Create/update an instrument to be listed on a secondary PrimaryExchange.
    /// </summary>
    public void CreateInstrument(Guid instrument, Guid exchange);

    /// <summary>
    /// Create/update a fundamental factor that can be used to measure the performance of a country/instrument at a given interval.
    /// </summary>
    public void CreateFundamental(Fundamental fundamental);

    /// <summary>
    /// Associate a given fundamental with a given country or instrument.
    /// </summary>
    public void CreateCountryFundamental(ref CountryFundamental fundamental);
    public void CreateInstrumentFundamental(ref InstrumentFundamental fundamental);

    /// <summary>
    /// Add the given instrument to the instrument group.
    /// </summary>
    public void CreateInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId);

    /// <summary>
    /// Create/update a text entry in the database for a given language text. 
    /// </summary>
    public void UpdateText(Guid id, string isoLang, string value);

    /// <summary>
    /// Updates the session information.
    /// </summary>
    public void UpdateSession(Guid id, DayOfWeek day, TimeOnly start, TimeOnly end);

    /// <summary>
    /// Updates the instrument information.
    /// </summary>
    public void UpdateInstrument(Guid id, Guid exchangeId, string ticker, DateTime inceptionDate);

    /// <summary>
    /// Create/update a fundamental factor value from a given DataProvider on a given date.
    /// </summary>
    public void UpdateCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime, double value);
    public void UpdateInstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime, double value);

    /// <summary>
    /// Update an instrument group definition.
    /// </summary>
    public void UpdateInstrumentGroup(Guid id, Guid parentId);
    public void UpdateInstrumentGroup(Guid id, string name, string description);

    /// <summary>
    /// Create/update price data from a given DataProvider for a given instrument. Synthetic data that was generated can be set for a given instrument as well in the event that the specific
    /// DataProvider has missing intervals of data.
    /// </summary>
    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume, bool synthetic);
    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, BarData bars);
    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Level1Data bars);

    /// <summary>
    /// Delete text entries for a specific Id and optionally a specific langauge.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteText(Guid id);
    public int DeleteText(Guid id, string isoLang);

    /// <summary>
    /// Delete a country definition.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteCountry(Guid id);

    /// <summary>
    /// Delete a holiday.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteHoliday(Guid id);

    /// <summary>
    /// Delete an exchange.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteExchange(Guid id);

    /// <summary>
    /// Delete a session from an exchange.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteSession(Guid id);

    /// <summary>
    /// Delete an instrument/stock/Forex definition and it's associated data.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteInstrument(Guid id, string ticker);

    /// <summary>
    /// Delete an instrument from a secondary PrimaryExchange.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteInstrumentFromExchange(Guid instrumentId, Guid exchangeId);

    /// <summary>
    /// Delete an instrument group definition.
    /// </summary>
    public void DeleteInstrumentGroup(Guid id);

    /// <summary>
    /// Remove child instrument group from a given parent instrument group.
    /// </summary>
    public void DeleteInstrumentGroupChild(Guid parentId, Guid childId);

    /// <summary>
    /// Delete the given instrument from the instrument group.
    /// </summary>
    public void DeleteInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId);

    /// <summary>
    /// Delete a fundamental defintion.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteFundamental(Guid id);

    /// <summary>
    /// Delete only the fundamental values associated with a given fundamental either for all the data providers or for a specific data provider.
    /// </summary>
    public int DeleteFundamentalValues(Guid id);
    public int DeleteFundamentalValues(string dataProviderName, Guid id);

    /// <summary>
    /// Delete the fundamental values for a given fundamental and a given country/instrument.
    /// </summary>
    public int DeleteCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId);
    public int DeleteInstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId);

    /// <summary>
    /// Delete a fundamental value for a specific data provider, given date and given country/instrument.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteCountryFundamentalValue(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime);
    public int DeleteInstrumentFundamentalValue(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime);

    /// <summary>
    /// Delete a specific price data.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime dateTime, bool? synthetic = null);

    /// <summary>
    /// Delete a set of price data entries.
    /// </summary>
    /// <returns>Number of data rows/entries removed</returns>
    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution = null, DateTime? from = null, DateTime? to = null, bool? synthetic = null);

    /// <summary>
    /// Get language texts.
    /// </summary>
    public IList<Text> GetTexts();
    public IList<Text> GetTexts(string isoLang);
    public string GetText(Guid id);
    public string GetText(Guid id, string isoLang);

    /// <summary>
    /// Returns the set of defined countries.
    /// </summary>
    public IList<Country> GetCountries();

    /// <summary>
    /// Returns country and exchange holidays.
    /// </summary>
    public IList<Holiday> GetHolidays();

    /// <summary>
    /// Returns exchange definitions.
    /// </summary>
    public IList<Exchange> GetExchanges();

    /// <summary>
    /// Returns exchange session data.
    /// </summary>
    public IList<Session> GetSessions();

    /// <summary>
    /// Returns instrument related data, overload to return instruments of a specific type.
    /// </summary>
    public IList<Instrument> GetInstruments();
    public IList<Instrument> GetInstruments(InstrumentType instrumentType);

    /// <summary>
    /// Returns country and company related fundamental definitions and values.
    /// </summary>
    public IList<Fundamental> GetFundamentals();
    public IList<CountryFundamental> GetCountryFundamentals(string dataProviderName);
    public IList<InstrumentFundamental> GetInstrumentFundamentals(string dataProviderName);

    /// <summary>
    /// Returns the set of defined instrument groups.
    /// </summary>
    public IList<InstrumentGroup> GetInstrumentGroups();

    /// <summary>
    /// Returns the set of instruments assigned to an instrument group.
    /// </summary>
    public IList<Guid> GetInstrumentGroupInstruments(Guid instrumentGroupId);

    /// <summary>
    /// Get the bar data from a given data providers tables.
    /// </summary>
    public DataCache GetInstrumentData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, DateTime from, DateTime to, PriceDataType priceDataType);

  }
}