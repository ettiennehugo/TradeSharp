using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Xml.Linq;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Data store for Sqlite database.
  /// </summary>
  public class SqliteDataStoreService : IDataStoreService
  {
    //constants
    public const string c_TableCountry = "Country";
    public const string c_TableHoliday = "Holiday";
    public const string c_TableExchange = "Exchange";
    public const string c_TableSession = "ExchangeSession";
    public const string c_TableInstrumentGroup = "InstrumentGroup";
    public const string c_TableInstrumentGroupInstrument = "InstrumentGroupInstrument";
    public const string c_TableInstrument = "Instrument";
    public const string c_TableInstrumentSecondaryExchange = "InstrumentSecondaryExchange";
    public const string c_TableForex = "Forex";
    public const string c_TableFundamentals = "Fundamentals";
    public const string c_TableCountryFundamentalAssociations = "CountryFundamentalAssociations";
    public const string c_TableCountryFundamentalValues = "CountryFundamentalValues";
    public const string c_TableInstrumentFundamentalAssociations = "InstrumentFundamentalAssociations";
    public const string c_TableInstrumentFundamentalValues = "InstrumentFundamentalValues";
    public const string c_TableInstrumentData = "Data";
    public const string c_IndexInstrumentData = "IData";
    public const string c_TableInstrumentDataSynthetic = "DataSynthetic";
    public const string c_IndexInstrumentDataSynthetic = "IDataSynthetic";

    public const string c_TableInstrumentDataSuffixMinute = "M1";
    public const string c_TableInstrumentDataSuffixHour = "H1";
    public const string c_TableInstrumentDataSuffixDay = "D";
    public const string c_TableInstrumentDataSuffixWeekly = "W";
    public const string c_TableInstrumentDataSuffixMonthly = "M";
    public const string c_TableInstrumentDataSuffixLevel1 = "L1";

    public static readonly Resolution[] s_SupportedResolutions = { Resolution.Minute, Resolution.Hour, Resolution.Day, Resolution.Week, Resolution.Month, Resolution.Level1 };

    //enums


    //types
    /// <summary>
    /// Association cache types for specific data providers sorted by name with dictionary of hash keys (fundamental and country/instrument) and Guid generated for the country or instrument fundamental. 
    /// </summary>
    public class AssociationCacheEntry : SortedDictionary<int, Guid> { }
    public class AssociationCache : SortedDictionary<string, AssociationCacheEntry> { }

    //attributes
    private IConfigurationService m_configurationService;
    private IDataProvider m_dataProvider;
    private string m_databaseFile;
    private string m_connectionString;
    private SqliteConnection m_connection;
    private AssociationCache m_countryFundamentalAssociations;
    private AssociationCache m_instrumentFundamentalAssociations;

    //constructors
    public SqliteDataStoreService(IConfigurationService configurationService)
    {
      m_configurationService = configurationService;
      m_databaseFile = "";
      m_connectionString = "";
      m_connection = new SqliteConnection();
      m_countryFundamentalAssociations = new AssociationCache();
      m_instrumentFundamentalAssociations = new AssociationCache();

      //Info on connection strings - https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings
      //Low level interoperability - https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/interop
      //Sqlite3 objects, functions - https://sqlite.org/c3ref/objlist.html, https://sqlite.org/c3ref/funclist.html

      //validate database type and setup the database connection
      IConfigurationService.DataStoreConfiguration dataStoreConfiguration = (IConfigurationService.DataStoreConfiguration)m_configurationService.General[IConfigurationService.GeneralConfiguration.DataStore];
      Trace.Assert(dataStoreConfiguration.Typename != this.GetType().Name, $"Incorrect data store \"{this.GetType().Name}\" instatiated against data store configuration \"{dataStoreConfiguration.Typename}\"");
      string tradeSharpHome = Environment.GetEnvironmentVariable(Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{Constants.TradeSharpHome}\" not defined.");
      m_databaseFile = string.Format("{0}\\{1}\\{2}", tradeSharpHome, Constants.DataDir, dataStoreConfiguration.ConnectionString);

      m_connectionString = new SqliteConnectionStringBuilder()
      {
        DataSource = m_databaseFile,
        Mode = SqliteOpenMode.ReadWriteCreate,
      }.ToString();

      m_connection = new SqliteConnection(m_connectionString);
      m_connection.Open();

      //enable write-ahead-log journalling for concurrent write/read operations - https://sqlite.org/wal.html
      ExecuteCommand("PRAGMA journal_mode=WAL");

      //create the data store schema
      CreateSchema();

      //create static required objects
      CreateDefaultObjects();
    }

    //finalizers
    public void Dispose()
    {
      m_connection.Close();
    }

    //interface implementations
    public void StartTransaction()
    {
      //https://sqlite.org/lang_transaction.html
      ExecuteCommand("BEGIN TRANSACTION");
    }

    public void EndTransaction(bool success)
    {
      //https://sqlite.org/lang_transaction.html
      if (success)
        ExecuteCommand("END TRANSACTION");
      else
        ExecuteCommand("ROLLBACK TRANSACTION");
    }

    public void CreateCountry(Country country)
    {
      ExecuteCommand($"INSERT OR REPLACE INTO {c_TableCountry} VALUES('{country.Id.ToString()}', {(long)country.AttributeSet}, '{SqlSafeString(country.Tag)}','{country.IsoCode}')");
    }

    public void UpdateCountry(Country country)
    {
      ExecuteCommand($"INSERT OR REPLACE INTO {c_TableCountry} VALUES('{country.Id.ToString()}', {(long)country.AttributeSet}, '{SqlSafeString(country.Tag)}','{country.IsoCode}')");

      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableExchange} " +
          $"SET AttributeSet = {(long)country.AttributeSet}, " +
              $"Tag = '{SqlSafeString(country.Tag)}', " +
              //$"IsoCode = '{country.IsoCode}' " +   //no update of the IsoCode, is only set on creation
          $"WHERE Id = '{country.Id.ToString()}'"
        );
    }

    public Country? GetCountry(Guid id)
    {
      Country? result = null;

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableCountry} WHERE Id = '{id.ToString()}'"))
        if (reader.Read()) result = new Country(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3));

      return result;
    }


    public IList<Country> GetCountries()
    {
      var result = new List<Country>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableCountry} ORDER BY IsoCode ASC"))
        while (reader.Read()) result.Add(new Country(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3)));

      return result;
    }

    public int DeleteCountry(Guid id)
    {
      int result = 0;

      result = Delete(c_TableCountry, id);
      foreach (var holidayId in GetAssociatedIds(c_TableHoliday, id, "ParentId")) result += DeleteHoliday(holidayId);
      foreach (var exchangeId in GetAssociatedIds(c_TableExchange, id, "CountryId")) result += DeleteExchange(exchangeId);

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProvider.Value, c_TableCountryFundamentalAssociations)} WHERE CountryId = '{id.ToString()}'"))
          while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, c_TableCountryFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");
        result += Delete(GetDataProviderDBName(dataProvider.Value, c_TableCountryFundamentalAssociations), id, "CountryId");
        CacheCountryFundamentalAssociations(dataProvider.Value);
      }

      return result;
    }

    public void CreateExchange(Exchange exchange)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableExchange} (Id, AttributeSet, Tag, CountryId, Name, TimeZone, LogoId) " +
          $"VALUES (" +
            $"'{exchange.Id.ToString()}', " +
            $"{(long)exchange.AttributeSet}, " +
            $"'{SqlSafeString(exchange.Tag)}', " +
            $"'{exchange.CountryId.ToString()}', " +
            $"'{SqlSafeString(exchange.Name)}', " +
            $"'{exchange.TimeZone.ToSerializedString()}', " +
            $"'{exchange.LogoId.ToString()}'" +
          $")"
      );
    }

    public Exchange? GetExchange(Guid id)
    {
      Exchange? result = null;

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableExchange} WHERE Id = '{id.ToString()}'"))
        if (reader.Read())
          result = new Exchange(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), TimeZoneInfo.FromSerializedString(reader.GetString(5)), reader.GetGuid(6));

      return result;
    }    
    
    public IList<Exchange> GetExchanges()
    {
      var result = new List<Exchange>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableExchange} ORDER BY Name ASC"))
        while (reader.Read())
          result.Add(new Exchange(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), TimeZoneInfo.FromSerializedString(reader.GetString(5)), reader.GetGuid(6)));

      return result;
    }

    public void UpdateExchange(Exchange exchange)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableExchange} " +
          $"SET CountryId = '{exchange.CountryId.ToString()}', " +
              $"Name = '{SqlSafeString(exchange.Name)}', " +
              $"AttributeSet = {(long)exchange.AttributeSet}, " +
              $"Tag = '{SqlSafeString(exchange.Tag)}', " +
              $"TimeZone = '{exchange.TimeZone.ToSerializedString()}', " +
              $"LogoId = '{exchange.LogoId}' " +
          $"WHERE Id = '{exchange.Id.ToString()}'"
      );
    }

    public int DeleteExchange(Guid id)
    {
      //get the exchange logo Id to delete the associated logo file
      Guid logoId = Guid.Empty;
      SqliteDataReader reader = ExecuteReader($"SELECT logoId FROM {c_TableExchange} WHERE Id = '{id.ToString()}'");
      if (reader.Read()) logoId = reader.GetGuid(0);

      //delete header entry
      int result = Delete(c_TableExchange, id);

      //delete logo image file
      string logoFilename = Exchange.GetLogoPath(logoId);
      if (logoFilename != Exchange.BlankLogoPath) File.Delete(logoFilename);

      //delete rest of the associated objects
      foreach (var holidayId in GetAssociatedIds(c_TableHoliday, id, "ParentId")) result += DeleteHoliday(holidayId);
      using (var instrumentRows = GetAssociatedRows(c_TableInstrument, id, "PrimaryExchangeId", "Id, Ticker"))
        while (instrumentRows.Read()) result += deleteInstrument(instrumentRows.GetGuid(0), instrumentRows.GetString(1));   //TOOD: This should rather move the instrument to the global exchange.
      result += Delete(c_TableInstrumentSecondaryExchange, id, "ExchangeId");
      foreach (var sessionId in GetAssociatedIds(c_TableSession, id, "ExchangeId")) result += DeleteSession(sessionId);
      return result;
    }

    public void CreateHoliday(Holiday holiday)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableHoliday} (Id, AttributeSet, Tag, ParentId, Name, HolidayType, Month, DayOfMonth, WeekOfMonth, DayOfWeek, MoveWeekendHoliday) " +
          $"VALUES (" +
            $"'{holiday.Id.ToString()}', " +
            $"{(long)holiday.AttributeSet}, " +
            $"'{SqlSafeString(holiday.Tag)}', " +
            $"'{holiday.ParentId.ToString()}', " +
            $"'{SqlSafeString(holiday.Name)}', " +
            $"{(int)holiday.Type}, " +
            $"{(int)holiday.Month}, " +
            $"{(int)holiday.DayOfMonth}, " +
            $"{(int)holiday.WeekOfMonth}, " +
            $"{(int)holiday.DayOfWeek}, " +
            $"{(int)holiday.MoveWeekendHoliday}" +
          ")"
      );
    }

    public Holiday? GetHoliday(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT * FROM {c_TableHoliday} WHERE Id = '{id.ToString()}'"))
      {
        if (reader.Read())
          return new Holiday(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), (HolidayType)reader.GetInt64(5), (Months)reader.GetInt64(6), reader.GetInt32(7), (DayOfWeek)reader.GetInt64(8), (WeekOfMonth)reader.GetInt64(9), (MoveWeekendHoliday)reader.GetInt64(10));

        return null;
      }
    }

    public IList<Holiday> GetHolidays(Guid parentId)
    {
      var result = new List<Holiday>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableHoliday} WHERE ParentId = '{parentId.ToString()}' ORDER BY Name ASC"))
        while (reader.Read())
          result.Add(new Holiday(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), (HolidayType)reader.GetInt64(5), (Months)reader.GetInt64(6), reader.GetInt32(7), (DayOfWeek)reader.GetInt64(8), (WeekOfMonth)reader.GetInt64(9), (MoveWeekendHoliday)reader.GetInt64(10)));

      return result;
    }

    public IList<Holiday> GetHolidays()
    {
      var result = new List<Holiday>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableHoliday} ORDER BY Name ASC"))
        while (reader.Read())
          result.Add(new Holiday(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), (HolidayType)reader.GetInt64(5), (Months)reader.GetInt64(6), reader.GetInt32(7), (DayOfWeek)reader.GetInt64(8), (WeekOfMonth)reader.GetInt64(9), (MoveWeekendHoliday)reader.GetInt64(10))); ;

      return result;
    }

    public void UpdateHoliday(Holiday holiday)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableHoliday} " +
          $"SET ParentId = '{holiday.ParentId.ToString()}', " +
              $"Name = '{SqlSafeString(holiday.Name)}', " +
              $"AttributeSet = {(long)holiday.AttributeSet}, " +
              $"Tag = '{SqlSafeString(holiday.Tag)}', " +
              $"HolidayType = {(int)holiday.Type}, " +
              $"Month = {(int)holiday.Month}, " +
              $"DayOfMonth = {(int)holiday.DayOfMonth}, " +
              $"WeekOfMonth = {(int)holiday.WeekOfMonth}, " +
              $"DayOfWeek = {(int)holiday.DayOfWeek}, " +
              $"MoveWeekendHoliday = {(int)holiday.MoveWeekendHoliday} " +
          $"WHERE Id = '{holiday.Id.ToString()}'"
      );
    }

    public int DeleteHoliday(Guid id)
    {
      return Delete(c_TableHoliday, id);
    }

    public void CreateSession(Session session)
    {
      ExecuteCommand(
      $"INSERT OR REPLACE INTO {c_TableSession} (Id, AttributeSet, Tag, Name, ExchangeId, DayOfWeek, StartTime, EndTime) " +
        $"VALUES (" +
          $"'{session.Id.ToString()}', " +
          $"{(long)session.AttributeSet}, " +
          $"'{SqlSafeString(session.Tag)}', " +
          $"'{SqlSafeString(session.Name)}', " +
          $"'{session.ExchangeId.ToString()}', " +
          $"{(int)session.DayOfWeek}, " +
          $"{session.Start.Ticks}, " +
          $"{session.End.Ticks}" +
        $")"
      );
    }

    public Session? GetSession(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT * FROM {c_TableSession} WHERE Id = '{id.ToString()}'"))
        if (reader.Read()) return new Session(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), (DayOfWeek)reader.GetInt32(5), new TimeOnly(reader.GetInt64(6)), new TimeOnly(reader.GetInt64(7)));

      return null;
    }

    public IList<Session> GetSessions(Guid exchangeId)
    {
      var result = new List<Session>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableSession} WHERE ExchangeId = '{exchangeId.ToString()}' ORDER BY DayOfWeek ASC, StartTime ASC, EndTime ASC"))
        while (reader.Read())
          result.Add(new Session(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), (DayOfWeek)reader.GetInt32(5), new TimeOnly(reader.GetInt64(6)), new TimeOnly(reader.GetInt64(7))));

      return result;
    }

    public IList<Session> GetSessions()
    {
      var result = new List<Session>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableSession}  ORDER BY ExchangeId ASC, DayOfWeek ASC, StartTime ASC, EndTime ASC"))
        while (reader.Read())
          result.Add(new Session(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), (DayOfWeek)reader.GetInt32(5), new TimeOnly(reader.GetInt64(6)), new TimeOnly(reader.GetInt64(7))));

      return result;
    }

    public void UpdateSession(Session session)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableSession} " +
          $"SET Name = '{SqlSafeString(session.Name)}', " +
              $"ExchangeId = '{session.ExchangeId.ToString()}', " +
              $"AttributeSet = {(long)session.AttributeSet}, " +
              $"Tag = '{SqlSafeString(session.Tag)}', " +
              $"DayOfWeek = {(int)session.DayOfWeek}, " +
              $"StartTime = {session.Start.Ticks}, " +
              $"EndTime = {session.End.Ticks} " +
          $"WHERE Id = '{session.Id.ToString()}'"
      );
    }

    public int DeleteSession(Guid id)
    {
      return Delete(c_TableSession, id);
    }


    //TODO: Needs to Update, Delete using the object instance. Also, needs to set the AttributeSet.

    public void CreateInstrumentGroup(InstrumentGroup instrumentGroup)
    {
      ExecuteCommand(
      $"INSERT OR REPLACE INTO {c_TableInstrumentGroup} (Id, AttributeSet, Tag, ParentId, Name, Description) " +
        $"VALUES (" +
          $"'{instrumentGroup.Id.ToString()}', " +
          $"{(long)instrumentGroup.AttributeSet}, " +
          $"'{SqlSafeString(instrumentGroup.Tag)}', " +
          $"'{instrumentGroup.ParentId.ToString()}', " +
          $"'{SqlSafeString(instrumentGroup.Name)}', " +
          $"'{SqlSafeString(instrumentGroup.Description)}'" +
        $")"
      );

      foreach (Guid instrumentId in instrumentGroup.Instruments) CreateInstrumentGroupInstrument(instrumentGroup.Id, instrumentId);
    }

    public void CreateInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableInstrumentGroupInstrument} (InstrumentGroupId, InstrumentId) " +
          $"VALUES (" +
            $"'{instrumentGroupId.ToString()}', " +
            $"'{instrumentId.ToString()}' " +
        $")"
      );
    }

    public InstrumentGroup? GetInstrumentGroup(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrumentGroup} WHERE Id == '{id.ToString()}'"))
        if (reader.Read())
        {
          IList<Guid> instruments = GetInstrumentGroupInstruments(id);
          return new InstrumentGroup(id, (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), reader.GetString(5), instruments);
        }

      return null;
    }

    public IList<InstrumentGroup> GetInstrumentGroups()
    {
      List<InstrumentGroup> result = new List<InstrumentGroup>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrumentGroup} ORDER BY ParentId ASC, Name ASC, Description ASC"))
      {
        while (reader.Read())
        {
          Guid id = reader.GetGuid(0);
          IList<Guid> instruments = GetInstrumentGroupInstruments(id);
          result.Add(new InstrumentGroup(id, (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), reader.GetString(5), instruments));
        }
      }

      return result;
    }

    public IList<Guid> GetInstrumentGroupInstruments(Guid instrumentGroupId)
    {
      List<Guid> result = new List<Guid>();

      using (var reader = ExecuteReader($"SELECT InstrumentGroupId, InstrumentId, Name, Description FROM {c_TableInstrumentGroupInstrument} INNER JOIN {c_TableInstrumentGroup} ON InstrumentGroupId == Id WHERE InstrumentGroupId = '{instrumentGroupId.ToString()}' ORDER BY Name ASC, Description ASC"))
        while (reader.Read()) result.Add(reader.GetGuid(1));

      return result;
    }

    public void UpdateInstrumentGroup(InstrumentGroup instrumentGroup)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableInstrumentGroup} SET " +
            $"ParentId = '{instrumentGroup.ParentId.ToString()}', " +
            $"AttributeSet = {(long)instrumentGroup.AttributeSet}, " +
            $"Tag = '{SqlSafeString(instrumentGroup.Tag)}', " +
            $"Name = '{SqlSafeString(instrumentGroup.Name)}', " +
            $"Description = '{instrumentGroup.Description}' " +
          $"WHERE Id = '{instrumentGroup.Id.ToString()}'"
      );

      Delete(c_TableInstrumentGroupInstrument, instrumentGroup.Id, "InstrumentGroupId");

      foreach (Guid instrumentId in instrumentGroup.Instruments)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {c_TableInstrumentGroupInstrument} (InstrumentGroupId, InstrumentId) " +
            $"VALUES (" +
              $"'{instrumentGroup.Id.ToString()}', " +
              $"'{instrumentId.ToString()}'" +
          $")"
        );
    }

    public int DeleteInstrumentGroup(Guid id)
    {
      int result = 0;
      using (var reader = ExecuteReader($"SELECT Id FROM {c_TableInstrumentGroup} WHERE ParentId = '{id.ToString()}'"))
        while (reader.Read()) result += DeleteInstrumentGroup(reader.GetGuid(0));

      result += ExecuteCommand($"DELETE FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{id.ToString()}'");
      result += ExecuteCommand($"DELETE FROM {c_TableInstrumentGroup} WHERE Id = '{id.ToString()}'");

      return result;
    }

    public int DeleteInstrumentGroupChild(Guid parentId, Guid childId)
    {
      //NOTE: We do not use the parentId for Sqlite, we just reset the parentId on the instrument group table for the given child.
      return ExecuteCommand($"UPDATE OR IGNORE {c_TableInstrumentGroup} SET ParentId = '{InstrumentGroup.InstrumentGroupRoot.ToString()}' WHERE Id = '{childId.ToString()}'");
    }

    public int DeleteInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId)
    {
      return ExecuteCommand($"DELETE FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{instrumentGroupId.ToString()}' AND InstrumentId = '{instrumentId.ToString()}'");
    }

    public void CreateInstrument(Instrument instrument)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableInstrument} (Id, AttributeSet, Tag, Type, Ticker, Name, Description, PrimaryExchangeId, InceptionDate) " +
          $"VALUES (" +
            $"'{instrument.Id.ToString()}', " +
            $"{(long)instrument.AttributeSet}, " +
            $"'{SqlSafeString(instrument.Tag)}', " +
            $"{(int)instrument.Type}, " +
            $"'{instrument.Ticker}', " +
            $"'{SqlSafeString(instrument.Name)}', " +
            $"'{SqlSafeString(instrument.Description)}', " +
            $"'{instrument.PrimaryExchangeId.ToString()}', " +
            $"{instrument.InceptionDate.ToUniversalTime().ToBinary()}" +
          $")"
      );

      foreach (Guid otherExchangeId in instrument.SecondaryExchangeIds)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {c_TableInstrumentSecondaryExchange} (InstrumentId, ExchangeId) " +
            $"VALUES (" +
              $"'{instrument.Id.ToString()}', " +
              $"'{otherExchangeId.ToString()}'" +
            $")"
        );
      }
    }

    public void AddInstrumentToExchange(Guid instrumentId, Guid exchangeId)
    {
      ExecuteCommand(
        $"INSERT OR IGNORE INTO {c_TableInstrumentSecondaryExchange} (InstrumentId, ExchangeId) " +
          $"VALUES (" +
            $"'{instrumentId.ToString()}', " +
            $"'{exchangeId.ToString()}' " +
          $")"
      );
    }

    public IList<Instrument> GetInstruments()
    {
      var result = new List<Instrument>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrument} ORDER BY Type ASC, Ticker ASC, Name ASC, Description ASC"))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          Guid instrumentId = reader.GetGuid(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{instrumentId.ToString()}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));
          
          result.Add(new Instrument(instrumentId, (Attributes)reader.GetInt64(1), reader.GetString(2), (InstrumentType)reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), DateTime.FromBinary(reader.GetInt64(8)), reader.GetGuid(7), secondaryExchangeIds));
        }

      return result;
    }

    public IList<Instrument> GetInstruments(InstrumentType instrumentType)
    {
      var result = new List<Instrument>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrument} WHERE Type = {(int)instrumentType} ORDER BY Ticker ASC, Name ASC, Description ASC"))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          Guid instrumentId = reader.GetGuid(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{instrumentId.ToString()}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          result.Add(new Instrument(instrumentId, (Attributes)reader.GetInt64(1), reader.GetString(2), (InstrumentType)reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), DateTime.FromBinary(reader.GetInt64(8)), reader.GetGuid(7), secondaryExchangeIds));
        }

      return result;
    }

    public Instrument? GetInstrument(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrument} WHERE Id = '{id.ToString()}'"))
        if (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{id.ToString()}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));
          return new Instrument(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), (InstrumentType)reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), DateTime.FromBinary(reader.GetInt64(8)), reader.GetGuid(7), secondaryExchangeIds);
        }

        return null;
    }

    public void UpdateInstrument(Instrument instrument)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableInstrument} " +
          $"SET Ticker = '{instrument.Ticker}', " + 
              $"AttributeSet = '{(long)instrument.AttributeSet}', " +
              $"Tag = '{SqlSafeString(instrument.Tag)}', " +
              $"Name = '{SqlSafeString(instrument.Name)}', " +
              $"Description = '{SqlSafeString(instrument.Description)}', " +
              $"PrimaryExchangeId = '{instrument.PrimaryExchangeId.ToString()}', " +
              $"InceptionDate = {instrument.InceptionDate.ToUniversalTime().ToBinary()} " +
          $"WHERE Id = '{instrument.Id.ToString()}'"
      );

      Delete(c_TableInstrumentSecondaryExchange, instrument.Id, "InstrumentId");
      foreach (Guid otherExchangeId in instrument.SecondaryExchangeIds)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {c_TableInstrumentSecondaryExchange} (InstrumentId, ExchangeId) " +
            $"VALUES (" +
              $"'{instrument.Id.ToString()}', " +
              $"'{otherExchangeId.ToString()}'" +
            $")"
        );
      }
    }

    public int DeleteInstrument(Instrument instrument)
    {
      return deleteInstrument(instrument.Id, instrument.Ticker);
    }

    public int deleteInstrument(Guid id, string ticker)
    {
      int result = Delete(c_TableInstrument, id);
      result += Delete(c_TableInstrumentSecondaryExchange, id, "InstrumentId");
      result += Delete(c_TableInstrumentGroupInstrument, id, "InstrumentId");

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalAssociations)} WHERE InstrumentId = '{id.ToString()}'"))
          while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");
        result += Delete(GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalAssociations), id, "InstrumentId");
        result += deleteData(dataProvider.Value, ticker, null);
        CacheInstrumentFundamentalAssociations(dataProvider.Value);
      }

      return result;
    }

    public int DeleteInstrumentFromExchange(Guid instrumentId, Guid exchangeId)
    {
      return ExecuteCommand($"DELETE FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{instrumentId.ToString()}' AND ExchangeId = '{exchangeId.ToString()}'");
    }

    public void CreateFundamental(Fundamental fundamental)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableFundamentals} (Id, AttributeSet, Tag, Name, Description, Category, ReleaseInterval)" +
          $"VALUES (" +
            $"'{fundamental.Id.ToString()}', " +
            $"{(long)fundamental.AttributeSet}, " +
            $"'{SqlSafeString(fundamental.Tag)}', " +
            $"'{SqlSafeString(fundamental.Name)}', " +
            $"'{SqlSafeString(fundamental.Description)}', " +
            $"{(int)fundamental.Category}, " +
            $"{(int)fundamental.ReleaseInterval}" +
        $")"
      );
    }

    public IList<Fundamental> GetFundamentals()
    {
      var result = new List<Fundamental>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableFundamentals} ORDER BY Category ASC, Name ASC, Description ASC"))
        while (reader.Read())
          result.Add(new Fundamental(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), (FundamentalCategory)reader.GetInt32(5), (FundamentalReleaseInterval)reader.GetInt32(6)));

      return result;
    }

    public int DeleteFundamental(Guid id)
    {
      int result = 0;

      result = DeleteFundamentalValues(id);
      result += Delete(c_TableFundamentals, id);

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        CacheCountryFundamentalAssociations(dataProvider.Value);
        CacheInstrumentFundamentalAssociations(dataProvider.Value);
      }
      return result;
    }

    public int DeleteFundamentalValues(Guid id)
    {
      int result = 0;
      foreach (var dataProvider in m_configurationService.DataProviders) result += DeleteFundamentalValues(dataProvider.Value, id);
      return result;
    }

    public int DeleteFundamentalValues(string dataProviderName, Guid id)
    {
      int result = 0;

      using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations)} WHERE FundamentalId = '{id.ToString()}'"))
        while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");

      using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalAssociations)} WHERE FundamentalId = '{id.ToString()}'"))
        while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        CacheCountryFundamentalAssociations(dataProvider.Value);
        CacheInstrumentFundamentalAssociations(dataProvider.Value);
      }

      return result;
    }

    public void CreateCountryFundamental(CountryFundamental fundamental)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {GetDataProviderDBName(fundamental.DataProviderName, c_TableCountryFundamentalAssociations)} (Id, FundamentalId, CountryId) " +
          $"VALUES (" +
            $"'{fundamental.AssociationId.ToString()}', " +
            $"'{fundamental.FundamentalId.ToString()}', " +
            $"'{fundamental.CountryId.ToString()}'" +
          $")"
      );

      AssociationCacheEntry? cacheEntry;
      if (m_countryFundamentalAssociations.TryGetValue(fundamental.DataProviderName, out cacheEntry))
        cacheEntry![fundamental.FundamentalId.GetHashCode() + fundamental.CountryId.GetHashCode()] = fundamental.AssociationId;
      else
      {
        cacheEntry = new AssociationCacheEntry() { { fundamental.FundamentalId.GetHashCode() + fundamental.CountryId.GetHashCode(), fundamental.AssociationId } };
        m_countryFundamentalAssociations[fundamental.DataProviderName] = cacheEntry;
      }
    }

    public IList<CountryFundamental> GetCountryFundamentals(string dataProviderName)
    {
      var result = new List<CountryFundamental>();

      //load basic fundamental and country associations
      string dataProviderAssociationTable = GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations);
      string selectQuery =
        $"SELECT * FROM {dataProviderAssociationTable} " +
          $"INNER JOIN {c_TableFundamentals} ON {dataProviderAssociationTable}.FundamentalId = {c_TableFundamentals}.Id " +
          $"WHERE {c_TableFundamentals}.Category = {(int)FundamentalCategory.Country} " +
          $"ORDER BY {c_TableFundamentals}.Name ASC, {c_TableFundamentals}.Description ASC, {dataProviderAssociationTable}.FundamentalId ASC, {dataProviderAssociationTable}.CountryId ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read()) result.Add(new CountryFundamental(dataProviderName, reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2)));

      //load fundamental values
      string dataProviderValueTable = GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues);
      selectQuery = $"SELECT * FROM {dataProviderValueTable} ORDER BY AssociationId, DateTime ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read())
        {
          Guid associationId = reader.GetGuid(0);
          DateTime dateTime = DateTime.FromBinary(reader.GetInt64(1));
          double value = reader.GetDouble(2);

          foreach (var countryFundamental in result)
            if (countryFundamental.AssociationId == associationId)
            {
              countryFundamental.AddValue(dateTime, value);
              break;
            }
        }

      return result;
    }

    public void UpdateCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime, double value)
    {
      Guid? associationId = GetCountryFundamentalAssociationId(dataProviderName, fundamentalId, countryId);
      if (!associationId.HasValue) throw new ArgumentException($"Country ({countryId}) is not associated with fundamental ({fundamentalId}).");

      ExecuteCommand(
        $"INSERT OR REPLACE INTO {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues)} (AssociationId, DateTime, Value) " +
          $"VALUES (" +
            $"'{associationId!.Value.ToString()}', " +
            $"{dateTime.ToUniversalTime().ToBinary()}, " +
            $"{value.ToString()}" +
        $")"
      );
    }

    public int DeleteCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId)
    {
      Guid? associationId = GetCountryFundamentalAssociationId(dataProviderName, fundamentalId, countryId);
      if (!associationId.HasValue) return 0;

      int result = ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations)} WHERE Id = '{associationId.ToString()}'");
      result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}'");

      CacheCountryFundamentalAssociations(dataProviderName);

      return result;
    }

    public int DeleteCountryFundamentalValue(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime)
    {
      Guid? associationId = GetCountryFundamentalAssociationId(dataProviderName, fundamentalId, countryId);

      if (!associationId.HasValue) return 0;  //no association, nothing to remove

      return ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}' AND DateTime = {dateTime.ToUniversalTime().ToBinary()}"); ;
    }

    public void CreateInstrumentFundamental(InstrumentFundamental fundamental)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {GetDataProviderDBName(fundamental.DataProviderName, c_TableInstrumentFundamentalAssociations)} (Id, FundamentalId, InstrumentId) " +
          $"VALUES (" +
            $"'{fundamental.AssociationId.ToString()}', " +
            $"'{fundamental.FundamentalId.ToString()}', " +
            $"'{fundamental.InstrumentId.ToString()}'" +
          $")"
      );

      AssociationCacheEntry? cacheEntry;
      if (m_instrumentFundamentalAssociations.TryGetValue(fundamental.DataProviderName, out cacheEntry))
        cacheEntry![fundamental.FundamentalId.GetHashCode() + fundamental.InstrumentId.GetHashCode()] = fundamental.AssociationId;
      else
      {
        cacheEntry = new AssociationCacheEntry() { { fundamental.FundamentalId.GetHashCode() + fundamental.InstrumentId.GetHashCode(), fundamental.AssociationId } };
        m_instrumentFundamentalAssociations[fundamental.DataProviderName] = cacheEntry;
      }
    }

    public IList<InstrumentFundamental> GetInstrumentFundamentals(string dataProviderName)
    {
      var result = new List<InstrumentFundamental>();

      //load basic fundamental and instrument associations
      string dataProviderAssociationTable = GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalAssociations);
      string selectQuery =
        $"SELECT * FROM {dataProviderAssociationTable} " +
          $"INNER JOIN {c_TableFundamentals} ON {dataProviderAssociationTable}.FundamentalId = {c_TableFundamentals}.Id " +
          $"WHERE {c_TableFundamentals}.Category = {(int)FundamentalCategory.Instrument} " +
          $"ORDER BY {c_TableFundamentals}.Name ASC, {c_TableFundamentals}.Description ASC, {dataProviderAssociationTable}.FundamentalId ASC, {dataProviderAssociationTable}.InstrumentId ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read()) result.Add(new InstrumentFundamental(dataProviderName, reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2)));

      //load fundamental values
      string dataProviderValueTable = GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues);
      selectQuery = $"SELECT * FROM {dataProviderValueTable} ORDER BY AssociationId, DateTime ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read())
        {
          Guid associationId = reader.GetGuid(0);
          DateTime dateTime = DateTime.FromBinary(reader.GetInt64(1));
          double value = reader.GetDouble(2);

          foreach (var countryFundamental in result)
            if (countryFundamental.AssociationId == associationId)
            {
              countryFundamental.AddValue(dateTime, value);
              break;
            }
        }

      return result;
    }

    public void UpdateInstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime, double value)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentId);
      if (!associationId.HasValue) throw new ArgumentException($"Instrument ({instrumentId}) is not associated with fundamental ({fundamentalId}).");

      ExecuteCommand(
        $"INSERT OR REPLACE INTO {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues)} (AssociationId, DateTime, Value) " +
          $"VALUES (" +
            $"'{associationId!.Value.ToString()}', " +
            $"{dateTime.ToUniversalTime().ToBinary()}, " +
            $"{value.ToString()}" +
        $")"
      );
    }

    public int DeleteInstrumentFundamental(string dataProviderName, Guid fundamentalId, Guid instrumentId)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentId);
      if (!associationId.HasValue) return 0;

      int result = ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalAssociations)} WHERE Id = '{associationId.ToString()}'");
      result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}'");

      CacheInstrumentFundamentalAssociations(dataProviderName);

      return result;
    }

    public int DeleteInstrumentFundamentalValue(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentId);

      if (!associationId.HasValue) return 0;  //no association, nothing to remove

      return ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}' AND DateTime = {dateTime.ToUniversalTime().ToBinary()}");
    }

    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, long volume, bool synthetic)
    {
      //level 1 data can not be updated by his method
      if (resolution == Resolution.Level1) throw new ArgumentException("Update for bar data can not update Level 1 data.");

      //create database command
      string command;
      string tableName;
      string normalizedTicker = ticker.ToUpper();

      if (!synthetic)
        tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution);
      else
        tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution);

      command =
        $"INSERT OR REPLACE INTO {tableName} (Ticker, DateTime, Open, High, Low, CLose, Volume) " +
          $"VALUES (" +
            $"'{normalizedTicker}', " +
            $"{dateTime.ToUniversalTime().ToBinary()}, " +
            $"{open}, " +
            $"{high}, " +
            $"{low}, " +
            $"{close}, " +
            $"{volume}" +
        $")";

      ExecuteCommand(command);
    }

    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, BarData bars)
    {
      //level 1 data can not be updated by his method
      if (resolution == Resolution.Level1) throw new ArgumentException("Update for bar data can not update Level 1 data.");
      if (bars.Count == 0) throw new ArgumentException("Update data count should not be zero.");

      //create database update
      using (var transaction = m_connection.BeginTransaction())
      {
        var command = m_connection.CreateCommand();
        command.Transaction = transaction;

        string tableName;
        string normalizedTicker = ticker.ToUpper();
        string barTableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution);
        string syntheticTableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution);

        for (int index = 0; index < bars.Count; index++)
        {
          if (!bars.Synthetic[index])
            tableName = barTableName;
          else
            tableName = syntheticTableName;

          command.CommandText = $"INSERT OR REPLACE INTO {tableName} (Ticker, DateTime, Open, High, Low, Close, Volume) " +
            $"VALUES (" +
              $"'{normalizedTicker}', " +
              $"{bars.DateTime[index].ToUniversalTime().ToBinary()}, " +
              $"{bars.Open[index]}, " +
              $"{bars.High[index]}, " +
              $"{bars.Low[index]}, " +
              $"{bars.Close[index]}, " +
              $"{bars.Volume[index]}" +
            $")";

          command.ExecuteNonQuery();
        }

        transaction.Commit();
      }
    }

    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Level1Data level1Data)
    {
      if (level1Data.Count == 0) throw new ArgumentException("Update data count should not be zero.");

      //create database update
      using (var transaction = m_connection.BeginTransaction())
      {
        var command = m_connection.CreateCommand();
        command.Transaction = transaction;

        string tableName;
        string normalizedTicker = ticker.ToUpper();
        string barTableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, Resolution.Level1);
        string syntheticTableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, Resolution.Level1);

        for (int index = 0; index < level1Data.Count; index++)
        {
          if (!level1Data.Synthetic[index])
            tableName = barTableName;
          else
            tableName = syntheticTableName;

          command.CommandText = $"INSERT OR REPLACE INTO {tableName} (Ticker, DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize) " +
            $"VALUES (" +
              $"'{normalizedTicker}', " +
              $"{level1Data.DateTime[index].ToUniversalTime().ToBinary()}, " +
              $"{level1Data.Bid[index]}, " +
              $"{level1Data.BidSize[index]}, " +
              $"{level1Data.Ask[index]}, " +
              $"{level1Data.AskSize[index]}, " +
              $"{level1Data.Last[index]}, " +
              $"{level1Data.LastSize[index]}" +
            $")";

          command.ExecuteNonQuery();
        }

        transaction.Commit();
      }
    }

    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime dateTime, bool? synthetic = null)
    {
      return deleteData(dataProviderName, ticker, resolution, dateTime, synthetic);
    }

    /// <summary>
    /// General utility function to delete price data from the database using various input parameters.
    /// </summary>
    protected int deleteData(string dataProviderName, string ticker, Resolution? resolution = null, DateTime? dateTime = null, bool? synthetic = null)
    {
      int result = 0;

      if (resolution.HasValue)
      {
        if (synthetic.HasValue)
        {
          //delete only specific type of data, actual vs synthetic
          string tableName;
          if (synthetic.Value)
            tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution.Value);
          else
            tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution.Value);

          result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {tableName} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                        ExecuteCommand($"DELETE FROM {tableName} WHERE Ticker = '{ticker}'");
        }
        else
        {
          //delete both actual and synthetic data
          result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution.Value)} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") : 
                                        ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution.Value)} WHERE Ticker = '{ticker}'");
          result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution.Value)} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                        ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution.Value)} WHERE Ticker = '{ticker}'");
        }
      }
      else
      {
        foreach (Resolution res in s_SupportedResolutions)
        {
          if (synthetic != null)
          {
            //delete only specific type of data, actual vs synthetic
            string tableName;
            if (synthetic.Value)
              tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, res);
            else
              tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, res);

            result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {tableName} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                          ExecuteCommand($"DELETE FROM {tableName} WHERE Ticker = '{ticker}'");
          }
          else
          {
            //delete both actual and synthetic data
            result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, res)} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                          ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, res)} WHERE Ticker = '{ticker}'");
            result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, res)} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                          ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, res)} WHERE Ticker = '{ticker}'");
          }
        }
      }

      return result;
    }

    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime? from = null, DateTime? to = null, bool? synthetic = null)
    {
      int result = 0;

      string where = $"Ticker = '{ticker}'";
      if (from != null) where += $"AND DateTime >= {from.Value.ToUniversalTime().ToBinary()}";
      if (to != null) where += $"AND DateTime >= {to.Value.ToUniversalTime().ToBinary()}";

      if (resolution != null)
      {
        if (synthetic != null)
        {
          //delete only specific type of data, actual vs synthetic
          string tableName;
          if (synthetic.Value)
            tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution.Value);
          else
            tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution.Value);

          result += ExecuteCommand($"DELETE FROM {tableName} WHERE " + where);
        }
        else
        {
          //delete both actual and synthetic data
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution.Value)} WHERE " + where);
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution.Value)} WHERE " + where);
        }
      }
      else
      {
        foreach (Resolution res in s_SupportedResolutions)
        {
          if (synthetic != null)
          {
            //delete only specific type of data, actual vs synthetic
            string tableName;
            if (synthetic.Value)
              tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, res);
            else
              tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, res);

            result += ExecuteCommand($"DELETE FROM {tableName} WHERE " + where);
          }
          else
          {
            //delete both actual and synthetic data
            result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, res)} WHERE " + where);
            result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, res)} WHERE " + where);
          }
        }
      }

      return result;
    }

    public DataCache GetBarData(string dataProviderName, Guid instrumentId, string ticker, DateTime from, DateTime to, Resolution resolution, PriceDataType priceDataType)
    {
      //validate inputs
      if (resolution == Resolution.Level1) throw new ArgumentException("GetBarData can not return Level  date, use GetLevel1Data.");

      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //create database command
      SortedDictionary<DateTime, Tuple<DateTime, double, double, double, double, long, bool>> list = new SortedDictionary<DateTime, Tuple<DateTime, double, double, double, double, long, bool>>();

      string command;
      string normalizedTicker = ticker.ToUpper();

      //load actual data if required
      if (priceDataType == PriceDataType.Actual || priceDataType == PriceDataType.Both)
      {
        command =
          $"SELECT * FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution)} " +
            $"WHERE " +
              $"Ticker = '{normalizedTicker}' " +
              $"AND DateTime >= {fromUtc.ToUniversalTime().ToBinary()} " +
              $"AND DateTime <= {toUtc.ToUniversalTime().ToBinary()} " +
            $"ORDER BY DateTime ASC";

        using (SqliteDataReader reader = ExecuteReader(command))
        {
          while (reader.Read())
          {
            var dateTime = DateTime.FromBinary(reader.GetInt64(1));
            list.Add(dateTime, new Tuple<DateTime, double, double, double, double, long, bool>(dateTime, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetInt64(6), false));
          }
        }
      }

      //load synthetic price data if required
      if (priceDataType == PriceDataType.Synthetic || priceDataType == PriceDataType.Both)
      {
        command =
          $"SELECT * FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution)} " +
            $"WHERE " +
              $"Ticker = '{normalizedTicker}' " +
              $"AND DateTime >= {fromUtc.ToUniversalTime().ToBinary()} " +
              $"AND DateTime <= {toUtc.ToUniversalTime().ToBinary()} " +
            $"ORDER BY DateTime ASC";

        using (SqliteDataReader reader = ExecuteReader(command))
        {
          while (reader.Read())
          {
            var dateTime = DateTime.FromBinary(reader.GetInt64(1));

            if (!list.ContainsKey(dateTime))
              list.Add(dateTime, new Tuple<DateTime, double, double, double, double, long, bool>(dateTime, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetInt64(6), true));
          }
        }
      }

      DataCache dataCache = new DataCache(dataProviderName, instrumentId, resolution, priceDataType, from, to, list.Count);
      BarData barData = (BarData)dataCache.Data;

      int i = 0;
      foreach (var bar in list)
      {
        barData.DateTime[i] = bar.Value.Item1;
        barData.Open[i] = bar.Value.Item2;
        barData.High[i] = bar.Value.Item3;
        barData.Low[i] = bar.Value.Item4;
        barData.Close[i] = bar.Value.Item5;
        barData.Volume[i] = bar.Value.Item6;
        barData.Synthetic[i] = bar.Value.Item7;
        i++;
      }

      return dataCache;
    }

    /// <summary>
    /// Structure to hold level 1 data from the database.
    /// </summary>
    private struct Level1DBRecord
    {
      public DateTime DateTime;
      public double Bid;
      public long BidSize;
      public double Ask;
      public long AskSize;
      public double Last;
      public long LastSize;
      public bool Synthetic;
    }

    /// <summary>
    /// Loads level 1 tick data from the database and returns the populated DataCache structure.
    /// </summary>
    public DataCache GetLevel1Data(string dataProviderName, Guid instrumentId, string ticker, DateTime from, DateTime to, PriceDataType priceDataType)
    {
      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //create database command
      SortedDictionary<DateTime, Level1DBRecord> list = new SortedDictionary<DateTime, Level1DBRecord>();

      string command;
      string normalizedTicker = ticker.ToUpper();

      //get actual bar data
      if (priceDataType == PriceDataType.Actual || priceDataType == PriceDataType.Both)
      {
        command =
          $"SELECT DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentData, Resolution.Level1)} " +
            $"WHERE " +
              $"Ticker = '{normalizedTicker}' " +
              $"AND DateTime >= {fromUtc.ToUniversalTime().ToBinary()} " +
              $"AND DateTime <= {toUtc.ToUniversalTime().ToBinary()} " +
            $"ORDER BY DateTime ASC";

        using (SqliteDataReader reader = ExecuteReader(command))
        {
          while (reader.Read())
          {
            Level1DBRecord level1DB = new Level1DBRecord();
            level1DB.DateTime = DateTime.FromBinary(reader.GetInt64(0));
            level1DB.Bid = reader.GetDouble(1);
            level1DB.BidSize = reader.GetInt64(2);
            level1DB.Ask = reader.GetDouble(3);
            level1DB.AskSize = reader.GetInt64(4);
            level1DB.Last = reader.GetDouble(5);
            level1DB.LastSize = reader.GetInt64(6);
            level1DB.Synthetic = false;
            list.Add(level1DB.DateTime, level1DB);
          }
        }
      }

      //get synthetic bar data
      if (priceDataType == PriceDataType.Synthetic || priceDataType == PriceDataType.Both)
      {
        command =
          $"SELECT DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, Resolution.Level1)} " +
            $"WHERE " +
              $"Ticker = '{normalizedTicker}' " +
              $"AND DateTime >= {fromUtc.ToUniversalTime().ToBinary()} " +
              $"AND DateTime <= {toUtc.ToUniversalTime().ToBinary()} " +
            $"ORDER BY DateTime ASC";

        using (SqliteDataReader reader = ExecuteReader(command))
        {
          while (reader.Read())
          {
            Level1DBRecord level1DB = new Level1DBRecord();
            level1DB.DateTime = DateTime.FromBinary(reader.GetInt64(0));
            level1DB.Bid = reader.GetDouble(1);
            level1DB.BidSize = reader.GetInt64(2);
            level1DB.Ask = reader.GetDouble(3);
            level1DB.AskSize = reader.GetInt64(4);
            level1DB.Last = reader.GetDouble(5);
            level1DB.LastSize = reader.GetInt64(6);
            level1DB.Synthetic = true;
            list.Add(level1DB.DateTime, level1DB);

            if (!list.ContainsKey(level1DB.DateTime))
              list.Add(level1DB.DateTime, level1DB);
          }
        }
      }

      DataCache dataCache = new DataCache(dataProviderName, instrumentId, Resolution.Level1, priceDataType, from, to, list.Count);
      Level1Data level1Data = (Level1Data)dataCache.Data;

      int i = 0;
      foreach (var entry in list)
      {
        level1Data.DateTime[i] = entry.Value.DateTime;
        level1Data.Bid[i] = entry.Value.Bid;
        level1Data.BidSize[i] = entry.Value.BidSize;
        level1Data.Ask[i] = entry.Value.Ask;
        level1Data.AskSize[i] = entry.Value.AskSize;
        level1Data.Last[i] = entry.Value.Last;
        level1Data.LastSize[i] = entry.Value.LastSize;
        level1Data.Synthetic[i] = entry.Value.Synthetic;
        i++;
      }

      return dataCache;
    }

    public DataCache GetInstrumentData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, DateTime from, DateTime to, PriceDataType priceDataType)
    {
      switch (resolution)
      {
        case Resolution.Minute:
        case Resolution.Hour:
        case Resolution.Day:
        case Resolution.Week:
        case Resolution.Month:
          return GetBarData(dataProviderName, instrumentId, ticker, from, to, resolution, priceDataType);

        case Resolution.Level1:
          return GetLevel1Data(dataProviderName, instrumentId, ticker, from, to, priceDataType);

        default:
          throw new ArgumentException("Unknown resolution.");
      }
    }

    //properties
    public IList<Resolution> SupportedDataResolutions { get => s_SupportedResolutions; }

    //methods
    /// <summary>
    /// Generic utility method to support deletion of objects using a specific id fieldname in the database. Returns the number of rows deleted.
    /// </summary>
    protected int Delete(string tableName, Guid id, string idFieldname = "Id")
    {
      return ExecuteCommand($"DELETE FROM {tableName} WHERE {idFieldname} = '{id.ToString()}'");
    }

    /// <summary>
    /// Returns the set of associated Id's from the database matching entries from a given parent object.
    /// </summary>
    protected List<Guid> GetAssociatedIds(string tableName, Guid id, string matchParentIdFieldname, string idFieldname = "Id")
    {
      List<Guid> result = new List<Guid>();

      using (var reader = ExecuteReader($"SELECT {idFieldname} FROM {tableName} WHERE {matchParentIdFieldname} = '{id.ToString()}'"))
      {
        while (reader.Read()) result.Add(reader.GetGuid(0));
      }

      return result;
    }

    /// <summary>
    /// Returns a reader of the database entries matching the given entries in a table.
    /// </summary>
    protected SqliteDataReader GetAssociatedRows(string tableName, Guid id, string matchParentIdFieldname, string selectFieldList = "Id")
    {
      var command = m_connection.CreateCommand();
      command.CommandText = $"SELECT {selectFieldList} FROM {tableName} WHERE {matchParentIdFieldname} = '{id.ToString()}'";
      return command.ExecuteReader();
    }

    /// <summary>
    /// Creates the database schema to store the data.
    /// </summary>
    public void CreateSchema()
    {
      try
      {
        StartTransaction();

        //create general data tables if required
        CreateCountryTable();
        CreateHolidayTable();
        CreateExchangeTable();
        CreateSessionTable();
        CreateInstrumentGroupTable();
        CreateInstrumentGroupInstrumentTable();
        CreateInstrumentTable();
        CreateInstrumentSecondaryExchangeTable();
        CreateFundamentalsTable();

        //create data provider specific data tables if required
        foreach (var dataProvider in m_configurationService.DataProviders)
        {
          if (!requireTableDefinitions(dataProvider.Value)) continue;
          CreateCountryFundamentalAssociationTable(dataProvider.Value);
          CreateCountryFundamentalValuesTable(dataProvider.Value);
          CreateInstrumentFundamentalAssociationTable(dataProvider.Value);
          CreateInstrumentFundamentalValuesTable(dataProvider.Value);
          foreach (Resolution resolution in s_SupportedResolutions)
          {
            CreateInstrumentDataTable(dataProvider.Value, resolution);
            CreateInstrumentDataSyntheticTable(dataProvider.Value, resolution);
          }
        }

        EndTransaction(true);
      }
      catch (Exception)
      {
        EndTransaction(false);
        throw;  //rethrow the exception with the stack in place
      }
    }

    /// <summary>
    /// Creates the default objects used for the data model that can not be removed.
    /// </summary>
    public void CreateDefaultObjects()
    {
      try
      {
        StartTransaction();

        if (GetRowCount(c_TableCountry, $"Id == '{Country.InternationalId.ToString()}'") == 0) CreateCountry(new Country(Country.InternationalId, Attributes.None, "", Country.InternationalIsoCode));
        if (GetRowCount(c_TableExchange, $"Id == '{Exchange.InternationalId.ToString()}'") == 0) CreateExchange(new Exchange(Exchange.InternationalId, Attributes.None, "", Country.InternationalId, "Global Exchange", TimeZoneInfo.Utc, Exchange.InternationalId));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Monday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Monday", Exchange.InternationalId, DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Tuesday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Tuesday", Exchange.InternationalId, DayOfWeek.Tuesday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Wednesday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Wednesday", Exchange.InternationalId, DayOfWeek.Wednesday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Thursday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Thursday", Exchange.InternationalId, DayOfWeek.Thursday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Friday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Friday", Exchange.InternationalId, DayOfWeek.Friday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Saturday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Saturday", Exchange.InternationalId, DayOfWeek.Saturday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(c_TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Sunday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, "", "Sunday", Exchange.InternationalId, DayOfWeek.Sunday, new TimeOnly(0, 0), new TimeOnly(23, 59)));

        EndTransaction(true);
      }
      catch (Exception)
      {
        EndTransaction(false);
        throw;
      }
    }

    /// <summary>
    /// Drops the database schema.
    /// </summary>
    public void DropSchema()
    {
      //https://sqlite.org/lang_droptable.html
      try
      {
        StartTransaction();

        DropTable(c_TableCountry);
        DropTable(c_TableHoliday);
        DropTable(c_TableExchange);
        DropTable(c_TableSession);
        DropTable(c_TableInstrumentGroup);
        DropTable(c_TableInstrumentGroupInstrument);
        DropTable(c_TableFundamentals);
        DropTable(c_TableInstrument);
        DropTable(c_TableInstrumentSecondaryExchange);
        DropTable(c_TableInstrumentData);
        DropIndex(c_IndexInstrumentData);
        DropTable(c_TableInstrumentDataSynthetic);
        DropIndex(c_IndexInstrumentDataSynthetic);

        //drop the data provider specific tables and indexes
        foreach (var dataProvider in m_configurationService.DataProviders)
        {
          DropTable(GetDataProviderDBName(dataProvider.Value, c_TableFundamentals));
          DropTable(GetDataProviderDBName(dataProvider.Value, c_TableCountryFundamentalAssociations));
          DropTable(GetDataProviderDBName(dataProvider.Value, c_TableCountryFundamentalValues));
          DropTable(GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalAssociations));
          DropTable(GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalValues));
          foreach (Resolution resolution in s_SupportedResolutions)
          {
            DropTable(GetDataProviderDBName(dataProvider.Value, c_TableInstrumentData, resolution));
            DropIndex(GetDataProviderDBName(dataProvider.Value, c_IndexInstrumentData, resolution));
            DropTable(GetDataProviderDBName(dataProvider.Value, c_TableInstrumentDataSynthetic, resolution));
            DropIndex(GetDataProviderDBName(dataProvider.Value, c_IndexInstrumentDataSynthetic, resolution));
          }
        }

        EndTransaction(true);
      }
      catch (Exception)
      {
        EndTransaction(false);
        throw;
      }
    }

    private void CreateCountryTable()
    {
      CreateTable(c_TableCountry,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        IsoCode TEXT
      ");
    }

    private void CreateHolidayTable()
    {
      CreateTable(c_TableHoliday,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        ParentId TEXT,
        Name TEXT,
        HolidayType INTEGER,
        Month INTEGER,
        DayOfMonth INTEGER DEFAULT(-1),
        DayOfWeek INTEGER DEFAULT(-1),
        WeekOfMonth INTEGER DEFAULT(-1),
        MoveWeekendHoliday INTEGER
      ");
    }

    private void CreateExchangeTable()
    {
      CreateTable(c_TableExchange,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        CountryId TEXT,
        Name TEXT,
        TimeZone TEXT,
        LogoId TEXT
      ");
    }

    private void CreateSessionTable()
    {
      CreateTable(c_TableSession,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        Name TEXT,
        ExchangeId TEXT,
        DayOfWeek INTEGER,
        StartTime INTEGER,
        EndTime INTEGER
      ");
    }

    private void CreateInstrumentGroupTable()
    {
      //table to store generic modelling of instruments into hierarchies for analysis
      CreateTable(c_TableInstrumentGroup,
      @"
        Id TEXT,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        ParentId TEXT,
        Name TEXT,
        Description TEXT,
        PRIMARY KEY(Id, ParentId) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentGroupInstrumentTable()
    {
      //table to store instruments associated with a given instrument group
      CreateTable(c_TableInstrumentGroupInstrument,
      @"
        InstrumentGroupId TEXT,
        InstrumentId TEXT,
        PRIMARY KEY(InstrumentGroupId, InstrumentId) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentTable()
    {
      //table of tradeable instruments
      CreateTable(c_TableInstrument,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        Type INTEGER,
        Ticker TEXT,
        Name TEXT,
        Description TEXT,
        PrimaryExchangeId TEXT,
        InceptionDate TYPE INTEGER
      ");
    }

    private void CreateInstrumentSecondaryExchangeTable()
    {
      //table of secondary exchanges on which an instrument is listed
      CreateTable(c_TableInstrumentSecondaryExchange,
      @"
        InstrumentId TEXT,
        ExchangeId TEXT,
        PRIMARY KEY(InstrumentId, ExchangeId) ON CONFLICT REPLACE
      ");
    }

    private void CreateFundamentalsTable()
    {
      //stores the fundamental factor definitions, the values are stored in the instrument fundamentals table
      CreateTable(c_TableFundamentals,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        Name TEXT,
        Description TEXT,
        Category INTEGER,
        ReleaseInterval INTEGER
      ");
    }

    private void CreateCountryFundamentalAssociationTable(string dataProviderName)
    {
      CreateTable(GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations),
      @"
        Id TEXT,
        FundamentalId TEXT,
        CountryId TEXT,
        PRIMARY KEY (Id, FundamentalId, CountryId) ON CONFLICT REPLACE
      ");
    }

    private void CreateCountryFundamentalValuesTable(string dataProviderName)
    {
      //stores the fundamental factor values recorded for a country on a specific date and time
      CreateTable(GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues),
      @"
        AssociationId TEXT,
        DateTime INTEGER,
        Value REAL,
        PRIMARY KEY (AssociationId, DateTime) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentFundamentalAssociationTable(string dataProviderName)
    {
      CreateTable(GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalAssociations),
      @"
        Id TEXT,
        FundamentalId TEXT,
        InstrumentId TEXT,
        PRIMARY KEY (Id, FundamentalId, InstrumentId) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentFundamentalValuesTable(string dataProviderName)
    {
      //stores the fundamental factor values recorded for an instrument on a specific date and time
      CreateTable(GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues),
      @"
        AssociationId TEXT,
        DateTime INTEGER,
        Value REAL,
        PRIMARY KEY (AssociationId, DateTime) ON CONFLICT REPLACE
      ");
    }

    /// <summary>
    /// Checks whether the table definitions need to be created for a specific data provider. We only check whether the bar data table exists for the DataProvider
    /// </summary>
    private bool requireTableDefinitions(string dataProviderName)
    {
      var command = m_connection.CreateCommand();
      command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='$tableName'";
      command.Parameters.AddWithValue("$tableName", GetDataProviderDBName(dataProviderName, c_TableInstrumentData, Resolution.Day));
      SqliteDataReader reader = command.ExecuteReader();
      return !reader.NextResult();
    }

    private void CreateInstrumentDataTable(string dataProviderName, Resolution resolution)
    {
      //stores the bar data that originated from a specific data provider, bar data is indexed to allow fast access to it
      string tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentData, resolution);
      string indexName = GetDataProviderDBName(dataProviderName, c_IndexInstrumentData, resolution);

      switch (resolution)
      {
        case Resolution.Minute:
        case Resolution.Hour:
        case Resolution.Day:
        case Resolution.Week:
        case Resolution.Month:
          CreateTable(tableName,
          @"
            Ticker TEXT,
            DateTime INTEGER,
            Open REAL,
            High REAL,
            Low REAL,
            Close REAL,
            Volume INTEGER,
            PRIMARY KEY(Ticker, DateTime) ON CONFLICT REPLACE
          ");
          CreateIndex(indexName, tableName, true, "Ticker,DateTime");
          break;

        case Resolution.Level1:
          CreateTable(tableName,
          @"
            Ticker TEXT,
            DateTime INTEGER,
            Bid REAL,
            BidSize INTEGER,
            Ask REAL,
            AskSize INTEGER,
            Last REAL,
            LastSize INTEGER,
            PRIMARY KEY(Ticker, DateTime) ON CONFLICT REPLACE
          ");
          CreateIndex(indexName, tableName, true, "Ticker,DateTime");
          break;
      }
    }

    private void CreateInstrumentDataSyntheticTable(string dataProviderName, Resolution resolution)
    {
      //stores the synthetic (generated) bar data that originated from a specific data provider, bar data is indexed to allow fast access to it
      //the purpose of this is that if a specific data provider has missing data that the data provider classes can patch the data with synthetically
      //generated data based on actual data
      //stores the bar data that originated from a specific data provider, bar data is indexed to allow fast access to it
      string tableName = GetDataProviderDBName(dataProviderName, c_TableInstrumentDataSynthetic, resolution);
      string indexName = GetDataProviderDBName(dataProviderName, c_IndexInstrumentDataSynthetic, resolution);

      switch (resolution)
      {
        case Resolution.Minute:
        case Resolution.Hour:
        case Resolution.Day:
        case Resolution.Week:
        case Resolution.Month:
          CreateTable(tableName,
          @"
            Ticker TEXT,
            DateTime INTEGER,
            Open REAL,
            High REAL,
            Low REAL,
            Close REAL,
            Volume INTEGER,
            PRIMARY KEY(Ticker, DateTime) ON CONFLICT REPLACE
          ");
          CreateIndex(indexName, tableName, true, "Ticker,DateTime");
          break;

        case Resolution.Level1:
          CreateTable(tableName,
          @"
            Ticker TEXT,
            DateTime INTEGER,
            Bid REAL,
            BidSize INTEGER,
            Ask REAL,
            AskSize INTEGER,
            Last REAL,
            LastSize INTEGER,
            PRIMARY KEY(Ticker, DateTime) ON CONFLICT REPLACE
          ");
          CreateIndex(indexName, tableName, true, "Ticker,DateTime");
          break;
      }
    }

    /// <summary>
    /// Execute non-query method on the database.
    /// </summary>
    public int ExecuteCommand(string command)
    {
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandText = command;
      return commandObj.ExecuteNonQuery();
    }

    /// <summary>
    /// Execute a database reader on the database.
    /// </summary>
    public SqliteDataReader ExecuteReader(string command)
    {
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandText = command;
      return commandObj.ExecuteReader();
    }

    /// <summary>
    /// Clear the database for testing purposes.
    /// </summary>
    public int ClearDatabase()
    {
      int result = ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableCountry}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableHoliday}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableExchange}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableSession}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableInstrumentGroup}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableInstrumentGroupInstrument}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableInstrument}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableInstrumentSecondaryExchange}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableFundamentals}");
      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, Data.SqliteDataStoreService.c_TableCountryFundamentalValues)}");
        result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues)}");

        foreach (Resolution resolution in SupportedDataResolutions)
        {
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, Data.SqliteDataStoreService.c_TableInstrumentData, resolution)}");
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic, resolution)}");
        }
      }

      return result;
    }
    
    public string SqlSafeString(string value)
    {
      return value.Replace("\'","\'\'");
    }

    public string GetDataProviderDBName(string dataProviderName, string name)
    {
      //NOTE: DataProvider name must be database safe so it can not contain any non-alphanumeric characters and it has to be unique among the set of defined
      //      data providers.
      return dataProviderName + name;
    }

    /// <summary>
    /// Returns the associated database table name used to store candlestick bar data at a given resolution.
    /// </summary>
    public string GetDataProviderDBName(string dataProviderName, string name, Resolution resolution)
    {
      string suffix;
      switch (resolution)
      {
        case Resolution.Minute:
          suffix = c_TableInstrumentDataSuffixMinute;
          break;

        case Resolution.Hour:
          suffix = c_TableInstrumentDataSuffixHour;
          break;

        case Resolution.Day:
          suffix = c_TableInstrumentDataSuffixDay;
          break;

        case Resolution.Week:
          suffix = c_TableInstrumentDataSuffixWeekly;
          break;

        case Resolution.Month:
          suffix = c_TableInstrumentDataSuffixMonthly;
          break;

        case Resolution.Level1:
          suffix = c_TableInstrumentDataSuffixLevel1;
          break;

        default:
          throw new ArgumentException("Unknown data resolution");
      }

      //NOTE: DataProvider name must be database safe so it can not contain any non-alphanumeric characters and it has to be unique among the set of defined
      //      data providers - this is checked in the DataProvider constructor.
      return dataProviderName + name + suffix;
    }

    /// <summary>
    /// Utility method to fascilitate line counting for testing purposes.
    /// </summary>
    public int GetRowCount(string tableName, string where)
    {
      int count = 0;

      var command = m_connection.CreateCommand();
      command.CommandText = $"SELECT * FROM {tableName} WHERE {where}";

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read()) count++;
      }

      return count;
    }

    /// <summary>
    /// Utility method to create database table.
    /// </summary>
    private void CreateTable(string name, string columns)
    {
      //https://sqlite.org/lang_createtable.html
      ExecuteCommand($"CREATE TABLE IF NOT EXISTS {name} ({columns})");
    }

    /// <summary>
    /// Utility method to create a database index on a database table using the given columns.
    /// </summary>
    private void CreateIndex(string indexName, string tableName, bool unique, string columns)
    {
      //https://sqlite.org/lang_createindex.html
      if (unique)
        ExecuteCommand($"CREATE UNIQUE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
      else
        ExecuteCommand($"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
    }

    /// <summary>
    /// Utility method to drop a database table.
    /// </summary>
    private void DropTable(string name)
    {
      //https://sqlite.org/lang_droptable.html
      ExecuteCommand($"DROP TABLE IF EXISTS {name}");
    }

    /// <summary>
    /// Utility method to drop a daa
    /// </summary>
    private void DropIndex(string name)
    {
      //https://sqlite.org/lang_dropindex.html
      ExecuteCommand($"DROP INDEX IF EXISTS {name}");
    }

    /// <summary>
    /// Cache the fundamental associations used for country fundamentals for faster access.
    /// </summary>
    private void CacheCountryFundamentalAssociations(string dataProviderName)
    {
      m_countryFundamentalAssociations.Clear();
      using (var reader = ExecuteReader($"SELECT * FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations)}"))
      {
        m_countryFundamentalAssociations.Remove(dataProviderName);
        AssociationCacheEntry cacheEntry = new AssociationCacheEntry();
        while (reader.Read())
          cacheEntry.Add(reader.GetGuid(1).GetHashCode() + reader.GetGuid(2).GetHashCode(), reader.GetGuid(0));
        m_countryFundamentalAssociations[dataProviderName] = cacheEntry;
      }
    }

    /// <summary>
    /// Cache the fundamental associations used for instrument fundamentals for faster access.
    /// </summary>
    private void CacheInstrumentFundamentalAssociations(string dataProviderName)
    {
      m_instrumentFundamentalAssociations.Clear();
      using (var reader = ExecuteReader($"SELECT * FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalAssociations)}"))
      {
        m_instrumentFundamentalAssociations.Remove(dataProviderName);
        AssociationCacheEntry cacheEntry = new AssociationCacheEntry();
        while (reader.Read())
          cacheEntry.Add(reader.GetGuid(1).GetHashCode() + reader.GetGuid(2).GetHashCode(), reader.GetGuid(0));
        m_instrumentFundamentalAssociations[dataProviderName] = cacheEntry;
      }
    }

    /// <summary>
    /// Returns the association Id for a given fundamental Id and country Id association.
    /// </summary>
    private Guid? GetCountryFundamentalAssociationId(string dataProviderName, Guid fundamentalId, Guid countryId)
    {
      AssociationCacheEntry? cacheEntry;

      int associationHash = fundamentalId.GetHashCode() + countryId.GetHashCode();

      //search for primary value under the given data provider name
      if (m_countryFundamentalAssociations.TryGetValue(dataProviderName, out cacheEntry) && cacheEntry.TryGetValue(fundamentalId.GetHashCode() + countryId.GetHashCode(), out Guid associationId)) 
        return associationId;
      
      //fallback to searching in all data providers
      foreach (var entry in m_countryFundamentalAssociations)
      {
        if (entry.Key == dataProviderName) continue;

        if (entry.Value.TryGetValue(associationHash, out associationId))
        {
          if (cacheEntry != null)
            cacheEntry[associationHash] = associationId;          
          else
          {
            cacheEntry = new AssociationCacheEntry();
            cacheEntry[associationHash] = associationId;
            m_countryFundamentalAssociations[dataProviderName] = cacheEntry;
          }

          return associationId;
        }
      }

      return null;
    }

    /// <summary>
    /// Returns the association Id for a given fundamental Id and instrument Id association.
    /// </summary>
    private Guid? GetInstrumentFundamentalAssociationId(string dataProviderName, Guid fundamentalId, Guid instrumentId)
    {
      AssociationCacheEntry? cacheEntry;

      int associationHash = fundamentalId.GetHashCode() + instrumentId.GetHashCode();

      //search for primary value under the given data provider name
      if (m_instrumentFundamentalAssociations.TryGetValue(dataProviderName, out cacheEntry) && cacheEntry.TryGetValue(associationHash, out Guid associationId)) 
        return associationId;

      //fallback to searching in all data providers
      foreach (var entry in m_instrumentFundamentalAssociations)
      {
        if (entry.Key == dataProviderName) continue;

        if (entry.Value.TryGetValue(associationHash, out associationId))
        {
          if (cacheEntry != null)
            cacheEntry[associationHash] = associationId;
          else
          {
            cacheEntry = new AssociationCacheEntry();
            cacheEntry[associationHash] = associationId;
            m_instrumentFundamentalAssociations[dataProviderName] = cacheEntry;
          }

          return associationId;
        }
      }

      return null;
    }
  }
}