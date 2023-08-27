using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for the data manager that runs on top of a data store that acts as the storage mechanism. In general objects are immutable and are only mutable on the
  /// data manager layer that would reflect the changes back to the objects. The reason for this is to support easy multithreading and locking only on the data manager
  /// and data store layer.
  /// </summary>
  public interface IDataManagerService : Common.IObservable<ModelChange>, Common.IObservable<FundamentalChange>, Common.IObservable<PriceChange>, IDisposable
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public IConfigurationService Configuration { get; }
    public IList<ICountry> Countries { get; }
    public IList<IExchange> Exchanges { get; }
    public IList<IInstrumentGroup> InstrumentGroups { get; }
    public IDataProvider DataProvider { get; set; }   //get/set the active data provider used by the data manager
    public IList<IDataProvider> DataProviders { get; }
    public IList<IInstrument> Instruments { get; }
    public IList<IFundamental> Fundamentals { get; }
    //public IDataStoreService DataStore { get; }
    public IInstrumentGroup InstrumentGroupRoot { get; }

    //methods
    /// <summary>
    /// Pause/resume observer notifications.
    /// </summary>
    public long PauseModelChangeNotifications();
    public long ResumeModelChangeNotifications();
    public long PauseFundamentalChangeNotifications();
    public long ResumeFundamentalChangeNotifications();
    public long PausePriceChangeNotifications();
    public long ResumePriceChangeNotifications();

    /// <summary>
    /// Create a new country definition.
    /// </summary>
    public ICountry Create(string isoCode);

    /// <summary>
    /// Create a holiday associated with a given country based on a very specific day within a given month.
    /// </summary>
    public IHoliday Create(ICountry country, string name, Months month, int dayOfMonth, MoveWeekendHoliday moveWeekendHoliday);

    /// <summary>
    /// Create a holiday associated with a given country based on a very specific day and week within a given month.
    /// </summary>
    public IHoliday Create(ICountry country, string name, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday);

    /// <summary>
    /// Create an PrimaryExchange definition within a given timezone.
    /// </summary>
    public IExchange Create(ICountry country, string name, TimeZoneInfo timeZone);

    /// <summary>
    /// Create a holiday associated with a given PrimaryExchange based on a very specific day within a given month.
    /// </summary>
    public IHoliday Create(IExchange exchange, string name, Months month, int dayOfMonth, MoveWeekendHoliday moveWeekendHoliday);

    /// <summary>
    /// Create a holiday associated with a given PrimaryExchange based on a very specific day and week within a given month.
    /// </summary>
    public IHoliday Create(IExchange exchange, string name, Months month, DayOfWeek dayOfWeek, WeekOfMonth weekOfMonth, MoveWeekendHoliday moveWeekendHoliday);

    /// <summary>
    /// Create a trading session definition on a given PrimaryExchange.
    /// </summary>
    public ISession Create(IExchange exchange, DayOfWeek day, string name, TimeOnly start, TimeOnly end);

    /// <summary>
    /// Create a fundamental factor defintion associated with countries/instruments that can be used to measure the performance of a country/instrument at a given interval.
    /// </summary>
    public IFundamental Create(string name, string description, FundamentalCategory category, FundamentalReleaseInterval releaseInterval);

    /// <summary>
    /// Create fundamental association with country to store values and release dates for the country fundamentals.
    /// </summary>
    public ICountryFundamental Create(IFundamental fundamental, ICountry country);

    /// <summary>
    /// Create fundamental association with instrument to store values and release dates for the instrument fundamentals.
    /// </summary>
    public IInstrumentFundamental Create(IFundamental fundamental, IInstrument instrument);

    /// <summary>
    /// Create an instrument group.
    /// </summary>
    public IInstrumentGroup Create(string name, string description, IInstrumentGroup? parent = null);

    /// <summary>
    /// Create an instrument being traded on a given PrimaryExchange (primary PrimaryExchange).
    /// </summary>
    public IInstrument Create(IExchange exchange, InstrumentType type, string ticker, string name, string description, DateTime inceptionDate);

    /// <summary>
    /// Creates a link between the given instrument and the exchange as a secondary exchange.
    /// </summary>
    public void CreateSecondaryExchange(IInstrument instrument, IExchange exchange);

    /// <summary>
    /// Update the name or description text for an object either in the current language or in some other given language to support translation.
    /// </summary>
    public void Update(IName namedObject, string name);
    public void Update(IName namedObject, string name, CultureInfo culture);
    public void Update(IDescription descriptionObject, string description);
    public void Update(IDescription descriptionObject, string description, CultureInfo culture);

    /// <summary>
    /// Update session object attributes.
    /// </summary>
    public void Update(ISession session, DayOfWeek day, TimeOnly start, TimeOnly end);

    /// <summary>
    /// Update instrument object attributes.
    /// </summary>
    public void Update(IInstrument instrument, Guid primaryExchangeId, string ticker, DateTime inceptionDate);

    /// <summary>
    /// Add/Updates the parent of a specific instrument group.
    /// </summary>
    public void Update(IInstrumentGroup instrumentGroup, IInstrumentGroup parent);

    /// <summary>
    /// Add given instrument to the instrument group.
    /// </summary>
    public void Update(IInstrumentGroup instrumentGroup, IInstrument instrument);

    /// <summary>
    /// Update a fundamental factor value from a given DataProvider on a given date.
    /// </summary>
    public void Update(ICountryFundamental fundamental, DateTime dateTime, double value);
    public void Update(IInstrumentFundamental fundamental, DateTime dateTime, double value);

    /// <summary>
    /// Update bar price data from a given DataProvider for a given instrument. Synthetic data that was generated can be set for a given instrument as well in the event that the specific
    /// DataProvider has missing.
    /// </summary>
    public void Update(IInstrument instrument, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume, bool synthetic);

    /// <summary>
    /// Perform a mass update of the price bar data for a specific data provider and instrument.
    /// </summary>
    public void Update(IInstrument instrument, Resolution resolution, IDataStoreService.BarData bars);

    /// <summary>
    /// Perform a mass update of the level 1 (tick) data for a specific data provider and instrument.
    /// </summary>
    public void Update(IInstrument instrument, IDataStoreService.Level1Data ticks);

    /// <summary>
    /// Delete a country definition.
    /// </summary>
    public void Delete(ICountry country);

    /// <summary>
    /// Delete a holiday.
    /// </summary>
    public void Delete(IHoliday holiday);

    /// <summary>
    /// Delete an exchange.
    /// </summary>
    public void Delete(IExchange exchange);

    /// <summary>
    /// Delete a session from an exchange.
    /// </summary>
    public void Delete(ISession session);

    /// <summary>
    /// Delete an instrument group.
    /// </summary>
    public void Delete(IInstrumentGroup instrumentGroup);

    /// <summary>
    /// Delete an instrument group (child) nested in another instrument group (parent).
    /// </summary>
    public void Delete(IInstrumentGroup parent, IInstrumentGroup child);

    /// <summary>
    /// Delete given instrument to the instrument group.
    /// </summary>
    public void Delete(IInstrumentGroup instrumentGroup, IInstrument instrument);

    /// <summary>
    /// Delete an instrument with all it's associated data.
    /// </summary>
    public void Delete(IInstrument instrument);

    /// <summary>
    /// Delete a secondary exchange association for a given instrument.
    /// </summary>
    public void DeleteSecondaryExchange(IInstrument instrument, IExchange exchange);

    /// <summary>
    /// Delete a fundamental.
    /// </summary>
    public void Delete(IFundamental fundamental);

    /// <summary>
    /// Delete the country fundamental for a given country.
    /// </summary>
    public void Delete(ICountryFundamental fundamental);

    /// <summary>
    /// Delete the instrument fundamental for a given instrument.
    /// </summary>
    public void Delete(IInstrumentFundamental fundamental);

    /// <summary>
    /// Delete a fundamental value for a given date/time.
    /// </summary>
    public void Delete(ICountryFundamental fundamental, DateTime dateTime);
    public void Delete(IInstrumentFundamental fundamental, DateTime dateTime);

    /// <summary>
    /// Delete a specific price bar.
    /// </summary>
    public void Delete(IInstrument instrument, Resolution resolution, DateTime dateTime, bool includeSynthetic);

    /// <summary>
    /// Delete a set of price bars.
    /// </summary>
    public void Delete(IInstrument instrument, Resolution resolution, DateTime from, DateTime to, bool includeSynthetic);
    
    /// <summary>
    /// Returns the data feed for a specific instrument and time interval based on data from a specific provider.
    /// </summary>
    public DataFeed GetDataFeed(IInstrument instrument, Resolution resolution, int interval, DateTime from, DateTime to, ToDateMode toDateMode = ToDateMode.Pinned, PriceDataType priceDataType = PriceDataType.Both);
  }
}
