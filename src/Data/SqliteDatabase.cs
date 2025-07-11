﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Xml.Linq;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Data store for Sqlite database.
  /// </summary>
  public partial class SqliteDatabase : SqlLiteBase, IDatabase
  {
    //constants
    public const string TableCountry = "Country";
    public const string TableHoliday = "Holiday";
    public const string TableExchange = "Exchange";
    public const string TableSession = "ExchangeSession";
    public const string TableInstrumentGroup = "InstrumentGroup";
    public const string TableInstrumentGroupInstrument = "InstrumentGroupInstrument";
    public const string TableInstrument = "Instrument";
    public const string TableInstrumentSecondaryExchange = "InstrumentSecondaryExchange";
    public const string TableStock = "Stock";
    public const string TableFundamentals = "Fundamentals";
    public const string TableCountryFundamentalAssociations = "CountryFundamentalAssociations";
    public const string TableCountryFundamentalValues = "CountryFundamentalValues";
    public const string TableInstrumentFundamentalAssociations = "InstrumentFundamentalAssociations";
    public const string TableInstrumentFundamentalValues = "InstrumentFundamentalValues";
    public const string TableInstrumentData = "Data";
    public const string IndexInstrumentData = "IData";

    public const string TableInstrumentDataSuffixMinute = "M1";
    public const string TableInstrumentDataSuffixHour = "H1";
    public const string TableInstrumentDataSuffixDay = "D";
    public const string TableInstrumentDataSuffixWeekly = "W";
    public const string TableInstrumentDataSuffixMonthly = "M";
    public const string TableInstrumentDataSuffixLevel1 = "L1";

    //TODO: Add L2 tables if required - to store the order book would require a lot of space.

    public static readonly Resolution[] s_SupportedResolutions = { Resolution.Minutes, Resolution.Hours, Resolution.Days, Resolution.Weeks, Resolution.Months, Resolution.Level1 };

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
    private AssociationCache m_countryFundamentalAssociations;
    private AssociationCache m_instrumentFundamentalAssociations;

    //constructors
    public SqliteDatabase(IConfigurationService configurationService, ILogger<SqliteDatabase> logger) : base(logger)
    {
      m_configurationService = configurationService;
      m_logger = logger;
      m_databaseFile = "";
      m_connectionString = "";
      m_countryFundamentalAssociations = new AssociationCache();
      m_instrumentFundamentalAssociations = new AssociationCache();
      IsOptimizing = false;

      //Info on connection strings - https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings
      //Low level interoperability - https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/interop
      //Sqlite3 objects, functions - https://sqlite.org/c3ref/objlist.html, https://sqlite.org/c3ref/funclist.html

      //validate database type and setup the database connection
      IDataStoreConfiguration dataStoreConfiguration = (IDataStoreConfiguration)m_configurationService.General[IConfigurationService.GeneralConfiguration.Database];
      Trace.Assert(dataStoreConfiguration.Type != this.GetType().Name, $"Incorrect data store \"{this.GetType().Name}\" instatiated against data store configuration \"{dataStoreConfiguration.Type}\"");
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

      //optimize database as best possible - this makes the DB very slow so rather use a once off optimization.
      //ExecuteCommand("PRAGMA auto_vacuum=1");   //perform cleanup after each commit (slows down the database) - https://sqlite.org/pragma.html#pragma_auto_vacuum

      //create the data store schema
      CreateSchema();

      //create static required objects
      CreateDefaultObjects();
    }

    //finalizers
    ~SqliteDatabase()
    {
      m_connection.Close();
    }

    //interface implementations
    public void Optimize()
    {
      if (IsOptimizing)
      {
        if (Debugging.DatabaseCalls) m_logger.LogWarning("Database is already optimizing, skipping this call.");
        return;
      }

      IsOptimizing = true;
      ExecuteCommand("VACUUM");
      IsOptimizing = false;
    }

    public void CreateCountry(Country country)
    {
      lock (this) ExecuteCommand($"INSERT OR REPLACE INTO {TableCountry} VALUES('{country.Id.ToString()}', {(long)country.AttributeSet}, '{ToSqlSafeString(country.TagStr)}','{country.IsoCode}')");
    }

    public void UpdateCountry(Country country)
    {
      lock (this)
      {
        ExecuteCommand($"INSERT OR REPLACE INTO {TableCountry} VALUES('{country.Id.ToString()}', {(long)country.AttributeSet}, '{ToSqlSafeString(country.TagStr)}','{country.IsoCode}')");

        ExecuteCommand(
          $"UPDATE OR FAIL {TableExchange} " +
            $"SET AttributeSet = {(long)country.AttributeSet}, " +
                $"Tag = '{ToSqlSafeString(country.TagStr)}', " +
            //$"IsoCode = '{country.IsoCode}' " +   //no update of the IsoCode, is only set on creation
            $"WHERE Id = '{country.Id.ToString()}'"
          );
      }
    }

    public Country? GetCountry(Guid id)
    {
      Country? result = null;

      using (var reader = ExecuteReader($"SELECT * FROM {TableCountry} WHERE Id = '{id.ToString()}'"))
        if (reader.Read()) result = new Country(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3));

      return result;
    }


    public IList<Country> GetCountries()
    {
      var result = new List<Country>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableCountry} ORDER BY IsoCode ASC"))
        while (reader.Read()) result.Add(new Country(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3)));

      return result;
    }

    public int DeleteCountry(Guid id)
    {
      int result = Delete(TableCountry, id);
      foreach (var holidayId in GetAssociatedIds(TableHoliday, id, "ParentId")) result += DeleteHoliday(holidayId);
      foreach (var exchangeId in GetAssociatedIds(TableExchange, id, "CountryId")) result += DeleteExchange(exchangeId);

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProvider.Key, TableCountryFundamentalAssociations)} WHERE CountryId = '{id.ToString()}'"))
          while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Key, TableCountryFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");
        result += Delete(GetDataProviderDBName(dataProvider.Key, TableCountryFundamentalAssociations), id, "CountryId");
        CacheCountryFundamentalAssociations(dataProvider.Key);
      }

      return result;
    }

    public void CreateExchange(Exchange exchange)
    {
      ExecuteCommand(
        $"INSERT OR REPLACE INTO {TableExchange} (Id, AttributeSet, Tag, CountryId, Name, TimeZone, LogoId, DefaultPriceDecimals, DefaultMinimumMovement, DefaultBigPointValue, Url, AlternateNames) " +
          $"VALUES (" +
            $"'{exchange.Id.ToString()}', " +
            $"{(long)exchange.AttributeSet}, " +
            $"'{ToSqlSafeString(exchange.TagStr)}', " +
            $"'{exchange.CountryId.ToString()}', " +
            $"'{ToSqlSafeString(exchange.Name)}', " +
            $"'{exchange.TimeZone.ToSerializedString()}', " +
            $"'{exchange.LogoId.ToString()}', " +
            $"{exchange.DefaultPriceDecimals}, " +
            $"{exchange.DefaultMinimumMovement}, " +
            $"{exchange.DefaultBigPointValue}, " +
            $"'{ToSqlSafeString(exchange.Url)}', " +
            $"'{ToSqlSafeString(Common.Utilities.ToCsv(exchange.AlternateNames))}'" +
          $")"
      );
    }

    public Exchange? GetExchange(Guid id)
    {
      Exchange? result = null;

      using (var reader = ExecuteReader($"SELECT * FROM {TableExchange} WHERE Id = '{id.ToString()}'"))
        if (reader.Read())
          result = new Exchange(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), TimeZoneInfo.FromSerializedString(reader.GetString(5)), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetGuid(6), reader.GetString(10));

      return result;
    }

    public IList<Exchange> GetExchanges()
    {
      var result = new List<Exchange>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableExchange} ORDER BY Name ASC"))
        while (reader.Read())
          result.Add(new Exchange(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), TimeZoneInfo.FromSerializedString(reader.GetString(5)), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetGuid(6), reader.GetString(10)));

      return result;
    }

    public void UpdateExchange(Exchange exchange)
    {
      lock (this)
        ExecuteCommand(
          $@"INSERT OR REPLACE INTO {TableExchange}
            (Id,CountryId,Name,AttributeSet,Tag,TimeZone,LogoId,DefaultPriceDecimals,DefaultMinimumMovement,DefaultBigPointValue,Url,AlternateNames)
            VALUES(
              '{exchange.Id.ToString()}',
              '{exchange.CountryId.ToString()}',
              '{ToSqlSafeString(exchange.Name)}',
              {(long)exchange.AttributeSet},
              '{ToSqlSafeString(exchange.TagStr)}',
              '{exchange.TimeZone.ToSerializedString()}',
              '{exchange.LogoId}',
              {exchange.DefaultPriceDecimals},
              {exchange.DefaultMinimumMovement},
              {exchange.DefaultBigPointValue},
              '{ToSqlSafeString(exchange.Url)}',
              '{ToSqlSafeString(Common.Utilities.ToCsv(exchange.AlternateNames))}'
            )"
        );
    }

    public int DeleteExchange(Guid id)
    {
      //get the exchange logo Id to delete the associated logo file
      Guid logoId = Guid.Empty;
      SqliteDataReader reader = ExecuteReader($"SELECT logoId FROM {TableExchange} WHERE Id = '{id.ToString()}'");
      if (reader.Read()) logoId = reader.GetGuid(0);

      //delete header entry
      int result = Delete(TableExchange, id);

      //delete logo image file
      string logoFilename = Exchange.GetLogoPath(logoId);
      if (logoFilename != Exchange.BlankLogoPath) File.Delete(logoFilename);

      //delete rest of the associated objects
      foreach (var holidayId in GetAssociatedIds(TableHoliday, id, "ParentId")) result += DeleteHoliday(holidayId);
      using (var instrumentRows = GetAssociatedRows(TableInstrument, id, "PrimaryExchangeId", "Ticker"))
        while (instrumentRows.Read()) result += deleteInstrument(instrumentRows.GetString(0));   //TODO: This should rather move the instrument to the global exchange.
      result += Delete(TableInstrumentSecondaryExchange, id, "ExchangeId");
      foreach (var sessionId in GetAssociatedIds(TableSession, id, "ExchangeId")) result += DeleteSession(sessionId);
      return result;
    }

    public void CreateHoliday(Holiday holiday)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableHoliday} (Id, AttributeSet, Tag, ParentId, Name, HolidayType, Month, DayOfMonth, WeekOfMonth, DayOfWeek, MoveWeekendHoliday) " +
            $"VALUES (" +
              $"'{holiday.Id.ToString()}', " +
              $"{(long)holiday.AttributeSet}, " +
              $"'{ToSqlSafeString(holiday.TagStr)}', " +
              $"'{holiday.ParentId.ToString()}', " +
              $"'{ToSqlSafeString(holiday.Name)}', " +
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
      using (var reader = ExecuteReader($"SELECT * FROM {TableHoliday} WHERE Id = '{id.ToString()}'"))
      {
        if (reader.Read())
          return new Holiday(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), (HolidayType)reader.GetInt64(5), (Months)reader.GetInt64(6), reader.GetInt32(7), (DayOfWeek)reader.GetInt64(8), (WeekOfMonth)reader.GetInt64(9), (MoveWeekendHoliday)reader.GetInt64(10));

        return null;
      }
    }

    public IList<Holiday> GetHolidays(Guid parentId)
    {
      var result = new List<Holiday>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableHoliday} WHERE ParentId = '{parentId.ToString()}' ORDER BY Name ASC"))
        while (reader.Read())
          result.Add(new Holiday(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), (HolidayType)reader.GetInt64(5), (Months)reader.GetInt64(6), reader.GetInt32(7), (DayOfWeek)reader.GetInt64(8), (WeekOfMonth)reader.GetInt64(9), (MoveWeekendHoliday)reader.GetInt64(10)));

      return result;
    }

    public IList<Holiday> GetHolidays()
    {
      var result = new List<Holiday>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableHoliday} ORDER BY Name ASC"))
        while (reader.Read())
          result.Add(new Holiday(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), (HolidayType)reader.GetInt64(5), (Months)reader.GetInt64(6), reader.GetInt32(7), (DayOfWeek)reader.GetInt64(8), (WeekOfMonth)reader.GetInt64(9), (MoveWeekendHoliday)reader.GetInt64(10))); ;

      return result;
    }

    public void UpdateHoliday(Holiday holiday)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableHoliday} " +
          $"(Id, ParentId, Name, AttributeSet, Tag, HolidayType, Month, DayOfMonth, WeekOfMonth, DayOfWeek, MoveWeekendHoliday) " +
          $"VALUES (" +
              $"'{holiday.Id.ToString()}', " +
              $"'{holiday.ParentId.ToString()}', " +
              $"'{ToSqlSafeString(holiday.Name)}', " +
              $"{(long)holiday.AttributeSet}, " +
              $"'{ToSqlSafeString(holiday.TagStr)}', " +
              $"{(int)holiday.Type}, " +
              $"{(int)holiday.Month}, " +
              $"{(int)holiday.DayOfMonth}, " +
              $"{(int)holiday.WeekOfMonth}, " +
              $"{(int)holiday.DayOfWeek}, " +
              $"{(int)holiday.MoveWeekendHoliday}" +
          $")"
        );
    }

    public int DeleteHoliday(Guid id)
    {
      return Delete(TableHoliday, id);
    }

    public void CreateSession(Session session)
    {
      lock (this)
        ExecuteCommand(
        $"INSERT OR REPLACE INTO {TableSession} (Id, AttributeSet, Tag, Name, ExchangeId, DayOfWeek, StartTime, EndTime) " +
          $"VALUES (" +
            $"'{session.Id.ToString()}', " +
            $"{(long)session.AttributeSet}, " +
            $"'{ToSqlSafeString(session.TagStr)}', " +
            $"'{ToSqlSafeString(session.Name)}', " +
            $"'{session.ExchangeId.ToString()}', " +
            $"{(int)session.DayOfWeek}, " +
            $"{session.Start.Ticks}, " +
            $"{session.End.Ticks}" +
          $")"
        );
    }

    public Session? GetSession(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT * FROM {TableSession} WHERE Id = '{id.ToString()}'"))
        if (reader.Read()) return new Session(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), (DayOfWeek)reader.GetInt32(5), new TimeOnly(reader.GetInt64(6)), new TimeOnly(reader.GetInt64(7)));

      return null;
    }

    public IList<Session> GetSessions(Guid exchangeId)
    {
      var result = new List<Session>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableSession} WHERE ExchangeId = '{exchangeId.ToString()}' ORDER BY DayOfWeek ASC, StartTime ASC, EndTime ASC"))
        while (reader.Read())
          result.Add(new Session(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), (DayOfWeek)reader.GetInt32(5), new TimeOnly(reader.GetInt64(6)), new TimeOnly(reader.GetInt64(7))));

      return result;
    }

    public IList<Session> GetSessions()
    {
      var result = new List<Session>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableSession}  ORDER BY ExchangeId ASC, DayOfWeek ASC, StartTime ASC, EndTime ASC"))
        while (reader.Read())
          result.Add(new Session(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), (DayOfWeek)reader.GetInt32(5), new TimeOnly(reader.GetInt64(6)), new TimeOnly(reader.GetInt64(7))));

      return result;
    }

    public void UpdateSession(Session session)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableSession} (" +
              "Id, " +
              "Name, " +
              "ExchangeId, " +
              "AttributeSet, " +
              "Tag, " +
              "DayOfWeek, " +
              "StartTime, " +
              "EndTime" +
          ") VALUES (" +
              $"'{session.Id.ToString()}', " +
              $"'{ToSqlSafeString(session.Name)}', " +
              $"'{session.ExchangeId.ToString()}', " +
              $"{(long)session.AttributeSet}, " +
              $"'{ToSqlSafeString(session.TagStr)}', " +
              $"{(int)session.DayOfWeek}, " +
              $"{session.Start.Ticks}, " +
              $"{session.End.Ticks}" +
          ")"
        );
    }

    public int DeleteSession(Guid id)
    {
      return Delete(TableSession, id);
    }

    public void CreateInstrumentGroup(InstrumentGroup instrumentGroup)
    {
      lock (this)
        ExecuteCommand(
        $"INSERT OR REPLACE INTO {TableInstrumentGroup} (Id, AttributeSet, Tag, ParentId, Name, Description, UserId, AlternateNames) " +
          $"VALUES (" +
            $"'{instrumentGroup.Id.ToString()}', " +
            $"{(long)instrumentGroup.AttributeSet}, " +
            $"'{ToSqlSafeString(instrumentGroup.TagStr)}', " +
            $"'{instrumentGroup.ParentId.ToString()}', " +
            $"'{ToSqlSafeString(instrumentGroup.Name)}', " +
            $"'{ToSqlSafeString(instrumentGroup.Description)}', " +
            $"'{ToSqlSafeString(instrumentGroup.UserId)}', " +
            $"'{ToSqlSafeString(Common.Utilities.ToCsv<string>(instrumentGroup.AlternateNames))}' " +
          $")"
        );

      foreach (string instrument in instrumentGroup.Instruments) CreateInstrumentGroupInstrument(instrumentGroup.Id, instrument);
    }

    public void CreateInstrumentGroupInstrument(Guid instrumentGroupId, string instrumentTicker)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableInstrumentGroupInstrument} (InstrumentGroupId, InstrumentTicker) " +
            $"VALUES (" +
              $"'{instrumentGroupId.ToString()}', " +
              $"'{instrumentTicker.ToString()}' " +
          $")"
        );
    }

    public InstrumentGroup? GetInstrumentGroup(Guid id)
    {
      using (var reader = ExecuteReader($"SELECT * FROM {TableInstrumentGroup} WHERE Id == '{id.ToString()}'"))
        if (reader.Read())
        {
          IList<string> instruments = GetInstrumentGroupInstruments(id);
          return new InstrumentGroup(id, (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(7))), reader.GetString(5), reader.GetString(6), instruments);
        }

      return null;
    }

    public IList<InstrumentGroup> GetInstrumentGroups()
    {
      List<InstrumentGroup> result = new List<InstrumentGroup>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableInstrumentGroup} ORDER BY ParentId ASC, Name ASC, Description ASC"))
      {
        while (reader.Read())
        {
          Guid id = reader.GetGuid(0);
          IList<string> instruments = GetInstrumentGroupInstruments(id);
          result.Add(new InstrumentGroup(id, (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4), Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(7))), reader.GetString(5), reader.GetString(6), instruments));
        }
      }

      return result;
    }

    public IList<string> GetInstrumentGroupInstruments(Guid instrumentGroupId)
    {
      List<string> result = new List<string>();

      using (var reader = ExecuteReader($"SELECT InstrumentGroupId, InstrumentTicker, Name, Description FROM {TableInstrumentGroupInstrument} INNER JOIN {TableInstrumentGroup} ON InstrumentGroupId == Id WHERE InstrumentGroupId = '{instrumentGroupId.ToString()}' ORDER BY InstrumentTicker ASC"))
        while (reader.Read()) result.Add(reader.GetString(1));

      return result;
    }

    public void UpdateInstrumentGroup(InstrumentGroup instrumentGroup)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableInstrumentGroup} (" +
              "Id, " +
              "ParentId, " +
              "AttributeSet, " +
              "Tag, " +
              "Name, " +
              "Description, " +
              "UserId, " +
              "AlternateNames" +
          ") VALUES (" +
              $"'{instrumentGroup.Id.ToString()}', " +
              $"'{instrumentGroup.ParentId.ToString()}', " +
              $"{(long)instrumentGroup.AttributeSet}, " +
              $"'{ToSqlSafeString(instrumentGroup.TagStr)}', " +
              $"'{ToSqlSafeString(instrumentGroup.Name)}', " +
              $"'{ToSqlSafeString(instrumentGroup.Description)}', " +
              $"'{ToSqlSafeString(instrumentGroup.UserId)}', " +
              $"'{ToSqlSafeString(string.Join(',', instrumentGroup.AlternateNames))}'" +
          ")"
        );

        Delete(TableInstrumentGroupInstrument, instrumentGroup.Id, "InstrumentGroupId");

        foreach (string instrument in instrumentGroup.Instruments)
          ExecuteCommand(
            $"INSERT OR REPLACE INTO {TableInstrumentGroupInstrument} (InstrumentGroupId, InstrumentTicker) " +
              $"VALUES (" +
                $"'{instrumentGroup.Id.ToString()}', " +
                $"'{instrument}'" +
            $")"
          );
      }
    }

    public int DeleteInstrumentGroup(Guid id)
    {
      int result = 0;
      using (var reader = ExecuteReader($"SELECT Id FROM {TableInstrumentGroup} WHERE ParentId = '{id.ToString()}'"))
        while (reader.Read()) result += DeleteInstrumentGroup(reader.GetGuid(0));

      lock (this)
      {
        result += ExecuteCommand($"DELETE FROM {TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{id.ToString()}'");
        result += ExecuteCommand($"DELETE FROM {TableInstrumentGroup} WHERE Id = '{id.ToString()}'");
      }

      return result;
    }

    public int DeleteInstrumentGroupChild(Guid parentId, Guid childId)
    {
      //NOTE: We do not use the parentId for Sqlite, we just reset the parentId on the instrument group table for the given child.
      int result = 0;
      lock (this) result = ExecuteCommand($"UPDATE OR IGNORE {TableInstrumentGroup} SET ParentId = '{InstrumentGroup.InstrumentGroupRoot.ToString()}' WHERE Id = '{childId.ToString()}'");
      return result;
    }

    public int DeleteInstrumentGroupInstrument(Guid instrumentGroupId, string instrumentTicker)
    {
      int result = 0;
      lock (this) result = ExecuteCommand($"DELETE FROM {TableInstrumentGroupInstrument} WHERE InstrumentGroupId = '{instrumentGroupId.ToString()}' AND InstrumentTicker = '{instrumentTicker}'");
      return result;
    }

    public void CreateInstrument(Instrument instrument)
    {
      lock (this)
      {
        //update common instrument elements
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableInstrument} (Ticker, AttributeSet, Tag, Type, Name, Description, PrimaryExchangeId, InceptionDate, PriceDecimals, MinimumMovement, BigPointValue, AlternateTickers, ExtendedProperties) " +
            $"VALUES (" +
              $"'{instrument.Ticker}', " +
              $"{(long)instrument.AttributeSet}, " +
              $"'{ToSqlSafeString(instrument.TagStr)}', " +
              $"{(int)instrument.Type}, " +
              $"'{ToSqlSafeString(instrument.Name)}', " +
              $"'{ToSqlSafeString(instrument.Description)}', " +
              $"'{instrument.PrimaryExchangeId.ToString()}', " +
              $"{instrument.InceptionDate.ToUniversalTime().ToBinary()}, " +
              $"{instrument.PriceDecimals}, " +
              $"{instrument.MinimumMovement}, " +
              $"{instrument.BigPointValue}," +
              $"'{ToSqlSafeString(Common.Utilities.ToCsv<string>(instrument.AlternateTickers))}'," +
              $"'{ToSqlSafeString(instrument.ExtendedProperties)}'" +
            $")"
        );

        foreach (Guid otherExchangeId in instrument.SecondaryExchangeIds)
        {
          ExecuteCommand(
            $"INSERT OR REPLACE INTO {TableInstrumentSecondaryExchange} (InstrumentTicker, ExchangeId) " +
              $"VALUES (" +
                $"'{instrument.Ticker}', " +
                $"'{otherExchangeId.ToString()}'" +
              $")"
          );
        }

        //update stock specific elements
        if (instrument is Stock stock)
        {
          ExecuteCommand(
            $"INSERT OR REPLACE INTO {TableStock} (Ticker, MarketCap, SharesOutstanding, EmployeeCount, Address, City, State, Zip, PhoneNumber, WebsiteUrl) " +
              $"VALUES (" +
                $"'{ToSqlSafeString(instrument.Ticker)}', " +
                $"{stock.MarketCap}, " +
                $"{stock.SharesOutstanding}, " +
                $"{stock.EmployeeCount}, " +
                $"'{ToSqlSafeString(stock.Address)}', " +
                $"'{ToSqlSafeString(stock.City)}', " +
                $"'{ToSqlSafeString(stock.State)}', " +
                $"'{ToSqlSafeString(stock.Zip)}', " +
                $"'{ToSqlSafeString(stock.PhoneNumber)}', " +
                $"'{ToSqlSafeString(stock.Url)}' " +
              $")"
          );
        }
      }
    }

    public void AddInstrumentToExchange(string instrumentTicker, Guid exchangeId)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR IGNORE INTO {TableInstrumentSecondaryExchange} (InstrumentTicker, ExchangeId) " +
            $"VALUES (" +
              $"'{instrumentTicker}', " +
              $"'{exchangeId.ToString()}' " +
            $")"
        );
    }

    /// <summary>
    /// Determines the total number of instruments defined in the database.
    /// </summary>
    public int GetInstrumentCount()
    {
      object? result = ExecuteScalar($"SELECT COUNT(*) FROM {TableInstrument}");
      return result != null ? (int)(long)result! : 0;   //database returns long so we first need to unbox it to it's actual type before trying to cast to int 
    }

    /// <summary>
    /// Determines the total number of instruments of the given type defined in the database.
    /// </summary>
    public int GetInstrumentCount(InstrumentType instrumentType)
    {
      if (instrumentType == InstrumentType.None) return GetInstrumentCount(); //boundary case where it count would simplify into no filter
      object? result = ExecuteScalar($"SELECT COUNT(*) FROM {TableInstrument} WHERE Type = {(int)instrumentType}");
      return result != null ? (int)(long)result! : 0;   //database returns long so we first need to unbox it to it's actual type before trying to cast to int
    }

    /// <summary>
    /// Returns the instruments matching the given filters (OR-relation).
    /// </summary>
    public int GetInstrumentCount(string tickerFilter, string nameFilter, string descriptionFilter)
    {
      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      object? result;
      if (filter != string.Empty)
        result = ExecuteScalar($"SELECT COUNT(*) FROM {TableInstrument} WHERE {filter}");
      else
        result = ExecuteScalar($"SELECT COUNT(*) FROM {TableInstrument}");
      return result != null ? (int)(long)result! : 0;   //database returns long so we first need to unbox it to it's actual type before trying to cast to int
    }

    /// <summary>
    /// Determines the total number of instruments of the given type defined in the database that match the given ticker, name OR description filters.
    /// If the ticker or description filter is null or empty then it is not used in the query, the filter can also use wildcard '*' for generic match.
    /// </summary>
    public int GetInstrumentCount(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter)
    {
      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      object? result;
      if (filter != string.Empty)
        result = ExecuteScalar($"SELECT COUNT(*) FROM {TableInstrument} WHERE Type = {(int)instrumentType} AND ({filter})");
      else
        result = ExecuteScalar($"SELECT COUNT(*) FROM {TableInstrument} WHERE Type = {(int)instrumentType}");
      return result != null ? (int)(long)result! : 0;   //database returns long so we first need to unbox it to it's actual type before trying to cast to int
    }

    /// <summary>
    /// Supports loading of all instruments, this method could take a long time to run so run it asynchronously in the view model.
    /// </summary>
    public IList<Instrument> GetInstruments()
    {
      var result = new List<Instrument>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableInstrument} ORDER BY Type ASC, Ticker ASC, Name ASC, Description ASC"))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);
          InstrumentType instrumentType = (InstrumentType)reader.GetInt32(3);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    public IList<Instrument> GetInstruments(InstrumentType instrumentType)
    {
      var result = new List<Instrument>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} ORDER BY Ticker ASC, Name ASC, Description ASC"))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    /// <summary>
    /// Returns the set of istruments matching the gven filters.
    /// </summary>
    public IList<Instrument> GetInstruments(string tickerFilter, string nameFilter, string descriptionFilter)
    {
      var result = new List<Instrument>();

      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" AND {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" AND {descriptionSql}" : descriptionSql;

      string sql = $"SELECT * FROM {TableInstrument} ORDER BY Ticker ASC, Name ASC, Description ASC";
      if (filter != string.Empty) sql = $"SELECT * FROM {TableInstrument} WHERE {filter} ORDER BY Ticker ASC, Name ASC, Description ASC";

      using (var reader = ExecuteReader(sql))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);
          InstrumentType instrumentType = (InstrumentType)reader.GetInt32(3);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    /// <summary>
    /// Returns the set of instruments of a given type that match the given ticker and description filters.
    /// </summary>
    public IList<Instrument> GetInstruments(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter)
    {
      var result = new List<Instrument>();

      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      string sql = $"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} ORDER BY Ticker ASC, Name ASC, Description ASC";
      if (filter != string.Empty) sql = $"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} AND ({filter}) ORDER BY Ticker ASC, Name ASC, Description ASC";

      using (var reader = ExecuteReader(sql))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    public IList<Instrument> GetInstrumentsOffset(string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      if (offset < 0 || count <= 0) return GetInstruments(tickerFilter, nameFilter, descriptionFilter);

      var result = new List<Instrument>();

      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      string sql = $"SELECT * FROM {TableInstrument} ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {count} OFFSET {offset}";
      if (filter != string.Empty) sql = $"SELECT * FROM {TableInstrument} WHERE {filter} ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {count} OFFSET {offset}";

      using (var reader = ExecuteReader(sql))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);
          InstrumentType instrumentType = (InstrumentType)reader.GetInt32(3);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    public IList<Instrument> GetInstrumentsOffset(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int offset, int count)
    {
      if (offset < 0 || count <= 0) return GetInstruments(instrumentType, tickerFilter, nameFilter, descriptionFilter);

      var result = new List<Instrument>();

      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      string sql = $"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {count} OFFSET {offset}";
      if (filter != string.Empty) sql = $"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} AND ({filter}) ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {count} OFFSET {offset}";

      using (var reader = ExecuteReader(sql))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    /// <summary>
    /// Returns the set of instruments matching the given filters using the given pageIndex and pageSize to retrieve a specific sub-set of instruments.
    /// </summary>
    public IList<Instrument> GetInstrumentsPage(string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize)
    {
      if (pageIndex < 0 || pageSize <= 0) return GetInstruments(tickerFilter, nameFilter, descriptionFilter);

      var result = new List<Instrument>();

      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      string sql = $"SELECT * FROM {TableInstrument} ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {pageSize} OFFSET {pageIndex * pageSize}";
      if (filter != string.Empty) sql = $"SELECT * FROM {TableInstrument} WHERE {filter} ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {pageSize} OFFSET {pageIndex * pageSize}";

      using (var reader = ExecuteReader(sql))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);
          InstrumentType instrumentType = (InstrumentType)reader.GetInt32(3);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    /// <summary>
    /// Returns the set of instruments of the given type that match the given ticker and description filters with .
    /// </summary>
    public IList<Instrument> GetInstrumentsPage(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize)
    {
      if (pageIndex < 0 || pageSize <= 0) return GetInstruments(instrumentType, tickerFilter, nameFilter, descriptionFilter);

      var result = new List<Instrument>();

      string tickerSql = string.Empty;
      if (tickerFilter != string.Empty)
      {
        tickerSql = "Ticker ";
        if (tickerFilter.Contains('*') || tickerFilter.Contains('?'))
        {
          tickerSql += $"LIKE '{tickerFilter}'";
          tickerSql = tickerSql.Replace('?', '_');
          tickerSql = tickerSql.Replace('*', '%');
        }
        else
          tickerSql += $"= '{tickerFilter}'";
      }

      string nameSql = string.Empty;
      if (nameFilter != string.Empty)
      {
        nameSql = "Name ";
        if (nameFilter.Contains('*') || nameFilter.Contains('?'))
        {
          nameSql += $"LIKE '{nameFilter}'";
          nameSql = nameSql.Replace('?', '_');
          nameSql = nameSql.Replace('*', '%');
        }
        else
          nameSql += $"= '{nameFilter}'";
      }

      string descriptionSql = string.Empty;
      if (descriptionFilter != string.Empty)
      {
        descriptionSql = "Description ";
        if (descriptionFilter.Contains('*') || descriptionFilter.Contains('?'))
        {
          descriptionSql += $"LIKE '{descriptionFilter}'";
          descriptionSql = descriptionSql.Replace('?', '_');
          descriptionSql = descriptionSql.Replace('*', '%');
        }
        else
          descriptionSql += $"= '{descriptionFilter}'";
      }

      string filter = tickerSql;
      if (nameSql != string.Empty) filter += filter.Length != 0 ? $" OR {nameSql}" : nameSql;
      if (descriptionSql != string.Empty) filter += filter.Length != 0 ? $" OR {descriptionSql}" : descriptionSql;

      string sql = $"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {pageSize} OFFSET {pageIndex * pageSize}";
      if (filter != string.Empty) sql = $"SELECT * FROM {TableInstrument} WHERE Type = {(int)instrumentType} AND ({filter}) ORDER BY Ticker ASC, Name ASC, Description ASC LIMIT {pageSize} OFFSET {pageIndex * pageSize}";

      using (var reader = ExecuteReader(sql))
        while (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          string ticker = reader.GetString(0);

          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              var stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = stockReader.GetString(4);
                  stock.City = stockReader.GetString(5);
                  stock.State = stockReader.GetString(6);
                  stock.Zip = stockReader.GetString(7);
                  stock.PhoneNumber = stockReader.GetString(8);
                  stock.Url = stockReader.GetString(9);
                }
              result.Add(stock);
              break;
            default:
              result.Add(new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12)));
              break;
          }
        }

      return result;
    }

    public Instrument? GetInstrument(string ticker)
    {
      string normalizedTicker = ticker.Trim().ToUpper();
      normalizedTicker = normalizedTicker.Trim();
      using (var reader = ExecuteReader($"SELECT * FROM {TableInstrument} WHERE Ticker = '{normalizedTicker}'"))
        if (reader.Read())
        {
          List<Guid> secondaryExchangeIds = new List<Guid>();
          using (var secondaryExchangeReader = ExecuteReader($"SELECT ExchangeId FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'"))
            while (secondaryExchangeReader.Read()) secondaryExchangeIds.Add(secondaryExchangeReader.GetGuid(0));

          InstrumentType instrumentType = (InstrumentType)reader.GetInt32(3);

          switch (instrumentType)
          {
            case InstrumentType.Stock:
              Stock stock = new Stock(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));

              using (var stockReader = ExecuteReader($"SELECT * FROM {TableStock} WHERE Ticker = '{ticker}'"))
                if (stockReader.Read())
                {
                  stock.MarketCap = stockReader.GetDouble(1);
                  stock.SharesOutstanding = stockReader.GetDouble(2);
                  stock.EmployeeCount = stockReader.GetInt32(3);
                  stock.Address = FromSqlSafeString(stockReader.GetString(4));
                  stock.City = FromSqlSafeString(stockReader.GetString(5));
                  stock.State = FromSqlSafeString(stockReader.GetString(6));
                  stock.Zip = FromSqlSafeString(stockReader.GetString(7));
                  stock.PhoneNumber = FromSqlSafeString(stockReader.GetString(8));
                  stock.Url = FromSqlSafeString(stockReader.GetString(9));
                }

              return stock;
            default:
              return new Instrument(ticker, (Attributes)reader.GetInt64(1), reader.GetString(2), instrumentType, Common.Utilities.FromCsv(FromSqlSafeString(reader.GetString(11))), reader.GetString(4), reader.GetString(5), DateTime.FromBinary(reader.GetInt64(7)), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetGuid(6), secondaryExchangeIds, reader.GetString(12));
          }
        }

      return null;
    }

    public void UpdateInstrument(Instrument instrument)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableInstrument} (" +
              "Ticker, " +
              "AttributeSet, " +
              "Tag, " +
              "Name, " +
              "Description, " +
              "PrimaryExchangeId, " +
              "InceptionDate, " +
              "PriceDecimals, " +
              "MinimumMovement, " +
              "BigPointValue, " +
              "AlternateTickers, " +
              "ExtendedProperties" +
          ") VALUES (" +
              $"'{instrument.Ticker}', " +
              $"{(long)instrument.AttributeSet}, " +
              $"'{ToSqlSafeString(instrument.TagStr)}', " +
              $"'{ToSqlSafeString(instrument.Name)}', " +
              $"'{ToSqlSafeString(instrument.Description)}', " +
              $"'{instrument.PrimaryExchangeId.ToString()}', " +
              $"{instrument.InceptionDate.ToUniversalTime().ToBinary()}, " +
              $"{instrument.PriceDecimals}, " +
              $"{instrument.MinimumMovement}, " +
              $"{instrument.BigPointValue}, " +
              $"'{ToSqlSafeString(string.Join(',', instrument.AlternateTickers))}', " +
              $"'{ToSqlSafeString(instrument.ExtendedProperties)}'" +
          ")"
        );

        ExecuteCommand($"DELETE FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{instrument.Ticker}'");

        foreach (Guid otherExchangeId in instrument.SecondaryExchangeIds)
        {
          ExecuteCommand(
            $"INSERT OR REPLACE INTO {TableInstrumentSecondaryExchange} (InstrumentTicker, ExchangeId) " +
              $"VALUES (" +
                $"'{instrument.Ticker}', " +
                $"'{otherExchangeId.ToString()}'" +
              $")"
          );
        }

        if (instrument is Stock stock)
        {
          ExecuteCommand(
            $"INSERT OR REPLACE INTO {TableStock} (Ticker, MarketCap, SharesOutstanding, EmployeeCount, Address, City, State, Zip, PhoneNumber, WebsiteUrl) " +
              $"VALUES (" +
                $"'{instrument.Ticker}', " +
                $"{stock.MarketCap}," +
                $"{stock.SharesOutstanding}, " +
                $"{stock.EmployeeCount}, " +
                $"'{ToSqlSafeString(stock.Address)}', " +
                $"'{ToSqlSafeString(stock.City)}', " +
                $"'{ToSqlSafeString(stock.State)}', " +
                $"'{ToSqlSafeString(stock.Zip)}', " +
                $"'{ToSqlSafeString(stock.PhoneNumber)}', " +
                $"'{ToSqlSafeString(stock.Url)}'" +
              $")"
          );
        }
      }
    }

    public int DeleteInstrument(Instrument instrument)
    {
      return deleteInstrument(instrument.Ticker);
    }

    public int deleteInstrument(string ticker)
    {
      int result = 0;

      lock (this)
      {
        result = ExecuteCommand($"DELETE FROM {TableInstrument} WHERE Ticker = '{ticker}'");
        result = ExecuteCommand($"DELETE FROM {TableStock} WHERE Ticker = '{ticker}'");
        result += ExecuteCommand($"DELETE FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{ticker}'");
        result += ExecuteCommand($"DELETE FROM {TableInstrumentGroupInstrument} WHERE InstrumentTicker = '{ticker}'");

        foreach (var dataProvider in m_configurationService.DataProviders)
        {
          using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProvider.Key, TableInstrumentFundamentalAssociations)} WHERE InstrumentTicker = '{ticker}'"))
            lock (this)
              while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Key, TableInstrumentFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Key, TableInstrumentFundamentalAssociations)} WHERE InstrumentTicker = '{ticker}'");
          result += deleteData(dataProvider.Key, ticker, null);
          CacheInstrumentFundamentalAssociations(dataProvider.Key);
        }
      }

      return result;
    }

    public int DeleteInstrumentFromExchange(string instrumentTicker, Guid exchangeId)
    {
      int result = 0;
      lock (this) result = ExecuteCommand($"DELETE FROM {TableInstrumentSecondaryExchange} WHERE InstrumentTicker = '{instrumentTicker}' AND ExchangeId = '{exchangeId.ToString()}'");
      return result;
    }

    public void CreateFundamental(Fundamental fundamental)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableFundamentals} (Id, AttributeSet, Tag, Name, Description, Category, ReleaseInterval)" +
            $"VALUES (" +
              $"'{fundamental.Id.ToString()}', " +
              $"{(long)fundamental.AttributeSet}, " +
              $"'{ToSqlSafeString(fundamental.TagStr)}', " +
              $"'{ToSqlSafeString(fundamental.Name)}', " +
              $"'{ToSqlSafeString(fundamental.Description)}', " +
              $"{(int)fundamental.Category}, " +
              $"{(int)fundamental.ReleaseInterval}" +
          $")"
        );
    }

    public IList<Fundamental> GetFundamentals()
    {
      var result = new List<Fundamental>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableFundamentals} ORDER BY Category ASC, Name ASC, Description ASC"))
        while (reader.Read())
          result.Add(new Fundamental(reader.GetGuid(0), (Attributes)reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), (FundamentalCategory)reader.GetInt32(5), (FundamentalReleaseInterval)reader.GetInt32(6)));

      return result;
    }

    public int DeleteFundamental(Guid id)
    {
      int result = DeleteFundamentalValues(id);
      result += Delete(TableFundamentals, id);

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        CacheCountryFundamentalAssociations(dataProvider.Key);
        CacheInstrumentFundamentalAssociations(dataProvider.Key);
      }

      return result;
    }

    public int DeleteFundamentalValues(Guid id)
    {
      int result = 0;
      foreach (var dataProvider in m_configurationService.DataProviders) result += DeleteFundamentalValues(dataProvider.Key, id);
      return result;
    }

    public int DeleteFundamentalValues(string dataProviderName, Guid id)
    {
      int result = 0;

      using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProviderName, TableCountryFundamentalAssociations)} WHERE FundamentalId = '{id.ToString()}'"))
        lock (this) while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableCountryFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");

      using (var reader = ExecuteReader($"SELECT Id FROM {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalAssociations)} WHERE FundamentalId = '{id.ToString()}'"))
        lock (this) while (reader.Read()) result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalValues)} WHERE AssociationId = '{reader.GetGuid(0).ToString()}'");

      foreach (var dataProvider in m_configurationService.DataProviders)
      {
        CacheCountryFundamentalAssociations(dataProvider.Key);
        CacheInstrumentFundamentalAssociations(dataProvider.Key);
      }

      return result;
    }

    public void CreateCountryFundamental(CountryFundamental fundamental)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {GetDataProviderDBName(fundamental.DataProviderName, TableCountryFundamentalAssociations)} (Id, FundamentalId, CountryId) " +
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
      string dataProviderAssociationTable = GetDataProviderDBName(dataProviderName, TableCountryFundamentalAssociations);
      string selectQuery =
        $"SELECT * FROM {dataProviderAssociationTable} " +
          $"INNER JOIN {TableFundamentals} ON {dataProviderAssociationTable}.FundamentalId = {TableFundamentals}.Id " +
          $"WHERE {TableFundamentals}.Category = {(int)FundamentalCategory.Country} " +
          $"ORDER BY {TableFundamentals}.Name ASC, {TableFundamentals}.Description ASC, {dataProviderAssociationTable}.FundamentalId ASC, {dataProviderAssociationTable}.CountryId ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read()) result.Add(new CountryFundamental(dataProviderName, reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2)));

      //load fundamental values
      string dataProviderValueTable = GetDataProviderDBName(dataProviderName, TableCountryFundamentalValues);
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

      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {GetDataProviderDBName(dataProviderName, TableCountryFundamentalValues)} (AssociationId, DateTime, Value) " +
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

      int result = 0;

      lock (this)
      {
        result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableCountryFundamentalAssociations)} WHERE Id = '{associationId.ToString()}'");
        result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableCountryFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}'");
      }

      CacheCountryFundamentalAssociations(dataProviderName);

      return result;
    }

    public int DeleteCountryFundamentalValue(string dataProviderName, Guid fundamentalId, Guid countryId, DateTime dateTime)
    {
      Guid? associationId = GetCountryFundamentalAssociationId(dataProviderName, fundamentalId, countryId);
      if (!associationId.HasValue) return 0;  //no association, nothing to remove
      int result = 0;
      lock (this) result = ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableCountryFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}' AND DateTime = {dateTime.ToUniversalTime().ToBinary()}");
      return result;
    }

    public void CreateInstrumentFundamental(InstrumentFundamental fundamental)
    {
      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {GetDataProviderDBName(fundamental.DataProviderName, TableInstrumentFundamentalAssociations)} (Id, FundamentalId, InstrumentTicker) " +
            $"VALUES (" +
              $"'{fundamental.AssociationId.ToString()}', " +
              $"'{fundamental.FundamentalId.ToString()}', " +
              $"'{fundamental.InstrumentTicker}'" +
            $")"
        );

      AssociationCacheEntry? cacheEntry;
      if (m_instrumentFundamentalAssociations.TryGetValue(fundamental.DataProviderName, out cacheEntry))
        cacheEntry![fundamental.FundamentalId.GetHashCode() + fundamental.InstrumentTicker.GetHashCode()] = fundamental.AssociationId;
      else
      {
        cacheEntry = new AssociationCacheEntry() { { fundamental.FundamentalId.GetHashCode() + fundamental.InstrumentTicker.GetHashCode(), fundamental.AssociationId } };
        m_instrumentFundamentalAssociations[fundamental.DataProviderName] = cacheEntry;
      }
    }

    public IList<InstrumentFundamental> GetInstrumentFundamentals(string dataProviderName)
    {
      var result = new List<InstrumentFundamental>();

      //load basic fundamental and instrument associations
      string dataProviderAssociationTable = GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalAssociations);
      string selectQuery =
        $"SELECT * FROM {dataProviderAssociationTable} " +
          $"INNER JOIN {TableFundamentals} ON {dataProviderAssociationTable}.FundamentalId = {TableFundamentals}.Id " +
          $"WHERE {TableFundamentals}.Category = {(int)FundamentalCategory.Instrument} " +
          $"ORDER BY {TableFundamentals}.Name ASC, {TableFundamentals}.Description ASC, {dataProviderAssociationTable}.FundamentalId ASC, {dataProviderAssociationTable}.InstrumentTicker ASC";

      using (var reader = ExecuteReader(selectQuery))
        while (reader.Read()) result.Add(new InstrumentFundamental(dataProviderName, reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2)));

      //load fundamental values
      string dataProviderValueTable = GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalValues);
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

    public void UpdateInstrumentFundamental(string dataProviderName, Guid fundamentalId, string instrumentTicker, DateTime dateTime, double value)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentTicker);
      if (!associationId.HasValue) throw new ArgumentException($"Instrument ({instrumentTicker}) is not associated with fundamental ({fundamentalId}).");

      lock (this)
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalValues)} (AssociationId, DateTime, Value) " +
            $"VALUES (" +
              $"'{associationId!.Value.ToString()}', " +
              $"{dateTime.ToUniversalTime().ToBinary()}, " +
              $"{value.ToString()}" +
          $")"
        );
    }

    public int DeleteInstrumentFundamental(string dataProviderName, Guid fundamentalId, string instrumentTicker)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentTicker);
      if (!associationId.HasValue) return 0;

      int result = 0;

      lock (this)
      {
        result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalAssociations)} WHERE Id = '{associationId.ToString()}'");
        result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}'");
      }

      CacheInstrumentFundamentalAssociations(dataProviderName);

      return result;
    }

    public int DeleteInstrumentFundamentalValue(string dataProviderName, Guid fundamentalId, string instrumentTicker, DateTime dateTime)
    {
      Guid? associationId = GetInstrumentFundamentalAssociationId(dataProviderName, fundamentalId, instrumentTicker);
      if (!associationId.HasValue) return 0;  //no association, nothing to remove
      int result = 0;
      lock (this) result = ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalValues)} WHERE AssociationId = '{associationId.ToString()}' AND DateTime = {dateTime.ToUniversalTime().ToBinary()}");
      return result;
    }

    public void UpdateData(string dataProviderName, string ticker, Resolution resolution, DateTime dateTime, double open, double high, double low, double close, double volume)
    {
      //level 1 data can not be updated by his method
      if (resolution == Resolution.Level1) throw new ArgumentException("Update for bar data can not update Level 1 data.");

      //create database command
      string command;
      string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution);
      string normalizedTicker = ticker.ToUpper();

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

      lock (this) ExecuteCommand(command);
    }

    public void UpdateData(string dataProviderName, string ticker, DateTime dateTime, double bid, double bidSize, double ask, double askSize, double last, double lastSize)
    {
      //create database command
      string command;
      string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, Resolution.Level1);
      string normalizedTicker = ticker.ToUpper();

      command =
        $"INSERT OR REPLACE INTO {tableName} (Ticker, DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize) " +
          $"VALUES (" +
            $"'{normalizedTicker}', " +
            $"{dateTime.ToUniversalTime().ToBinary()}, " +
            $"{bid}, " +
            $"{bidSize}, " +
            $"{ask}, " +
            $"{askSize}, " +
            $"{last}, " +
            $"{lastSize}" +
        $")";

      lock (this) ExecuteCommand(command);
    }

    /// <summary>
    /// Mass update of bar data is much faster than single UpdateData calls to use it for mass updated where appropriate.
    /// </summary>
    public void UpdateData(string dataProviderName, string ticker, Resolution resolution, DataCacheBars bars)
    {
      //level 1 data can not be updated by his method
      if (resolution == Resolution.Level1) throw new ArgumentException("Update for bar data can not update Level 1 data.");
      if (bars.Count == 0) return;

      //aquire database lock
      lock (this)
      {
        string normalizedTicker = ticker.ToUpper();
        string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution);

        for (int index = 0; index < bars.Count; index++)
        {
          //create command for update - NOTE: you can not reuse commands in SQLite
          var command = m_connection.CreateCommand();
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
      }
    }

    /// <summary>
    /// Mass update of bar data is much faster than single UpdateData calls to use it for mass updated where appropriate.
    /// </summary>
    public void UpdateData(string dataProviderName, string ticker, Resolution resolution, IList<IBarData> bars)
    {
      //level 1 data can not be updated by his method
      if (resolution == Resolution.Level1) throw new ArgumentException("Update for bar data can not update Level 1 data.");
      if (bars.Count == 0) return;

      //create database update
      lock (this)
      {
        string normalizedTicker = ticker.ToUpper();
        string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution);

        foreach (IBarData bar in bars)
        {
          var command = m_connection.CreateCommand();
          command.CommandText = $"INSERT OR REPLACE INTO {tableName} (Ticker, DateTime, Open, High, Low, Close, Volume) " +
            $"VALUES (" +
              $"'{normalizedTicker}', " +
              $"{bar.DateTime.ToUniversalTime().ToBinary()}, " +
              $"{bar.Open}, " +
              $"{bar.High}, " +
              $"{bar.Low}, " +
              $"{bar.Close}, " +
              $"{bar.Volume}" +
            $")";

          command.ExecuteNonQuery();
        }
      }
    }

    public void UpdateData(string dataProviderName, string ticker, DataCacheLevel1 level1Data)
    {
      if (level1Data.Count == 0) throw new ArgumentException("Update data count should not be zero.");

      //create database update
      lock (this)
      {
        string normalizedTicker = ticker.ToUpper();
        string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, Resolution.Level1);

        for (int index = 0; index < level1Data.Count; index++)
        {
          //create command for update - NOTE: you can not reuse commands in SQLite
          var command = m_connection.CreateCommand();
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
      }
    }

    public void UpdateData(string dataProviderName, string ticker, Resolution resolution, IList<ILevel1Data> bars)
    {
      //bar data can not be updated by his method
      if (resolution != Resolution.Level1) throw new ArgumentException("Update for Level 1 data can not update bar data.");
      if (bars.Count == 0) return;

      //create database update
      lock (this)
      {
        string normalizedTicker = ticker.ToUpper();
        string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution);

        foreach (ILevel1Data bar in bars)
        {
          var command = m_connection.CreateCommand();
          command.CommandText = $"INSERT OR REPLACE INTO {tableName} (Ticker, DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize) " +
            $"VALUES (" +
              $"'{normalizedTicker}', " +
              $"{bar.DateTime.ToUniversalTime().ToBinary()}, " +
              $"{bar.Bid}, " +
              $"{bar.BidSize}, " +
              $"{bar.Ask}, " +
              $"{bar.AskSize}, " +
              $"{bar.Last}, " +
              $"{bar.LastSize}" +
            $")";

          command.ExecuteNonQuery();
        }
      }
    }

    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime dateTime)
    {
      return deleteData(dataProviderName, ticker, resolution, dateTime);
    }

    /// <summary>
    /// General utility function to delete price data from the database using various input parameters.
    /// </summary>
    protected int deleteData(string dataProviderName, string ticker, Resolution? resolution = null, DateTime? dateTime = null)
    {
      int result = 0;

      lock (this)
      {
        if (resolution.HasValue)
        {
          result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution.Value)} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                        ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution.Value)} WHERE Ticker = '{ticker}'");
        }
        else
        {
          //delete all resolutions
          foreach (Resolution res in s_SupportedResolutions)
          {
            result += dateTime.HasValue ? ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, res)} WHERE Ticker = '{ticker}' AND DateTime = {dateTime.Value.ToUniversalTime().ToBinary()}") :
                                          ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, res)} WHERE Ticker = '{ticker}'");
          }
        }
      }

      return result;
    }

    public int DeleteData(string dataProviderName, string ticker, Resolution? resolution, DateTime? from = null, DateTime? to = null)
    {
      int result = 0;

      string where = $"Ticker = '{ticker}'";
      if (from != null) where += $" AND DateTime >= {from.Value.ToUniversalTime().ToBinary()}";
      if (to != null) where += $" AND DateTime >= {to.Value.ToUniversalTime().ToBinary()}";

      lock (this)
      {
        if (resolution != null)
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution.Value)} WHERE " + where);
        else
        {
          foreach (Resolution res in s_SupportedResolutions)
            result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, res)} WHERE " + where);
        }
      }

      return result;
    }

    private static DateTime convertDateTimeBasedOnConfiguration(DateTime utcDateTime, IConfigurationService.TimeZone timeZoneToUse, Exchange? exchange)
    {
      switch (timeZoneToUse)
      {
        case IConfigurationService.TimeZone.UTC:
          return utcDateTime;
        case IConfigurationService.TimeZone.Local:
          return utcDateTime.ToLocalTime();
        case IConfigurationService.TimeZone.Exchange:
          if (exchange == null) throw new ArgumentException("Exchange must be specified when using Exchange time zone.");
          return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, exchange.TimeZone); //can not use the BaseUtcOffset, we need to use the conversion function to take into account daylight savings time
      }

      return utcDateTime; //default to UTC
    }

    public DataCache GetBarData(string dataProviderName, string ticker, DateTime from, DateTime to, Resolution resolution)
    {
      //validate inputs
      if (resolution == Resolution.Level1) throw new ArgumentException("GetBarData can not return Level  date, use GetLevel1Data.");

      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //create database command
      List<Tuple<DateTime, double, double, double, double, double, bool>> list = new List<Tuple<DateTime, double, double, double, double, double, bool>>();
      string command;

      //load data
      command =
        $"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
            $"AND DateTime >= {fromUtc.ToUniversalTime().ToBinary()} " +
            $"AND DateTime <= {toUtc.ToUniversalTime().ToBinary()} " +
          $"ORDER BY DateTime ASC";

      using (SqliteDataReader reader = ExecuteReader(command))
      {
        while (reader.Read())
        {
          var dateTime = convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(1)), timeZoneToUse, exchange);
          list.Add(new Tuple<DateTime, double, double, double, double, double, bool>(dateTime, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6), false));
        }
      }

      //construct returned cache entry    
      DataCache dataCache = new DataCache(dataProviderName, ticker, resolution, from, to, list.Count);
      DataCacheBars barData = (DataCacheBars)dataCache.Data;

      int i = 0;
      foreach (var bar in list)
      {
        barData.DateTime[i] = bar.Item1;
        barData.Open[i] = bar.Item2;
        barData.High[i] = bar.Item3;
        barData.Low[i] = bar.Item4;
        barData.Close[i] = bar.Item5;
        barData.Volume[i] = bar.Item6;
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
      public double BidSize;
      public double Ask;
      public double AskSize;
      public double Last;
      public double LastSize;
    }

    private static int compareLevel1Data(Level1DBRecord x, Level1DBRecord y)
    {
      return x.DateTime.CompareTo(y.DateTime);
    }

    /// <summary>
    /// Loads level 1 tick data from the database and returns the populated DataCache structure.
    /// </summary>
    public DataCache GetDataCache(string dataProviderName, string ticker, DateTime from, DateTime to)
    {
      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //create database command
      List<Level1DBRecord> list = new List<Level1DBRecord>();

      string command;
      string normalizedTicker = ticker.ToUpper();

      //load bar data
      command =
        $"SELECT DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, Resolution.Level1)} " +
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
          level1DB.BidSize = reader.GetDouble(2);
          level1DB.Ask = reader.GetDouble(3);
          level1DB.AskSize = reader.GetDouble(4);
          level1DB.Last = reader.GetDouble(5);
          level1DB.LastSize = reader.GetDouble(6);
          list.Add(level1DB);
        }
      }

      //construct returned cache
      DataCache dataCache = new DataCache(dataProviderName, ticker, Resolution.Level1, from, to, list.Count);
      DataCacheLevel1 level1Data = (DataCacheLevel1)dataCache.Data;

      int i = 0;
      foreach (var entry in list)
      {
        level1Data.DateTime[i] = entry.DateTime;
        level1Data.Bid[i] = entry.Bid;
        level1Data.BidSize[i] = entry.BidSize;
        level1Data.Ask[i] = entry.Ask;
        level1Data.AskSize[i] = entry.AskSize;
        level1Data.Last[i] = entry.Last;
        level1Data.LastSize[i] = entry.LastSize;
        i++;
      }

      return dataCache;
    }

    public IBarData? GetBarData(string dataProviderName, string ticker, Resolution resolution, DateTime dateTime, string priceFormatMask)
    {
      if (resolution == Resolution.Level1) throw new ArgumentException("GetBarData can not return level 1 data using interface IBarData, use ILevelData instead.");

      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime dateTimeUtc = dateTime.ToUniversalTime();
      string command;

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //load data if found
      command =
        $"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
            $"AND DateTime == {dateTimeUtc.ToUniversalTime().ToBinary()}";

      using (SqliteDataReader reader = ExecuteReader(command))
        if (reader.Read())
          return new BarData(resolution, convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(1)), timeZoneToUse, exchange), priceFormatMask, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6));

      return null;
    }

    private static int compareBarData(IBarData x, IBarData y)
    {
      return x.DateTime.CompareTo(y.DateTime);
    }

    public int GetDataCount(string dataProviderName, string ticker, Resolution resolution)
    {
      //create database command
      string normalizedTicker = ticker.ToUpper();

      SqliteCommand commandObj = m_connection.CreateCommand();
      commandObj.CommandText =
      $"SELECT COUNT(*) FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{normalizedTicker}'";
      return Convert.ToInt32(commandObj.ExecuteScalar());  //NOTE: This places an upper limit on the number of rows to be stored in the database of int.MaxValue.
    }

    public int GetDataCount(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to)
    {
      //create database command
      string normalizedTicker = ticker.ToUpper();

      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      SqliteCommand commandObj = m_connection.CreateCommand();
      commandObj.CommandText =
      $"SELECT COUNT(*) FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{normalizedTicker}' " +
            $"AND DateTime >= {fromUtc.ToBinary()} " +
            $"AND DateTime <= {toUtc.ToBinary()}";
      return Convert.ToInt32(commandObj.ExecuteScalar());  //NOTE: This places an upper limit on the number of rows to be stored in the database of int.MaxValue.
    }

    public IList<IBarData> GetBarData(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to, string priceFormatMask)
    {
      if (resolution == Resolution.Level1) throw new ArgumentException("GetBarData can not return level 1 data using interface IBarData, use ILevelData instead.");

      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //create database command
      List<IBarData> result = new List<IBarData>();
      string command;

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //load bar data from database
      command =
        $"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
            $"AND DateTime >= {fromUtc.ToBinary()} " +
            $"AND DateTime <= {toUtc.ToBinary()} " +
          $"ORDER BY DateTime ASC";

      using (SqliteDataReader reader = ExecuteReader(command))
      {
        while (reader.Read())
        {
          var dateTime = convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(1)), timeZoneToUse, exchange);
          result.Add(new BarData(resolution, dateTime, priceFormatMask, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6)));
        }
      }

      return result;
    }

    /// <summary>
    /// Supports paged loading of the data from the database. NOTE: The database layer does NOT support all or merging of the actual and syntehtic price data type as it would require
    /// storing of state information in order to support proper paging, for this reason an abstraction layer above the database is required to merge the data if required.
    /// </summary>
    public IList<IBarData> GetBarData(string dataProviderName, string ticker, Resolution resolution, int index, int count, string priceFormatMask)
    {
      if (resolution == Resolution.Level1) throw new ArgumentException("GetBarData can not return level 1 data using interface IBarData, use ILevelData instead.");

      //create database command
      List<IBarData> result = new List<IBarData>();
      string command;

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //load bar data
      command =
        $"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
          $"ORDER BY DateTime ASC " +
          $"LIMIT {count} OFFSET {index}";

      using (SqliteDataReader reader = ExecuteReader(command))
      {
        while (reader.Read())
        {
          var dateTime = convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(1)), timeZoneToUse, exchange);
          result.Add(new BarData(resolution, dateTime, priceFormatMask, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6)));
        }
      }

      return result;
    }

    /// <summary>
    /// Supports paged loading of the data from the database with date filtering. NOTE: The database layer does NOT support all or merging of the actual and syntehtic price data type as it would require
    /// storing of state information in order to support proper paging, for this reason an abstraction layer above the database is required to merge the data if required.
    /// </summary>
    public IList<IBarData> GetBarData(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to, int index, int count, string priceFormatMask)
    {
      if (resolution == Resolution.Level1) throw new ArgumentException("GetBarData can not return level 1 data using interface IBarData, use ILevelData instead.");

      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //create database command
      List<IBarData> result = new List<IBarData>();
      string command;

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //load bar data
      command =
        $"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
            $"AND DateTime >= {fromUtc.ToBinary()} " +
            $"AND DateTime <= {toUtc.ToBinary()} " +
          $"ORDER BY DateTime ASC " +
          $"LIMIT {count} OFFSET {index}";

      using (SqliteDataReader reader = ExecuteReader(command))
      {
        while (reader.Read())
        {
          var dateTime = convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(1)), timeZoneToUse, exchange);
          result.Add(new BarData(resolution, dateTime, priceFormatMask, reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6)));
        }
      }

      return result;
    }

    public ILevel1Data? GetLevel1Data(string dataProviderName, string ticker, DateTime dateTime, string priceFormatMask)
    {
      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime dateTimeUtc = dateTime.ToUniversalTime();

      //create database command
      string command;

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //get actual bar data
      command =
        $"SELECT DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, Resolution.Level1)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
            $"AND DateTime == {dateTimeUtc.ToBinary()}";

      using (SqliteDataReader reader = ExecuteReader(command))
        if (reader.Read())
          return new Level1Data(convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(0)), timeZoneToUse, exchange), priceFormatMask, reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6));

      return null;
    }

    private static int compareLevel1Data(ILevel1Data x, ILevel1Data y)
    {
      return x.DateTime.CompareTo(y.DateTime);
    }

    public IList<ILevel1Data> GetLevel1Data(string dataProviderName, string ticker, DateTime from, DateTime to, string priceFormatMask)
    {
      //bar data selection must always be based in UTC datetime - we force this on the database layer to make sure we avoid unintended bugs where selections are unintentionally with mixed DateTime kinds.
      DateTime fromUtc = from.ToUniversalTime();
      DateTime toUtc = to.ToUniversalTime();

      //get the time zone to use for the date time conversion
      IConfigurationService.TimeZone timeZoneToUse = (IConfigurationService.TimeZone)m_configurationService.General[IConfigurationService.GeneralConfiguration.TimeZone];
      Exchange? exchange = null;
      if (timeZoneToUse == IConfigurationService.TimeZone.Exchange)
      {
        Instrument? instrument = GetInstrument(ticker);
        if (instrument == null) throw new ArgumentException($"Instrument ({ticker}) not found.");
        exchange = GetExchange(instrument.PrimaryExchangeId);
      }

      //create database command
      List<ILevel1Data> result = new List<ILevel1Data>();

      //create database command
      string command;

      //get level1 data
      command =
        $"SELECT DateTime, Bid, BidSize, Ask, AskSize, Last, LastSize FROM {GetDataProviderDBName(dataProviderName, TableInstrumentData, Resolution.Level1)} " +
          $"WHERE " +
            $"Ticker = '{ticker.Trim().ToUpper()}' " +
            $"AND DateTime >= {fromUtc.ToBinary()} " +
            $"AND DateTime <= {toUtc.ToBinary()} " +
          $"ORDER BY DateTime ASC";

      using (SqliteDataReader reader = ExecuteReader(command))
        while (reader.Read())
        {
          DateTime dateTime = convertDateTimeBasedOnConfiguration(DateTime.FromBinary(reader.GetInt64(0)), timeZoneToUse, exchange);
          result.Add(new Level1Data(dateTime, priceFormatMask, reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetDouble(6)));
        }

      return result;
    }

    public DataCache GetDataCache(string dataProviderName, string ticker, Resolution resolution, DateTime from, DateTime to)
    {
      switch (resolution)
      {
        case Resolution.Minutes:
        case Resolution.Hours:
        case Resolution.Days:
        case Resolution.Weeks:
        case Resolution.Months:
          return GetBarData(dataProviderName, ticker, from, to, resolution);

        case Resolution.Level1:
          return GetDataCache(dataProviderName, ticker, from, to);

        default:
          throw new ArgumentException("Unknown resolution.");
      }
    }

    //properties
    public IList<Resolution> SupportedDataResolutions { get => s_SupportedResolutions; }
    [ObservableProperty] bool m_isOptimizing;

    //methods
    /// <summary>
    /// Generic utility method to support deletion of objects using a specific id fieldname in the database. Returns the number of rows deleted.
    /// </summary>
    protected int Delete(string tableName, Guid id, string idFieldname = "Id")
    {
      int result = 0;
      lock (this) result = ExecuteCommand($"DELETE FROM {tableName} WHERE {idFieldname} = '{id.ToString()}'");
      return result;
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
    public override void CreateSchema()
    {
      lock (this)
      {
        //create general data tables if required
        CreateCountryTable();
        CreateHolidayTable();
        CreateExchangeTable();
        CreateSessionTable();
        CreateInstrumentGroupTable();
        CreateInstrumentGroupInstrumentTable();
        CreateInstrumentTable();
        CreateStockTable();
        CreateInstrumentSecondaryExchangeTable();
        CreateFundamentalsTable();

        //create data provider specific data tables if required
        foreach (var dataProvider in m_configurationService.DataProviders)
        {
          if (!requireTableDefinitions(dataProvider.Key)) continue;
          CreateCountryFundamentalAssociationTable(dataProvider.Key);
          CreateCountryFundamentalValuesTable(dataProvider.Key);
          CreateInstrumentFundamentalAssociationTable(dataProvider.Key);
          CreateInstrumentFundamentalValuesTable(dataProvider.Key);
          foreach (Resolution resolution in s_SupportedResolutions)
            CreateInstrumentDataTable(dataProvider.Key, resolution);
        }
      }
    }

    /// <summary>
    /// Creates the default objects used for the data model that can not be removed.
    /// </summary>
    public override void CreateDefaultObjects()
    {
      lock (this)
      {
        base.CreateDefaultObjects();
        if (GetRowCount(TableCountry, $"Id == '{Country.InternationalId.ToString()}'") == 0) CreateCountry(new Country(Country.InternationalId, Attributes.None, "", Country.InternationalIsoCode));
        if (GetRowCount(TableExchange, $"Id == '{Exchange.InternationalId.ToString()}'") == 0) CreateExchange(new Exchange(Exchange.InternationalId, Attributes.None, "", Country.InternationalId, "Global Exchange", Array.Empty<string>(), TimeZoneInfo.Utc, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Exchange.InternationalId, string.Empty));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Monday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Monday", Exchange.InternationalId, DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Tuesday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Tuesday", Exchange.InternationalId, DayOfWeek.Tuesday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Wednesday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Wednesday", Exchange.InternationalId, DayOfWeek.Wednesday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Thursday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Thursday", Exchange.InternationalId, DayOfWeek.Thursday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Friday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Friday", Exchange.InternationalId, DayOfWeek.Friday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Saturday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Saturday", Exchange.InternationalId, DayOfWeek.Saturday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
        if (GetRowCount(TableSession, $"ExchangeId == '{Exchange.InternationalId.ToString()}' AND DayOfWeek == {(int)DayOfWeek.Sunday}") == 0) CreateSession(new Session(Guid.NewGuid(), Attributes.None, string.Empty, "Sunday", Exchange.InternationalId, DayOfWeek.Sunday, new TimeOnly(0, 0), new TimeOnly(23, 59)));
      }
    }

    /// <summary>
    /// Drops the database schema.
    /// </summary>
    public void DropSchema()
    {
      //https://sqlite.org/lang_droptable.html
      lock (this)
      {
        DropTable(TableCountry);
        DropTable(TableHoliday);
        DropTable(TableExchange);
        DropTable(TableSession);
        DropTable(TableInstrumentGroup);
        DropTable(TableInstrumentGroupInstrument);
        DropTable(TableFundamentals);
        DropTable(TableInstrument);
        DropTable(TableStock);
        DropTable(TableInstrumentSecondaryExchange);
        DropTable(TableInstrumentData);
        DropIndex(IndexInstrumentData);

        //drop the data provider specific tables and indexes
        foreach (var dataProvider in m_configurationService.DataProviders)
        {
          DropTable(GetDataProviderDBName(dataProvider.Key, TableFundamentals));
          DropTable(GetDataProviderDBName(dataProvider.Key, TableCountryFundamentalAssociations));
          DropTable(GetDataProviderDBName(dataProvider.Key, TableCountryFundamentalValues));
          DropTable(GetDataProviderDBName(dataProvider.Key, TableInstrumentFundamentalAssociations));
          DropTable(GetDataProviderDBName(dataProvider.Key, TableInstrumentFundamentalValues));
          foreach (Resolution resolution in s_SupportedResolutions)
          {
            DropTable(GetDataProviderDBName(dataProvider.Key, TableInstrumentData, resolution));
            DropIndex(GetDataProviderDBName(dataProvider.Key, IndexInstrumentData, resolution));
          }
        }
      }
    }

    private void CreateCountryTable()
    {
      CreateTable(TableCountry,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        IsoCode TEXT
      ");
    }

    private void CreateHolidayTable()
    {
      CreateTable(TableHoliday,
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
      CreateTable(TableExchange,
      @"
        Id TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        CountryId TEXT,
        Name TEXT,
        TimeZone TEXT,
        LogoId TEXT,
        DefaultPriceDecimals INTEGER,
        DefaultMinimumMovement INTEGER,
        DefaultBigPointValue INTERGER,
        Url TEXT,
        AlternateNames TEXT
      ");
    }

    private void CreateSessionTable()
    {
      CreateTable(TableSession,
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
      CreateTable(TableInstrumentGroup,
      @"
        Id TEXT,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        ParentId TEXT,
        Name TEXT,
        Description TEXT,
        UserId TEXT,
        AlternateNames TEXT,
        PRIMARY KEY(Id, ParentId) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentGroupInstrumentTable()
    {
      //table to store instruments associated with a given instrument group
      CreateTable(TableInstrumentGroupInstrument,
      @"
        InstrumentGroupId TEXT,
        InstrumentTicker TEXT,
        PRIMARY KEY(InstrumentGroupId, InstrumentTicker) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentTable()
    {
      //base table of tradeable instruments
      CreateTable(TableInstrument,
      @"
        Ticker TEXT PRIMARY KEY ON CONFLICT REPLACE,
        AttributeSet INTEGER DEFAULT(0),
        Tag TEXT,
        Type INTEGER,
        Name TEXT,
        Description TEXT,
        PrimaryExchangeId TEXT,
        InceptionDate TYPE INTEGER,
        PriceDecimals INTEGER,
        MinimumMovement INTEGER,
        BigPointValue INTERGER,
        AlternateTickers TEXT,
        ExtendedProperties TEXT
      ");
    }

    private void CreateStockTable()
    {
      //base table of tradeable instruments
      CreateTable(TableStock,
      @"
        Ticker TEXT PRIMARY KEY ON CONFLICT REPLACE,
        MarketCap REAL,
        SharesOutstanding REAL,
        EmployeeCount NUMBER,
        Address TEXT,
        City TEXT,
        State TEXT,
        Zip TEXT,
        PhoneNumber TEXT,
        WebsiteUrl TEXT
      ");
    }

    private void CreateInstrumentSecondaryExchangeTable()
    {
      //table of secondary exchanges on which an instrument is listed
      CreateTable(TableInstrumentSecondaryExchange,
      @"
        InstrumentTicker TEXT,
        ExchangeId TEXT,
        PRIMARY KEY(InstrumentTicker, ExchangeId) ON CONFLICT REPLACE
      ");
    }

    private void CreateFundamentalsTable()
    {
      //stores the fundamental factor definitions, the values are stored in the instrument fundamentals table
      CreateTable(TableFundamentals,
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
      CreateTable(GetDataProviderDBName(dataProviderName, TableCountryFundamentalAssociations),
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
      CreateTable(GetDataProviderDBName(dataProviderName, TableCountryFundamentalValues),
      @"
        AssociationId TEXT,
        DateTime INTEGER,
        Value REAL,
        PRIMARY KEY (AssociationId, DateTime) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentFundamentalAssociationTable(string dataProviderName)
    {
      CreateTable(GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalAssociations),
      @"
        Id TEXT,
        FundamentalId TEXT,
        InstrumentTicker TEXT,
        PRIMARY KEY (Id, FundamentalId, InstrumentTicker) ON CONFLICT REPLACE
      ");
    }

    private void CreateInstrumentFundamentalValuesTable(string dataProviderName)
    {
      //stores the fundamental factor values recorded for an instrument on a specific date and time
      CreateTable(GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalValues),
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
      command.Parameters.AddWithValue("$tableName", GetDataProviderDBName(dataProviderName, TableInstrumentData, Resolution.Days));
      SqliteDataReader reader = command.ExecuteReader();
      return !reader.NextResult();
    }

    private void CreateInstrumentDataTable(string dataProviderName, Resolution resolution)
    {
      //stores the bar data that originated from a specific data provider, bar data is indexed to allow fast access to it
      string tableName = GetDataProviderDBName(dataProviderName, TableInstrumentData, resolution);
      string indexName = GetDataProviderDBName(dataProviderName, IndexInstrumentData, resolution);

      switch (resolution)
      {
        case Resolution.Minutes:
        case Resolution.Hours:
        case Resolution.Days:
        case Resolution.Weeks:
        case Resolution.Months:
          CreateTable(tableName,
          @"
            Ticker TEXT,
            DateTime INTEGER,
            Open REAL,
            High REAL,
            Low REAL,
            Close REAL,
            Volume REAL,
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
            BidSize REAL,
            Ask REAL,
            AskSize REAL,
            Last REAL,
            LastSize REAL,
            PRIMARY KEY(Ticker, DateTime) ON CONFLICT REPLACE
          ");
          CreateIndex(indexName, tableName, true, "Ticker,DateTime");
          break;
      }
    }

    /// <summary>
    /// Execute non-query method on the database.
    /// timeout = a timeout of zero is interpreted as an infinite timeout.
    /// </summary>
    public int ExecuteCommand(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database non-query - ${command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteNonQuery();
    }

    public object? ExecuteScalar(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database scalar read - ${command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteScalar();
    }

    /// <summary>
    /// Execute a database reader on the database.
    /// </summary>
    public SqliteDataReader ExecuteReader(string command, int timeout = -1)
    {
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteReader();
    }

    /// <summary>
    /// Clear the database for testing purposes.
    /// </summary>
    public override int ClearDatabase()
    {
      int result = 0;

      lock (this)
      {
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableCountry}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableHoliday}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableExchange}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableSession}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableInstrumentGroup}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableInstrumentGroupInstrument}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableInstrument}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableInstrumentSecondaryExchange}");
        result += ExecuteCommand($"DELETE FROM {Data.SqliteDatabase.TableFundamentals}");
        foreach (var dataProvider in m_configurationService.DataProviders)
        {
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Key, Data.SqliteDatabase.TableCountryFundamentalValues)}");
          result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Key, Data.SqliteDatabase.TableInstrumentFundamentalValues)}");

          foreach (Resolution resolution in SupportedDataResolutions)
            result += ExecuteCommand($"DELETE FROM {GetDataProviderDBName(dataProvider.Key, Data.SqliteDatabase.TableInstrumentData, resolution)}");
        }
      }

      return result;
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
        case Resolution.Minutes:
          suffix = TableInstrumentDataSuffixMinute;
          break;

        case Resolution.Hours:
          suffix = TableInstrumentDataSuffixHour;
          break;

        case Resolution.Days:
          suffix = TableInstrumentDataSuffixDay;
          break;

        case Resolution.Weeks:
          suffix = TableInstrumentDataSuffixWeekly;
          break;

        case Resolution.Months:
          suffix = TableInstrumentDataSuffixMonthly;
          break;

        case Resolution.Level1:
          suffix = TableInstrumentDataSuffixLevel1;
          break;

        default:
          throw new ArgumentException("Unknown data resolution");
      }

      //NOTE: DataProvider name must be database safe so it can not contain any non-alphanumeric characters and it has to be unique among the set of defined
      //      data providers - this is checked in the DataProvider constructor.
      return dataProviderName + name + suffix;
    }

    /// <summary>
    /// Cache the fundamental associations used for country fundamentals for faster access.
    /// </summary>
    private void CacheCountryFundamentalAssociations(string dataProviderName)
    {
      m_countryFundamentalAssociations.Clear();
      using (var reader = ExecuteReader($"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableCountryFundamentalAssociations)}"))
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
      using (var reader = ExecuteReader($"SELECT * FROM {GetDataProviderDBName(dataProviderName, TableInstrumentFundamentalAssociations)}"))
      {
        m_instrumentFundamentalAssociations.Remove(dataProviderName);
        AssociationCacheEntry cacheEntry = new AssociationCacheEntry();
        while (reader.Read())
          cacheEntry.Add(reader.GetGuid(1).GetHashCode() + reader.GetString(2).GetHashCode(), reader.GetGuid(0));
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
    private Guid? GetInstrumentFundamentalAssociationId(string dataProviderName, Guid fundamentalId, string instrumentTicker)
    {
      AssociationCacheEntry? cacheEntry;

      int associationHash = fundamentalId.GetHashCode() + instrumentTicker.GetHashCode();

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