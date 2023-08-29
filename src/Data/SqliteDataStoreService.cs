using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Http.Headers;
using TradeSharp.Common;
using static TradeSharp.Data.IDataStoreService;

namespace TradeSharp.Data
{
  /// <summary>
  /// Data store for Sqlite database.
  /// </summary>
  public class SqliteDataStoreService : IDataStoreService
  {
    //constants
    public const string c_TableLanguageText = "LanguageText";

    public const string c_IndexLanguageText = "ILanguageText";
    public const string c_TableCountry = "Country";
    public const string c_TableHoliday = "Holiday";
    public const string c_TableExchange = "Exchange";
    public const string c_TableExchangeSession = "ExchangeSession";
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
    private string m_databaseFile;
    private string m_connectionString;
    private SqliteConnection m_connection;
    private Dictionary<string, IList<Text>> m_texts;
    private AssociationCache m_countryFundamentalAssociations;
    private AssociationCache m_instrumentFundamentalAssociations;

    //constructors
    public SqliteDataStoreService(IConfigurationService configurationService)
    {
      m_configurationService = configurationService;
      m_databaseFile = "";
      m_connectionString = "";
      m_connection = new SqliteConnection();
      m_texts = new Dictionary<string, IList<Text>>();
      m_countryFundamentalAssociations = new AssociationCache();
      m_instrumentFundamentalAssociations = new AssociationCache();

      //Info on connection strings - https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings
      //Low level interoperability - https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/interop
      //Sqlite3 objects, functions - https://sqlite.org/c3ref/objlist.html, https://sqlite.org/c3ref/funclist.html

      //validate database type and setup the database connection
      IConfigurationService.DataStoreConfiguration dataStoreConfiguration = (IConfigurationService.DataStoreConfiguration)m_configurationService.General[IConfigurationService.GeneralConfiguration.DataStore];
      Trace.Assert(dataStoreConfiguration.Typename != this.GetType().Name, $"Incorrect data store \"{this.GetType().Name}\" instatiated against data store configuration \"{dataStoreConfiguration.Typename}\"");
      m_databaseFile = dataStoreConfiguration.ConnectionString;

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
      LoadTexts();
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

    public Guid CreateText(string isoLang, string text)
    {
      Guid id = Guid.NewGuid();
      string escapedText = text.Replace("'", "''");
      ExecuteCommand($"INSERT INTO {c_TableLanguageText} VALUES ('{id.ToString()}', '{isoLang}', '{escapedText}')");
      return id;
    }

    public void CreateCountry(IDataStoreService.Country country)
    {
      ExecuteCommand($"INSERT OR REPLACE INTO {c_TableCountry} VALUES('{country.Id.ToString()}', '{country.IsoCode}')");
    }

    public void CreateHoliday(IDataStoreService.Holiday holiday)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableHoliday} (Id, ParentId, NameTextId, HolidayType, Month, DayOfMonth, WeekOfMonth, DayOfWeek, MoveWeekendHoliday) " +
          $"VALUES (" +
            $"'{holiday.Id.ToString()}', " +
            $"'{holiday.ParentId.ToString()}', " +
            $"'{holiday.NameTextId.ToString()}', " +
            $"{(int)holiday.Type}, " +
            $"{(int)holiday.Month}, " +
            $"{(int)holiday.DayOfMonth}, " +
            $"{(int)holiday.WeekOfMonth}, " +
            $"{(int)holiday.DayOfWeek}, " +
            $"{(int)holiday.MoveWeekendHoliday}" +
          ")"
      );
    }

    public void CreateExchange(IDataStoreService.Exchange exchange)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableExchange} (Id, CountryId, NameTextId, TimeZone) " +
          $"VALUES (" +
            $"'{exchange.Id.ToString()}', " +
            $"'{exchange.CountryId.ToString()}', " +
            $"'{exchange.NameTextId.ToString()}', " +
            $"'{exchange.TimeZone.ToSerializedString()}'" +
          $")"
      );
    }

    public void CreateSession(IDataStoreService.Session session)
    {
      ExecuteCommand(
      $"INSERT OR REPLACE INTO {c_TableExchangeSession} (Id, NameTextId, ExchangeId, DayOfWeek, StartTime, EndTime) " +
        $"VALUES (" +
          $"'{session.Id.ToString()}', " +
          $"'{session.NameTextId.ToString()}', " +
          $"'{session.ExchangeId.ToString()}', " +
          $"{(int)session.DayOfWeek}, " +
          $"{session.Start.Ticks}, " +
          $"{session.End.Ticks}" +
        $")"
      );
    }

    public void CreateCountryFundamental(ref IDataStoreService.CountryFundamental fundamental)
    {
      fundamental.AssociationId = Guid.NewGuid();

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

    public void CreateInstrumentFundamental(ref IDataStoreService.InstrumentFundamental fundamental)
    {
      fundamental.AssociationId = Guid.NewGuid();

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

    public void CreateInstrumentGroup(IDataStoreService.InstrumentGroup instrumentGroup)
    {
      ExecuteCommand(
      $"INSERT OR REPLACE INTO {c_TableInstrumentGroup} (Id, ParentId, NameTextId, DescriptionTextId) " +
        $"VALUES (" +
          $"'{instrumentGroup.Id.ToString()}', " +
          $"'{instrumentGroup.ParentId.ToString()}', " +
          $"'{instrumentGroup.NameTextId.ToString()}', " +
          $"'{instrumentGroup.DescriptionTextId.ToString()}'" +
        $")"
      );

      foreach (Guid instrumentId in instrumentGroup.Instruments) CreateInstrumentGroupInstrument(instrumentGroup.Id, instrumentId);
    }

    public void CreateInstrument(IDataStoreService.Instrument instrument)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableInstrument} (Id, Type, Ticker, NameTextId, DescriptionTextId, PrimaryExchangeId, InceptionDate) " +
          $"VALUES (" +
            $"'{instrument.Id.ToString()}', " +
            $"{(int)instrument.Type}, " +
            $"'{instrument.Ticker}', " +
            $"'{instrument.NameTextId.ToString()}', " +
            $"'{instrument.DescriptionTextId.ToString()}', " +
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

    public void CreateInstrument(Guid instrumentId, Guid exchangeId)
    {
      ExecuteCommand(
        $"INSERT OR IGNORE INTO {c_TableInstrumentSecondaryExchange} (InstrumentId, ExchangeId) " +
          $"VALUES (" +
            $"'{instrumentId.ToString()}', " +
            $"'{exchangeId.ToString()}' " +
          $")"
      );
    }

    public void CreateFundamental(IDataStoreService.Fundamental fundamental)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {c_TableFundamentals} (Id, NameTextId, DescriptionTextId, Category, ReleaseInterval)" +
          $"VALUES (" +
            $"'{fundamental.Id.ToString()}', " +
            $"'{fundamental.NameTextId.ToString()}', " +
            $"'{fundamental.DescriptionTextId.ToString()}', " +
            $"{(int)fundamental.Category}, " +
            $"{(int)fundamental.ReleaseInterval}" +
        $")"
      );
    }

    public void UpdateSession(Guid id, DayOfWeek day, TimeOnly start, TimeOnly end)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableExchangeSession} " +
          $"SET DayOfWeek = {(int)day}, " +
              $"StartTime = {start.Ticks}, " +
              $"EndTime = {end.Ticks} " +
          $"WHERE Id = '{id.ToString()}'"
      );
    }

    public void UpdateInstrument(Guid id, Guid exchangeId, string ticker, DateTime inceptionDate)
    {
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableInstrument} " +
          $"SET Ticker = '{ticker}', " +
              $"PrimaryExchangeId = '{exchangeId.ToString()}', " +
              $"InceptionDate = {inceptionDate.ToUniversalTime().ToBinary()} " +
          $"WHERE Id = '{id.ToString()}'"
      );
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

    public void UpdateInstrumentGroup(Guid id, Guid parentId)
    {
      //Id and ParentId must be unique in combination
      ExecuteCommand(
        $"UPDATE OR FAIL {c_TableInstrumentGroup} SET " +
            $"ParentId = '{parentId.ToString()}' " +
          $"WHERE Id = '{id.ToString()}'"
      );
    }

    public void UpdateInstrumentGroup(Guid id, string name, string description)
    {
      Guid? nameTextId = GetNameTextId(c_TableInstrumentGroup, id);
      Guid? descriptionTextId = GetDescriptionTextId(c_TableInstrumentGroup, id);

      if (nameTextId == null || descriptionTextId == null) throw new ArgumentException($"Instrument '{id}' does not have an associated name and/or description text id.");

      UpdateText(nameTextId.Value, m_configurationService.CultureInfo.ThreeLetterISOLanguageName, name);
      UpdateText(descriptionTextId.Value, m_configurationService.CultureInfo.ThreeLetterISOLanguageName, description);
    }

    public void UpdateStockGroups(Guid id, Guid parentSectorId, Guid parentIndustryGroupId, Guid parentIndustryId, Guid parentSubIndustryId)
    {
      throw new NotImplementedException();
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

    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, Resolution resolution, IDataStoreService.BarData bars)
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

    public void UpdateData(string dataProviderName, Guid instrumentId, string ticker, IDataStoreService.Level1Data level1Data)
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

    public int DeleteText(Guid id)
    {
      return ExecuteCommand($"DELETE FROM {c_TableLanguageText} WHERE Id = '{id.ToString()}'");
    }

    public int DeleteText(Guid id, string isoLang)
    {
      return ExecuteCommand($"DELETE FROM {c_TableLanguageText} WHERE Id = '{id.ToString()}' AND IsoLang = '{isoLang}'");
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
        result += Delete(GetDataProviderDBName(dataProvider.Value, c_TableCountryFundamentalAssociations), id, null, null, "CountryId");
        CacheCountryFundamentalAssociations(dataProvider.Value);
      }

      return result;
    }

    public int DeleteHoliday(Guid id)
    {
      return Delete(c_TableHoliday, id, GetNameTextId(c_TableHoliday, id));
    }

    public int DeleteExchange(Guid id)
    {
      int result = Delete(c_TableExchange, id, GetNameTextId(c_TableExchange, id));
      foreach (var holidayId in GetAssociatedIds(c_TableHoliday, id, "ParentId")) result += DeleteHoliday(holidayId);
      using (var instrumentRows = GetAssociatedRows(c_TableInstrument, id, "PrimaryExchangeId", "Id, Ticker"))
        while (instrumentRows.Read()) result += DeleteInstrument(instrumentRows.GetGuid(0), instrumentRows.GetString(1));
      result += Delete(c_TableInstrumentSecondaryExchange, id, null, null, "ExchangeId");
      foreach (var sessionId in GetAssociatedIds(c_TableExchangeSession, id, "ExchangeId")) result += DeleteSession(sessionId);
      return result;
    }

    public int DeleteSession(Guid id)
    {
      return Delete(c_TableExchangeSession, id, GetNameTextId(c_TableExchangeSession, id));
    }

    public int DeleteInstrument(Guid id, string ticker)
    {
      int result = 0;

      result = Delete(c_TableInstrument, id, GetNameTextId(c_TableInstrument, id), GetDescriptionTextId(c_TableInstrument, id));
      result += Delete(c_TableInstrumentSecondaryExchange, id, null, null, "InstrumentId");
      result += Delete(c_TableInstrumentGroupInstrument, id, null, null, "InstrumentId");

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalAssociations)} WHERE InstrumentId = '{id.ToString()}'"))
          while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");
        result += Delete(GetDataProviderDBName(dataProvider.Value, c_TableInstrumentFundamentalAssociations), id, null, null, "InstrumentId");
        result += deleteData(dataProvider.Value, ticker, null);
        CacheInstrumentFundamentalAssociations(dataProvider.Value);
      }

      return result;
    }

    public int DeleteInstrumentFromExchange(Guid instrumentId, Guid exchangeId)
    {
      return ExecuteCommand($"DELETE FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{instrumentId.ToString()}' AND ExchangeId = '{exchangeId.ToString()}'");
    }

    public void DeleteInstrumentGroup(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT Id FROM {c_TableInstrumentGroup} WHERE ParentId = '{id.ToString()}'"))
        while (reader.Read()) DeleteInstrumentGroup(reader.GetGuid(0));

      ExecuteCommand($"DELETE FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{id.ToString()}'");
      ExecuteCommand($"DELETE FROM {c_TableInstrumentGroup} WHERE Id = '{id.ToString()}'");
    }

    public void DeleteInstrumentGroupChild(Guid parentId, Guid childId)
    {
      //NOTE: We do not use the parentId for Sqlite, we just reset the parentId on the instrument group table for the given child.
      ExecuteCommand($"UPDATE OR IGNORE {c_TableInstrumentGroup} SET ParentId = '{InstrumentGroupRoot.Instance.Id.ToString()}' WHERE Id = '{childId.ToString()}'");
    }

    public void DeleteInstrumentGroupInstrument(Guid instrumentGroupId, Guid instrumentId)
    {
      ExecuteCommand($"DELETE FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{instrumentGroupId.ToString()}' AND InstrumentId = '{instrumentId.ToString()}'");
    }

    public int DeleteFundamental(Guid id)
    {
      int result = 0;

      result = DeleteFundamentalValues(id);
      result += Delete(c_TableFundamentals, id, GetNameTextId(c_TableFundamentals, id), GetDescriptionTextId(c_TableFundamentals, id));

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

    public int DeleteCountryFundamental(string dataProviderName, Guid fundamentalId, Guid countryId)
    {
      Guid? associationId = GetCountryFundamentalAssociationId(dataProviderName, fundamentalId, countryId);
      if (!associationId.HasValue) return 0;

      int result = ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations)} WHERE Id = '{associationId.ToString()}'");
      result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}'");

      CacheCountryFundamentalAssociations(dataProviderName);

      return result;
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

    public int DeleteCountryFundamentalValue(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime)
    {
      Guid? associationId = GetCountryFundamentalAssociationId(dataProviderName, fundamentalId, countryId);

      if (!associationId.HasValue) return 0;  //no association, nothing to remove

      return ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}' AND DateTime = {dateTime.ToUniversalTime().ToBinary()}"); ;
    }

    public int DeleteInstrumentFundamentalValue(string dataProviderName, Guid fundamentalId, Guid instrumentId, DateTime dateTime)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentId);

      if (!associationId.HasValue) return 0;  //no association, nothing to remove

      return ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}' AND DateTime = {dateTime.ToUniversalTime().ToBinary()}");
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

    public IList<Text> GetTexts()
    {
      var result = new List<Text>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableLanguageText}"))
        while (reader.Read()) result.Add(new Text(reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));

      return result;
    }

    public IList<Text> GetTexts(string isoLang)
    {
      var result = new List<Text>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableLanguageText} WHERE IsoLang = '{isoLang}'"))
        while (reader.Read()) result.Add(new Text(reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));

      return result;
    }

    public string GetText(Guid id, string isoLang)
    {
      string result = "";

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableLanguageText} WHERE Id = '{id.ToString()}' AND IsoLang = '{isoLang}'"))
        if (reader.Read()) result = reader.GetString(2);

      if (result.Length != 0)
        return result;

      return TradeSharp.Common.Resources.NoTextAvailable;
    }

    public IList<IDataStoreService.Country> GetCountries()
    {
      var result = new List<IDataStoreService.Country>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableCountry}"))
        while (reader.Read()) result.Add(new IDataStoreService.Country(reader.GetGuid(0), reader.GetString(1)));

      return result;
    }

    public IList<IDataStoreService.Holiday> GetHolidays()
    {
      var result = new List<IDataStoreService.Holiday>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableHoliday}"))
        while (reader.Read())
        {
          Guid nameTextId = reader.GetGuid(2);
          result.Add(new IDataStoreService.Holiday(reader.GetGuid(0), reader.GetGuid(1), nameTextId, GetText(nameTextId), (HolidayType)reader.GetInt64(3), (Months)reader.GetInt64(4), reader.GetInt32(5), (DayOfWeek)reader.GetInt64(6), (WeekOfMonth)reader.GetInt64(7), (MoveWeekendHoliday)reader.GetInt64(8))); ;
        }

      return result;
    }

    public IList<IDataStoreService.Exchange> GetExchanges()
    {
      var result = new List<IDataStoreService.Exchange>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableExchange}"))
        while (reader.Read())
        {
          Guid nameTextId = reader.GetGuid(2);
          result.Add(new IDataStoreService.Exchange(reader.GetGuid(0), reader.GetGuid(1), nameTextId, GetText(nameTextId), TimeZoneInfo.FromSerializedString(reader.GetString(3))));
        }

      return result;
    }

    public IList<IDataStoreService.Session> GetSessions()
    {
      var result = new List<IDataStoreService.Session>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableExchangeSession}"))
        while (reader.Read())
        {
          Guid nameTextId = reader.GetGuid(1);
          result.Add(new IDataStoreService.Session(reader.GetGuid(0), nameTextId, GetText(nameTextId), reader.GetGuid(2), (DayOfWeek)reader.GetInt32(3), new TimeOnly(reader.GetInt64(4)), new TimeOnly(reader.GetInt64(5))));
        }

      return result;
    }

    public IList<IDataStoreService.Instrument> GetInstruments()
    {
      var result = new List<IDataStoreService.Instrument>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrument}"))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          Guid instrumentId = reader.GetGuid(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{instrumentId.ToString()}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          List<Guid> instrumentGroupIds = new List<Guid>();

          using (var instrumentGroupReader = ExecuteReader($"SELECT InstrumentGroupId FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentId = '{instrumentId.ToString()}'"))
            while (instrumentGroupReader.Read()) instrumentGroupIds.Add(instrumentGroupReader.GetGuid(0));

          Guid nameTextId = reader.GetGuid(3);
          Guid descriptionTextId = reader.GetGuid(4);
          result.Add(new IDataStoreService.Instrument(instrumentId, (InstrumentType)reader.GetInt32(1), reader.GetString(2), nameTextId, GetText(nameTextId), descriptionTextId, GetText(descriptionTextId), DateTime.FromBinary(reader.GetInt64(6)), instrumentGroupIds, reader.GetGuid(5), secondaryExchangeIds));
        }

      return result;
    }

    public IList<IDataStoreService.Instrument> GetInstruments(InstrumentType instrumentType)
    {
      var result = new List<IDataStoreService.Instrument>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrument} WHERE Type = {(int)instrumentType}"))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          Guid instrumentId = reader.GetGuid(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {c_TableInstrumentSecondaryExchange} WHERE InstrumentId = '{instrumentId.ToString()}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          List<Guid> instrumentGroupIds = new List<Guid>();

          using (var instrumentGroupReader = ExecuteReader($"SELECT InstrumentGroupId FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentId = '{instrumentId.ToString()}'"))
            while (instrumentGroupReader.Read()) instrumentGroupIds.Add(instrumentGroupReader.GetGuid(0));

          Guid nameTextId = reader.GetGuid(3);
          Guid descriptionTextId = reader.GetGuid(4);
          result.Add(new IDataStoreService.Instrument(instrumentId, (InstrumentType)reader.GetInt32(1), reader.GetString(2), nameTextId, GetText(nameTextId), descriptionTextId, GetText(descriptionTextId), DateTime.FromBinary(reader.GetInt64(6)), instrumentGroupIds, reader.GetGuid(5), secondaryExchangeIds));
        }

      return result;
    }

    public IList<IDataStoreService.Fundamental> GetFundamentals()
    {
      var result = new List<IDataStoreService.Fundamental>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableFundamentals}"))
        while (reader.Read())
        {
          Guid nameTextId = reader.GetGuid(1);
          Guid descriptionTextId = reader.GetGuid(2);
          result.Add(new IDataStoreService.Fundamental(reader.GetGuid(0), nameTextId, GetText(nameTextId), descriptionTextId, GetText(descriptionTextId), (FundamentalCategory)reader.GetInt32(3), (FundamentalReleaseInterval)reader.GetInt32(4)));
        }

      return result;
    }

    public IList<IDataStoreService.CountryFundamental> GetCountryFundamentals(string dataProviderName)
    {
      var result = new List<IDataStoreService.CountryFundamental>();

      //load basic fundamental and country associations
      string dataProviderAssociationTable = GetDataProviderDBName(dataProviderName, c_TableCountryFundamentalAssociations);
      string selectQuery =
        $"SELECT * FROM {dataProviderAssociationTable} " +
          $"INNER JOIN {c_TableFundamentals} ON {dataProviderAssociationTable}.FundamentalId = {c_TableFundamentals}.Id " +
          $"WHERE {c_TableFundamentals}.Category = {(int)FundamentalCategory.Country} " +
          $"ORDER BY {dataProviderAssociationTable}.FundamentalId ASC, {dataProviderAssociationTable}.CountryId ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read()) result.Add(new IDataStoreService.CountryFundamental(dataProviderName, reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2)));

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

    public IList<IDataStoreService.InstrumentFundamental> GetInstrumentFundamentals(string dataProviderName)
    {
      var result = new List<IDataStoreService.InstrumentFundamental>();

      //load basic fundamental and instrument associations
      string dataProviderAssociationTable = GetDataProviderDBName(dataProviderName, c_TableInstrumentFundamentalAssociations);
      string selectQuery =
        $"SELECT * FROM {dataProviderAssociationTable} " +
          $"INNER JOIN {c_TableFundamentals} ON {dataProviderAssociationTable}.FundamentalId = {c_TableFundamentals}.Id " +
          $"WHERE {c_TableFundamentals}.Category = {(int)FundamentalCategory.Instrument} " +
          $"ORDER BY {dataProviderAssociationTable}.FundamentalId ASC, {dataProviderAssociationTable}.InstrumentId ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read()) result.Add(new IDataStoreService.InstrumentFundamental(dataProviderName, reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2)));

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

    public IList<IDataStoreService.InstrumentGroup> GetInstrumentGroups()
    {
      List<IDataStoreService.InstrumentGroup> result = new List<IDataStoreService.InstrumentGroup>();

      using (var reader = ExecuteReader($"SELECT * FROM {c_TableInstrumentGroup}"))
      {
        while (reader.Read())
        {
          Guid id = reader.GetGuid(0);
          IList<Guid> instruments = GetInstrumentGroupInstruments(id);
          Guid nameTextId = reader.GetGuid(2);
          Guid descriptionTextId = reader.GetGuid(3);
          result.Add(new IDataStoreService.InstrumentGroup(id, reader.GetGuid(1), nameTextId, GetText(nameTextId), descriptionTextId, GetText(descriptionTextId), instruments));
        }
      }

      return result;
    }

    public IList<Guid> GetInstrumentGroupInstruments(Guid instrumentGroupId)
    {
      List<Guid> result = new List<Guid>();

      using (var reader = ExecuteReader($"SELECT InstrumentId FROM {c_TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{instrumentGroupId.ToString()}'"))
        while (reader.Read()) result.Add(reader.GetGuid(0));

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
            $"ORDER BY DateTime";

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
            $"ORDER BY DateTime";

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
            $"ORDER BY DateTime";

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
            $"ORDER BY DateTime";

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
    protected int Delete(string tableName, Guid id, Guid? nameTextId = null, Guid? descriptionTextId = null, string idFieldname = "Id")
    {
      int rowsDeleted = ExecuteCommand($"DELETE FROM {tableName} WHERE {idFieldname} = '{id.ToString()}'");

      //delete associated texts if main delete was successfull and we have id's to work with
      if (rowsDeleted > 0 && nameTextId.HasValue) rowsDeleted += DeleteText(nameTextId.Value);
      if (rowsDeleted > 0 && descriptionTextId.HasValue) rowsDeleted += DeleteText(descriptionTextId.Value);

      return rowsDeleted;
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
        CreateLanguageTextTable();
        CreateCountryTable();
        CreateHolidayTable();
        CreateExchangeTable();
        CreateExchangeSessionTable();
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
    /// Drops the database schema.
    /// </summary>
    public void DropSchema()
    {
      //https://sqlite.org/lang_droptable.html

      try
      {
        StartTransaction();

        DropTable(c_TableLanguageText);
        DropIndex(c_IndexLanguageText);
        DropTable(c_TableCountry);
        DropTable(c_TableHoliday);
        DropTable(c_TableExchange);
        DropTable(c_TableExchangeSession);
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

    /// <summary>
    /// Create the language text table to supports multiple languages.
    /// </summary>
    private void CreateLanguageTextTable()
    {
      //defines a string table for translatable texts used for any text data entered into the database
      //once an entry is created only the text can be updated (conflict fail on IsoLang)
      CreateTable(c_TableLanguageText,
      @"
        Id TEXT,
        IsoLang TEXT,
        Value TEXT,
        PRIMARY KEY(Id, IsoLang) ON CONFLICT REPLACE
      ");
      CreateIndex(c_IndexLanguageText, c_TableLanguageText, true, "Id,IsoLang");
    }

    private void CreateCountryTable()
    {
      CreateTable(c_TableCountry,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        IsoCode TEXT
      ");
    }

    private void CreateHolidayTable()
    {
      CreateTable(c_TableHoliday,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        ParentId TEXT,
        NameTextId TEXT,
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
        CountryId TEXT,
        NameTextId TEXT,
        TimeZone TEXT
      ");
    }

    private void CreateExchangeSessionTable()
    {
      CreateTable(c_TableExchangeSession,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        NameTextId TEXT,
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
        ParentId TEXT,
        NameTextId TEXT,
        DescriptionTextId TEXT,
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
        Type INTEGER,
        Ticker TEXT,
        NameTextId TEXT,
        DescriptionTextId TEXT,
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
        NameTextId TEXT,
        DescriptionTextId TEXT,
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
      int result = ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableLanguageText}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableCountry}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableHoliday}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableExchange}");
      result += ExecuteCommand($"DELETE FROM {Data.SqliteDataStoreService.c_TableExchangeSession}");
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

    ///// <summary>
    ///// Generate unique table name from a data provider type name.
    ///// </summary>
    //protected string uniqueTableName(string dataProviderName)
    //{
    //  //currently only use the final part of the data provider type name as the table name since in most cases
    //  //this should be unique enough - a hash could be generated from the type name to make sure it is always unique based
    //  //on the namespace as well
    //  string typename = dataProviderName;
    //  if (typename.Contains(",")) typename = typename.Substring(0, typename.IndexOf(","));
    //  string[] nameComponents = typename.Split('.');
    //  return nameComponents[nameComponents.Length - 1];
    //}

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

    public void UpdateText(Guid id, string isoLang, string value)
    {
      string escapedText = value.Replace("'", "''");
      ExecuteCommand($"INSERT OR REPLACE INTO {c_TableLanguageText} (Id, IsoLang, Value) VALUES ('{id.ToString()}', '{isoLang}', '{escapedText}')");
    }

    private void LoadTexts()
    {
      foreach (var culture in m_configurationService.CultureFallback)
        if (culture.ThreeLetterISOLanguageName != CultureInfo.InvariantCulture.ThreeLetterISOLanguageName && !m_texts.ContainsKey(culture.ThreeLetterISOLanguageName))
          m_texts.Add(culture.ThreeLetterISOLanguageName, GetTexts(culture.ThreeLetterISOLanguageName));
    }

    public string GetText(Guid id)
    {
      //try to find the text according to the set of culture fallbacks
      foreach (var culture in m_configurationService.CultureFallback)
        foreach (var text in m_texts[culture.ThreeLetterISOLanguageName])
          if (text.Id == id) return text.Value;

      //try to find text in ANY language
      using (var reader = ExecuteReader($"SELECT Value FROM {c_TableLanguageText} WHERE Id = '{id}' LIMIT 1"))
        if (reader.Read()) return reader.GetString(0);

      return TradeSharp.Common.Resources.NoTextAvailable;
    }

    /// <summary>
    /// Retrieves the name text Id for a given ObjectWithId in a specific database table.
    /// </summary>
    private Guid? GetNameTextId(string tableName, Guid id)
    {
      var command = m_connection.CreateCommand();
      command.CommandText = $"SELECT NameTextId FROM {tableName} WHERE Id = '{id.ToString()}'";

      using (var reader = command.ExecuteReader())
        if (reader.Read()) return reader.GetGuid(0);

      return null;
    }

    /// <summary>
    /// Retrieves the description text Id for a given ObjectWithId in a specific database table.
    /// </summary>
    private Guid? GetDescriptionTextId(string tableName, Guid id)
    {
      var command = m_connection.CreateCommand();
      command.CommandText = $"SELECT DescriptionTextId FROM {tableName} WHERE Id = '{id.ToString()}'";

      using (var reader = command.ExecuteReader())
        if (reader.Read()) return reader.GetGuid(0);

      return null;
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