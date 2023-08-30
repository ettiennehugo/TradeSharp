using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data;
using Moq;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;
using static TradeSharp.Data.IDataStoreService;
using TradeSharp.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System.Resources;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;
using System.Collections;
using static TradeSharp.Common.IConfigurationService;

namespace TradeSharp.Data.Testing
{
  [TestClass]
  public class SqliteDataStore
  {
    //constants


    //enums


    //types


    //attributes
    private Mock<IConfigurationService> m_configuration;
    private Dictionary<string, object> m_generalConfiguration;
    private Mock<IDataManagerService> m_dataManager;
    private Mock<IDataProvider> m_dataProvider1;
    private Mock<IDataProvider> m_dataProvider2;
    private IList<IDataProvider> m_dataProviders;
    private CultureInfo m_cultureEnglish;
    private CultureInfo m_cultureFrench;
    private CultureInfo m_cultureGerman;
    private RegionInfo m_regionInfo;
    private Data.SqliteDataStoreService m_dataStore;
    private Country m_country;
    private TimeZoneInfo m_timeZone;
    private Exchange m_exchange;
    private Instrument m_instrument;

    //constructors
    public SqliteDataStore()
    {
      //create instance components
      m_cultureEnglish = CultureInfo.GetCultureInfo("en-US");
      m_cultureFrench = CultureInfo.GetCultureInfo("fr-FR");
      m_cultureGerman = CultureInfo.GetCultureInfo("de-DE");
      m_regionInfo = new RegionInfo(m_cultureEnglish.Name);

      m_configuration = new Mock<IConfigurationService>(MockBehavior.Strict);
      m_configuration.Setup(x => x.CultureInfo).Returns(m_cultureEnglish);
      m_configuration.Setup(x => x.RegionInfo).Returns(m_regionInfo);
      m_configuration.Setup(x => x.CultureFallback).Returns(new List<CultureInfo>(1) { m_cultureEnglish, m_cultureFrench }); //we use m_cultureGerman as the ANY language fallback
      Type testDataProviderType = typeof(TradeSharp.Data.Testing.TestDataProvider);
      m_configuration.Setup(x => x.DataProviders).Returns(new Dictionary<string, string>() { { "TestDataProvider1", "TestDataProvider1" }, { "TestDataProvider2", "TestDataProvider2" } });

      m_generalConfiguration = new Dictionary<string, object>() {
          { IConfigurationService.GeneralConfiguration.TimeZone, (object)IConfigurationService.TimeZone.Local },
          { IConfigurationService.GeneralConfiguration.CultureFallback, new List<CultureInfo>(1) { m_cultureEnglish } },
          { IConfigurationService.GeneralConfiguration.DataStore, new IConfigurationService.DataStoreConfiguration(typeof(TradeSharp.Data.SqliteDataStoreService).ToString(), Path.GetTempPath() + "TradeSharpTest.db") }
      };

      m_configuration.Setup(x => x.General).Returns(m_generalConfiguration);

      m_dataProvider1 = new Mock<IDataProvider>().SetupAllProperties();
      m_dataProvider1.SetupGet(x => x.Name).Returns("TestDataProvider1");
      m_dataProvider2 = new Mock<IDataProvider>().SetupAllProperties();
      m_dataProvider2.SetupGet(x => x.Name).Returns("TestDataProvider2");
      m_dataProviders = new List<IDataProvider>();
      m_dataProviders.Add(m_dataProvider1.Object);
      m_dataProviders.Add(m_dataProvider2.Object);
      m_dataManager = new Mock<IDataManagerService>().SetupAllProperties();
      m_dataManager.SetupGet(x => x.DataProvider).Returns(m_dataProvider1.Object);
      m_dataManager.SetupGet(x => x.DataProviders).Returns(m_dataProviders);

      m_dataStore = new TradeSharp.Data.SqliteDataStoreService(m_configuration.Object);

      //remove stale data from previous tests - this is to ensure proper test isolation
      m_dataStore.ClearDatabase();

      //create common attributes used for testing
      m_country = new Country(m_dataStore, m_dataManager.Object, "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_exchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "TestExchange", m_timeZone);
      m_instrument = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Stock, "TEST", "TestInstrument", "TestInstrumentDescription", DateTime.Now.ToUniversalTime()); //database layer stores dates in UTC
    }

    //finalizers
    ~SqliteDataStore()
    {
      m_dataStore.DropSchema(); //erase test database
      m_dataStore.Dispose();
    }

    //interface implementations


    //properties


    //methods
    [TestMethod]
    public void CreateText_PersistData_Success()
    {
      Guid id = m_dataStore.CreateText("eng", "TestText");
      Assert.IsNotNull(id, "Null text Id returned.");

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = 'eng' " +
        $"AND Value = 'TestText' ")
      , "Language text entry not created in database.");
    }

    [TestMethod]
    public void UpdateText_PersistData_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureEnglish.ThreeLetterISOLanguageName, "TestText");
      m_dataStore.UpdateText(id, m_cultureFrench.ThreeLetterISOLanguageName, "TexteD'essai");

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureEnglish.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TestText' ")
      , "Language text entry for current culture not created in database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureFrench.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TexteD''essai' ")
      , "Language text entry for French culture not created in database.");
    }

    [TestMethod]
    public void DeleteText_AllLanguages_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureEnglish.ThreeLetterISOLanguageName, "TestText");
      m_dataStore.UpdateText(id, m_cultureFrench.ThreeLetterISOLanguageName, "TexteD'essai");

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureEnglish.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TestText' ")
      , "Language text entry for current culture not created in database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureFrench.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TexteD''essai' ")
      , "Language text entry for French culture not created in database.");

      //delete text in all languages
      Assert.AreEqual(2, m_dataStore.DeleteText(id), "DeleteText did not return the correct number of database rows deleted.");

      //check that database entries no longer exist
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureEnglish.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TestText' ")
      , "Language text entry for current culture not created in database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureFrench.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TexteD''essai' ")
      , "Language text entry for French culture not created in database.");
    }

    [TestMethod]
    public void DeleteText_SpecificLanguages_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureEnglish.ThreeLetterISOLanguageName, "TestText");
      m_dataStore.UpdateText(id, m_cultureFrench.ThreeLetterISOLanguageName, "TexteD'essai");

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureEnglish.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TestText' ")
      , "Language text entry for current culture not created in database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureFrench.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TexteD''essai' ")
      , "Language text entry for French culture not created in database.");

      //delete the French translation of the text
      Assert.AreEqual(1, m_dataStore.DeleteText(id, m_cultureFrench.ThreeLetterISOLanguageName), "DeleteText did not return the correct number of database rows deleted.");

      //check that English text still exists
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureEnglish.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TestText' ")
      , "Language text entry for current culture not created in database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableLanguageText,
        $"Id = '{id.ToString()}' " +
        $"AND IsoLang = '{m_cultureFrench.ThreeLetterISOLanguageName}' " +
        $"AND Value = 'TexteD''essai' ")
      , "Language text entry for French culture not created in database.");
    }

    [TestMethod]
    public void GetText_CurrentLanguage_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureEnglish.ThreeLetterISOLanguageName, "TestText");
      m_dataStore.UpdateText(id, m_cultureFrench.ThreeLetterISOLanguageName, "TexteD'essai");

      string textValue = m_dataStore.GetText(id);
      Assert.IsFalse(textValue.Length == 0, "Null text value returned.");
      Assert.AreEqual(textValue, "TestText", "GetText did not return the correct text for the current culture.");
    }

    [TestMethod]
    public void GetText_FallbackLanguage_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureFrench.ThreeLetterISOLanguageName, "TexteD'essai");
      string textValue = m_dataStore.GetText(id);
      Assert.AreEqual(textValue, "TexteD'essai", "GetText did not return the French text fallback value.");
    }

    [TestMethod]
    public void GetText_AnyLanguage_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureEnglish.ThreeLetterISOLanguageName, "Testtext");
      m_dataStore.UpdateText(id, m_cultureGerman.ThreeLetterISOLanguageName, "Prüfentext");
      m_dataStore.DeleteText(id, m_cultureEnglish.ThreeLetterISOLanguageName);
      string textValue = m_dataStore.GetText(id);
      Assert.AreEqual(textValue, "Prüfentext", "GetText did not return the any text fallback German text.");
    }

    [TestMethod]
    public void GetText_SpecificLanguage_Success()
    {
      Guid id = m_dataStore.CreateText(m_cultureEnglish.ThreeLetterISOLanguageName, "TestText");
      m_dataStore.UpdateText(id, m_cultureFrench.ThreeLetterISOLanguageName, "TexteD'essai");
      m_dataStore.UpdateText(id, m_cultureGerman.ThreeLetterISOLanguageName, "Prüfentext");

      string textValue = m_dataStore.GetText(id, m_cultureFrench.ThreeLetterISOLanguageName);
      Assert.AreEqual(textValue, "TexteD'essai", "GetText did not return the correct specific text for the French language.");

      textValue = m_dataStore.GetText(id, m_cultureGerman.ThreeLetterISOLanguageName);
      Assert.AreEqual(textValue, "Prüfentext", "GetText did not return the correct specific text for the German language.");
    }

    [TestMethod]
    public void CreateCountry_PersistData_Success()
    {
      m_dataStore.CreateCountry(new IDataStoreService.Country(m_country.Id, m_country.LanguageCode));
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.LanguageCode}'"), "Country data not persisted to database.");
    }

    [TestMethod]
    public void CreateCountryHoliday_DayOfMonth_Success()
    {
      Holiday holiday = new Holiday(m_dataStore, m_dataManager.Object, m_country, "CountryDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);

      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, m_country.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, holiday.DayOfMonth, 0, 0, holiday.MoveWeekendHoliday));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND ParentId = '{holiday.Country.Id.ToString()}' " +
        $"AND HolidayType = {(int)holiday.DayOfWeek} " +
        $"AND Month = {(int)holiday.Month} " +
        $"AND DayOfMonth = {(int)holiday.DayOfMonth} " +
        $"AND DayOfWeek = {(int)holiday.DayOfWeek} " +
        $"AND WeekOfMonth = {(int)holiday.WeekOfMonth} " +
        $"AND MoveWeekendHoliday = {(int)holiday.MoveWeekendHoliday} ")
      , "Country holiday for day of month not persisted to database.");

    }

    [TestMethod]
    public void CreateCountryHoliday_DayOfWeek_Success()
    {
      Holiday holiday = new Holiday(m_dataStore, m_dataManager.Object, m_country, "CountryDayOfWeek", Months.January, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);

      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, m_country.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, 0, holiday.DayOfWeek, holiday.WeekOfMonth, holiday.MoveWeekendHoliday));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND ParentId = '{holiday.Country.Id.ToString()}' " +
        $"AND HolidayType = {(int)holiday.DayOfWeek} " +
        $"AND Month = {(int)holiday.Month} " +
        $"AND DayOfMonth = {(int)holiday.DayOfMonth} " +
        $"AND DayOfWeek = {(int)holiday.DayOfWeek} " +
        $"AND WeekOfMonth = {(int)holiday.WeekOfMonth} " +
        $"AND MoveWeekendHoliday = {(int)holiday.MoveWeekendHoliday} ")
      , "Country holiday for day of week not persisted to database.");
    }

    [TestMethod]
    public void CreateExchange_PersistData_Success()
    {
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND CountryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");
    }

    [TestMethod]
    public void CreateHoliday_ExchangeDayOfMonth_Success()
    {
      ExchangeHoliday holiday = new ExchangeHoliday(m_dataStore, m_dataManager.Object, m_exchange, "ExchangeDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);

      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, m_exchange.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, holiday.DayOfMonth, 0, 0, holiday.MoveWeekendHoliday));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND ParentId = '{holiday.Exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)holiday.DayOfWeek} " +
        $"AND Month = {(int)holiday.Month} " +
        $"AND DayOfMonth = {(int)holiday.DayOfMonth} " +
        $"AND DayOfWeek = {(int)holiday.DayOfWeek} " +
        $"AND WeekOfMonth = {(int)holiday.WeekOfMonth} " +
        $"AND MoveWeekendHoliday = {(int)holiday.MoveWeekendHoliday} ")
      , "Exchange holiday for day of month not persisted to database.");

    }

    [TestMethod]
    public void CreateHoliday_ExchangeDayOfWeek_Success()
    {
      ExchangeHoliday holiday = new ExchangeHoliday(m_dataStore, m_dataManager.Object, m_exchange, "ExchangeDayOfWeek", Months.January, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);

      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(holiday.Id, m_exchange.Id, holiday.NameTextId, holiday.Name, holiday.Type, holiday.Month, 0, holiday.DayOfWeek, holiday.WeekOfMonth, holiday.MoveWeekendHoliday));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND ParentId = '{holiday.Exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)holiday.DayOfWeek} " +
        $"AND Month = {(int)holiday.Month} " +
        $"AND DayOfMonth = {(int)holiday.DayOfMonth} " +
        $"AND DayOfWeek = {(int)holiday.DayOfWeek} " +
        $"AND WeekOfMonth = {(int)holiday.WeekOfMonth} " +
        $"AND MoveWeekendHoliday = {(int)holiday.MoveWeekendHoliday} ")
      , "Exchange holiday for day of week not persisted to database.");
    }

    [TestMethod]
    public void CreateSession_PersistData_Success()
    {
      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "TestSession", startTime, endTime);

      m_dataStore.CreateSession(new IDataStoreService.Session(session.Id, session.NameTextId, session.Name, session.Exchange.Id, session.Day, session.Start, session.End));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{session.Exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = {session.Start.Ticks} " +
        $"AND EndTime = {session.End.Ticks} ")
      , "Session not persisted to database.");
    }

    [TestMethod]
    public void CreateCountryFundamental_PersistAssociation_Success()
    {
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      CountryFundamental countryFundamental = new CountryFundamental(fundamental, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalAssociations),
        $"Id = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{countryFundamental.Fundamental.Id.ToString()}' " +
        $"AND CountryId = '{countryFundamental.Country.Id.ToString()}'")
      , "Country fundamental association not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrumentFundamental_PersistAssociation_Success()
    {
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalAssociations),
        $"Id = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{instrumentFundamental.Fundamental.Id.ToString()}' " +
        $"AND InstrumentId = '{instrumentFundamental.Instrument.Id.ToString()}'")
      , "Instrument fundamental association not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrumentGroup_PersistData_Success()
    {
      InstrumentGroup instrumentGroup = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName", "TestInstrumentGroupDescription");

      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup.Id, instrumentGroup.Parent.Id, instrumentGroup.NameTextId, instrumentGroup.Name, instrumentGroup.DescriptionTextId, instrumentGroup.Description, new List<Guid> { m_instrument.Id }));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup.DescriptionTextId.ToString()}'")
      , "Instrument group not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group and instrument association not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrument_PersistData_Success()
    {
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrument_AdditionalExchangePersistData_Success()
    {
      Exchange exchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "SecondaryTestExchange", m_timeZone);
      m_dataStore.CreateInstrument(m_instrument.Id, exchange.Id);

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentSecondaryExchange,
        $"InstrumentId = '{m_instrument.Id.ToString()}' " +
        $"AND ExchangeId = '{exchange.Id.ToString()}' ")
      , "Secondary exchange not persisted.");
    }

    [TestMethod]
    public void CreateFundamental_PersistData_Success()
    {
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableFundamentals,
        $"Id = '{fundamental.Id.ToString()}' " +
        $"AND Category = {(int)fundamental.Category} " +
        $"AND ReleaseInterval = {(int)fundamental.ReleaseInterval}")
      , "Fundamental not persisted to database.");
    }
    
    [TestMethod]
    public void UpdateSession_ChangeDayAndTime_Success()
    {
      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "TestSession", startTime, endTime);

      m_dataStore.CreateSession(new IDataStoreService.Session(session.Id, session.NameTextId, session.Name, session.Exchange.Id, session.Day, session.Start, session.End));

      m_dataStore.UpdateSession(session.Id, DayOfWeek.Tuesday, startTime.AddMinutes(5), endTime.AddMinutes(5));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{session.Exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Tuesday} " +
        $"AND StartTime = {startTime.AddMinutes(5).Ticks} " +
        $"AND EndTime = {endTime.AddMinutes(5).Ticks} ")
      , "Session not updated in database.");
    }

    [TestMethod]
    public void UpdateInstrument_ChangeAllAttributes_Success()
    {
      Exchange exchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "SecondaryTestExchange", m_timeZone);
      InstrumentGroup instrumentGroup = new InstrumentGroup(m_dataStore, m_dataManager.Object, "Test Instrument Group", "Test Instrument Group Description");
      DateTime dateTime = DateTime.Now.AddDays(3);

      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.UpdateInstrument(m_instrument.Id, exchange.Id, "NEW", dateTime);

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = 'NEW' " +
        $"AND PrimaryExchangeId = '{exchange.Id.ToString()}' " +
        $"AND InceptionDate = {dateTime.ToUniversalTime().ToBinary()}")
      , "Instrument not updated in database.");
    }

    [TestMethod]
    public void UpdateCountryFundamental_PersistData_Success()
    {
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      CountryFundamental countryFundamental = new CountryFundamental(fundamental, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");
    }

    [TestMethod]
    public void UpdateInstrumentFundamental_PersistData_Success()
    {
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, fundamental.Id, m_instrument.Id, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    public void UpdateData_SingleBarDataPersist_Success(Resolution resolution, PriceDataType priceDataType)
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double open = 1.0;
      double high = 2.0;
      double low = 3.0;
      double close = 4.0;
      long volume = 5;

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id,m_instrument.Ticker, resolution, dateTime, open, high, low, close, volume, priceDataType == PriceDataType.Synthetic);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, priceDataType == PriceDataType.Synthetic ? Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic : Data.SqliteDataStoreService.c_TableInstrumentData, resolution),
        $"Ticker = '{m_instrument.Ticker}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Open = {open.ToString()} " +
        $"AND High = {high.ToString()} " +
        $"AND Low = {low.ToString()} " +
        $"AND Close = {close.ToString()} " +
        $"AND Volume = {volume.ToString()} ")
      , "Actual bar value not persisted to database.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    public void UpdateData_SingleBarDataUpdate_Success(Resolution resolution, PriceDataType priceDataType)
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double open = 1.0;
      double high = 2.0;
      double low = 3.0;
      double close = 4.0;
      long volume = 5;

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, open, high, low, close, volume, priceDataType == PriceDataType.Synthetic);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, priceDataType == PriceDataType.Synthetic ? Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic : Data.SqliteDataStoreService.c_TableInstrumentData, resolution),
        $"Ticker = '{m_instrument.Ticker}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Open = {open.ToString()} " +
        $"AND High = {high.ToString()} " +
        $"AND Low = {low.ToString()} " +
        $"AND Close = {close.ToString()} " +
        $"AND Volume = {volume.ToString()} ")
      , "Original bar value not persisted to database.");

      //update same bar again with different values
      close = 9.0;
      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, open, high, low, close, volume, priceDataType == PriceDataType.Synthetic);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, priceDataType == PriceDataType.Synthetic ? Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic : Data.SqliteDataStoreService.c_TableInstrumentData, resolution),
        $"Ticker = '{m_instrument.Ticker}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Open = {open.ToString()} " +
        $"AND High = {high.ToString()} " +
        $"AND Low = {low.ToString()} " +
        $"AND Close = {close.ToString()} " +
        $"AND Volume = {volume.ToString()} ")
      , "Updated bar value not persisted to database.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    public void UpdateData_RangeBarDataPersist_Success(Resolution resolution, PriceDataType priceDataType)
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      BarData barData = new IDataStoreService.BarData(5);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155 };
      barData.Synthetic = new List<bool> { priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic };

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      for (int index = 0; index < barData.Count; index++)
      {
        Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, priceDataType == PriceDataType.Synthetic ? Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic : Data.SqliteDataStoreService.c_TableInstrumentData, resolution),
          $"Ticker = '{m_instrument.Ticker}' " +
          $"AND DateTime = {barData.DateTime[index].ToBinary()} " +
          $"AND Open = {barData.Open[index].ToString()} " +
          $"AND High = {barData.High[index].ToString()} " +
          $"AND Low = {barData.Low[index].ToString()} " +
          $"AND Close = {barData.Close[index].ToString()} " +
          $"AND Volume = {barData.Volume[index].ToString()} ")
        , "Original bar value from list not persisted to database.");
      }
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    public void UpdateData_RangeBarDataUpdate_Success(Resolution resolution, PriceDataType priceDataType)
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      BarData barData = new IDataStoreService.BarData(5);
      barData.DateTime = new List<DateTime>{ dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155 };
      barData.Synthetic = new List<bool> { priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic};

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      for (int index = 0; index < barData.Count; index++)
      {
        Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, priceDataType == PriceDataType.Synthetic ? Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic : Data.SqliteDataStoreService.c_TableInstrumentData, resolution),
          $"Ticker = '{m_instrument.Ticker}' " +
          $"AND DateTime = {barData.DateTime[index].ToBinary()} " +
          $"AND Open = {barData.Open[index].ToString()} " +
          $"AND High = {barData.High[index].ToString()} " +
          $"AND Low = {barData.Low[index].ToString()} " +
          $"AND Close = {barData.Close[index].ToString()} " +
          $"AND Volume = {barData.Volume[index].ToString()} ")
        , "Original bar value from list not persisted to database.");
      }

      barData = new IDataStoreService.BarData(5);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5) };
      barData.Open = new List<double> { 211.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 222.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 233.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 244.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 555 };
      barData.Synthetic = new List<bool> { priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic };

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      for (int index = 0; index < barData.Count; index++)
      {
        Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, priceDataType == PriceDataType.Synthetic ? Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic : Data.SqliteDataStoreService.c_TableInstrumentData, resolution),
          $"Ticker = '{m_instrument.Ticker}' " +
          $"AND DateTime = {barData.DateTime[index].ToBinary()} " +
          $"AND Open = {barData.Open[index].ToString()} " +
          $"AND High = {barData.High[index].ToString()} " +
          $"AND Low = {barData.Low[index].ToString()} " +
          $"AND Close = {barData.Close[index].ToString()} " +
          $"AND Volume = {barData.Volume[index].ToString()} ")
        , "Original bar value from list not replaced in database.");
      }
    }

    [TestMethod]
    public void DeleteCountry_DataRemoved_Success()
    {
      m_dataStore.CreateCountry(new IDataStoreService.Country(m_country.Id, m_country.LanguageCode));
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.LanguageCode}'"), "Country data not persisted to database.");
      Assert.AreEqual(1, m_dataStore.DeleteCountry(m_country.Id), "DeleteCountry did not return the correct number of rows removed");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.LanguageCode}'"), "Country data not persisted to database.");
    }

    [TestMethod]
    public void DeleteCountry_CountryAndRelatedDataRemoved_Success()
    {
      //create country and a related objects
      m_dataStore.CreateCountry(new IDataStoreService.Country(m_country.Id, m_country.LanguageCode));
      Holiday countryHolidayDayOfMonth = new Holiday(m_dataStore, m_dataManager.Object, m_country, "CountryDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(countryHolidayDayOfMonth.Id, m_country.Id, countryHolidayDayOfMonth.NameTextId, countryHolidayDayOfMonth.Name, countryHolidayDayOfMonth.Type, countryHolidayDayOfMonth.Month, countryHolidayDayOfMonth.DayOfMonth, 0, 0, countryHolidayDayOfMonth.MoveWeekendHoliday));

      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));
      ExchangeHoliday exchangeHolidayDayOfMonth = new ExchangeHoliday(m_dataStore, m_dataManager.Object, m_exchange, "ExchangeDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(exchangeHolidayDayOfMonth.Id, m_exchange.Id, exchangeHolidayDayOfMonth.NameTextId, exchangeHolidayDayOfMonth.Name, exchangeHolidayDayOfMonth.Type, exchangeHolidayDayOfMonth.Month, exchangeHolidayDayOfMonth.DayOfMonth, 0, 0, exchangeHolidayDayOfMonth.MoveWeekendHoliday));

      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "TestSession", startTime, endTime);
      m_dataStore.CreateSession(new IDataStoreService.Session(session.Id, session.NameTextId, session.Name, session.Exchange.Id, session.Day, session.Start, session.End));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));

      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      CountryFundamental countryFundamental = new CountryFundamental(fundamental, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);

      //confirm that data was correctly persisted
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.LanguageCode}'"), "Country data not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{countryHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{countryHolidayDayOfMonth.Country.Id.ToString()}' " +
        $"AND HolidayType = {(int)countryHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)countryHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {countryHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)countryHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)countryHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Country holiday for day of month not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND CountryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{exchangeHolidayDayOfMonth.Exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)exchangeHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)exchangeHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {exchangeHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)exchangeHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)exchangeHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Exchange holiday for day of month not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}'")
      , "Instrument not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalAssociations),
        $"Id = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{countryFundamental.Fundamental.Id.ToString()}' " +
        $"AND CountryId = '{countryFundamental.Country.Id.ToString()}'")
      , "Country fundamental association not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");

      //delete country
      Assert.AreEqual(8, m_dataStore.DeleteCountry(m_country.Id), "DeleteCountry did not return the correct number of rows removed");

      //check that country and all it's related data was removed
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.LanguageCode}'"), "Country data not persisted to database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{countryHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{countryHolidayDayOfMonth.Country.Id.ToString()}' " +
        $"AND HolidayType = {(int)countryHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)countryHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {countryHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)countryHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)countryHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Country holiday for day of month not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{exchangeHolidayDayOfMonth.Exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)exchangeHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)exchangeHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {exchangeHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)exchangeHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)exchangeHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Exchange holiday for day of month not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}'")
      , "Instrument not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalAssociations),
        $"Id = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{countryFundamental.Fundamental.Id.ToString()}' " +
        $"AND CountryId = '{countryFundamental.Country.Id.ToString()}'")
      , "Country fundamental association not persisted to database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");
    }

    [TestMethod]
    public void DeleteExchange_DataRemoved_Success()
    {
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");

      Assert.AreEqual(1, m_dataStore.DeleteExchange(m_exchange.Id), "DeleteExchange did not return the correct number of rows removed");

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not removed from database.");
    }

    [TestMethod]
    public void DeleteExchange_ExchangeAndRelatedDataRemoved_Success()
    {
      //create exchange and related objects
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));
      ExchangeHoliday exchangeHolidayDayOfMonth = new ExchangeHoliday(m_dataStore, m_dataManager.Object, m_exchange, "ExchangeDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(exchangeHolidayDayOfMonth.Id, m_exchange.Id, exchangeHolidayDayOfMonth.NameTextId, exchangeHolidayDayOfMonth.Name, exchangeHolidayDayOfMonth.Type, exchangeHolidayDayOfMonth.Month, exchangeHolidayDayOfMonth.DayOfMonth, 0, 0, exchangeHolidayDayOfMonth.MoveWeekendHoliday));

      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "TestSession", startTime, endTime);
      m_dataStore.CreateSession(new IDataStoreService.Session(session.Id, session.NameTextId, session.Name, session.Exchange.Id, session.Day, session.Start, session.End));

      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));

      //check that data was properly persisted
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{m_exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)HolidayType.DayOfMonth} " +
        $"AND Month = {(int)Months.January} " +
        $"AND DayOfMonth = {1} " +
        $"AND MoveWeekendHoliday = {(int)MoveWeekendHoliday.DontAdjust}")
      , "Exchange holiday for day of month not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}'")
      , "Instrument not persisted to database.");

      //delete exchange
      Assert.AreEqual(4, m_dataStore.DeleteExchange(m_exchange.Id), "DeleteExchange did not return the correct number of rows removed");

      //check that exchange data and all related objects were removed
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{m_exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)HolidayType.DayOfMonth} " +
        $"AND Month = {(int)Months.January} " +
        $"AND DayOfMonth = {1} " +
        $"AND MoveWeekendHoliday = {(int)MoveWeekendHoliday.DontAdjust}")
      , "Exchange holiday for day of month not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}'")
      , "Instrument not deleted from database.");
    }

    [TestMethod]
    public void DeleteSession_DataRemoved_Success()
    {
      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "TestSession", startTime, endTime);

      m_dataStore.CreateSession(new IDataStoreService.Session(session.Id, session.NameTextId, session.Name, session.Exchange.Id, session.Day, session.Start, session.End));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = {session.Start.Ticks} " +
        $"AND EndTime = {session.End.Ticks} ")
      , "Session not persisted to database.");

      Assert.AreEqual(1, m_dataStore.DeleteSession(session.Id), "DeleteSession did not return the correct number of rows removed");

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableExchangeSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.Day} " +
        $"AND StartTime = {session.Start.Ticks} " +
        $"AND EndTime = {session.End.Ticks} ")
      , "Session not removed from database.");
    }

    [TestMethod]
    public void DeleteFundamental_DataRemoved_Success()
    {
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableFundamentals,
        $"Id = '{fundamental.Id.ToString()}' " +
        $"AND Category = {(int)fundamental.Category} " +
        $"AND ReleaseInterval = {(int)fundamental.ReleaseInterval}")
      , "Fundamental not persisted to database.");

      Assert.AreEqual(1, m_dataStore.DeleteFundamental(fundamental.Id), "DeleteFundamental did not return the correct number of rows removed");

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableFundamentals,
        $"Id = '{fundamental.Id.ToString()}' " +
        $"AND Category = {(int)fundamental.Category} " +
        $"AND ReleaseInterval = {(int)fundamental.ReleaseInterval}")
      , "Fundamental not removed from database.");
    }

    [TestMethod]
    public void DeleteFundamental_CountryFundamentalAndRelatedDataRemoved_Success()
    {
      //create countryFundamental and associated value1
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      CountryFundamental countryFundamental = new CountryFundamental(fundamental, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(2, m_dataStore.DeleteFundamental(fundamental.Id), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed as well
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");
    }

    [TestMethod]
    public void DeleteFundamental_CountryForDataProvider_Success()
    {
      Fundamental fundamental1 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental1", "TestCountryFundamentalDescription1", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental1.Id, fundamental1.NameTextId, fundamental1.Name, fundamental1.DescriptionTextId, fundamental1.Description, fundamental1.Category, fundamental1.ReleaseInterval));
      Fundamental fundamental2 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental2", "TestCountryFundamentalDescription2", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental2.Id, fundamental2.NameTextId, fundamental2.Name, fundamental2.DescriptionTextId, fundamental2.Description, fundamental2.Category, fundamental2.ReleaseInterval));

      CountryFundamental countryFundamental1 = new CountryFundamental(fundamental1, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental1 = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental1.Fundamental.Id, countryFundamental1.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental1);
      countryFundamental1.AssociationId = sCountryFundamental1.AssociationId;

      CountryFundamental countryFundamental2 = new CountryFundamental(fundamental2, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental2 = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental2.Fundamental.Id, countryFundamental2.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental2);
      countryFundamental2.AssociationId = sCountryFundamental2.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental1.FundamentalId, countryFundamental1.CountryId, dateTime, value);
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental2.FundamentalId, countryFundamental2.CountryId, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value2 not persisted to database.");

      m_dataStore.DeleteCountryFundamental(m_dataProvider1.Object.Name, fundamental1.Id, m_country.Id);

      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value1 not removed from database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value2 incorrectly removed from database.");
    }

    [TestMethod]
    public void DeleteFundamentalValue_CountryForDataProvider_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      CountryFundamental countryFundamental = new CountryFundamental(fundamental, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;
      DateTime dateTime2 = DateTime.Now.AddDays(1).ToUniversalTime();
      double value2 = 2.0;

      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime1, value1);
      m_dataStore.UpdateCountryFundamental(m_dataProvider2.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime2, value2);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value2 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_dataStore.DeleteFundamentalValues(m_dataProvider1.Object.Name, fundamental.Id), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed for data provider 1 but that data provider 2's data is still present
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value not removed from database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteInstrumentFundamental_ForDataProvider_Success()
    {
      Fundamental fundamental1 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental1", "TestInstrumentFundamentalDescription1", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental1.Id, fundamental1.NameTextId, fundamental1.Name, fundamental1.DescriptionTextId, fundamental1.Description, fundamental1.Category, fundamental1.ReleaseInterval));
      Fundamental fundamental2 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental2", "TestInstrumentFundamentalDescription2", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental2.Id, fundamental2.NameTextId, fundamental2.Name, fundamental2.DescriptionTextId, fundamental2.Description, fundamental2.Category, fundamental2.ReleaseInterval));

      InstrumentFundamental instrumentFundamental1 = new InstrumentFundamental(fundamental1, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental1 = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental1.Fundamental.Id, instrumentFundamental1.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental1);
      instrumentFundamental1.AssociationId = sInstrumentFundamental1.AssociationId;

      InstrumentFundamental instrumentFundamental2 = new InstrumentFundamental(fundamental2, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental2 = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental2.Fundamental.Id, instrumentFundamental2.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental2);
      instrumentFundamental2.AssociationId = sInstrumentFundamental2.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental1.FundamentalId, instrumentFundamental1.InstrumentId, dateTime, value);
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental2.FundamentalId, instrumentFundamental2.InstrumentId, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value2 not persisted to database.");

      m_dataStore.DeleteInstrumentFundamental(m_dataProvider1.Object.Name, fundamental1.Id, m_instrument.Id);

      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value1 not removed from database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value2 incorrectly removed from database.");
    }

    [TestMethod]
    public void DeleteFundamentalValues_InstrumentFundamentalAndRelatedDataRemoved_Success()
    {
      //create instrumentFundamental and associated values
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;
      DateTime dateTime2 = DateTime.Now.AddDays(1).ToUniversalTime();
      double value2 = 2.0;

      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime1, value1);
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider2.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime2, value2);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value2 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_dataStore.DeleteFundamentalValues(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed for data provider 1 but not data provider 2
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value not removed from database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteFundamentalValues_InstrumentForDataProvider_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;

      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.Instrument.Id, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_dataStore.DeleteFundamentalValues(instrumentFundamental.FundamentalId), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed as well
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not removed from database.");
    }

    [TestMethod]
    public void DeleteCountryFundamentalValue_DataRemoved_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      CountryFundamental countryFundamental = new CountryFundamental(fundamental, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;

      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.Country.Id, dateTime1, value1);

      DateTime dateTime2 = DateTime.Now.ToUniversalTime();
      double value2 = 2.0;

      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.Country.Id, dateTime2, value2);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value 1 not persisted to database.");

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value 2 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_dataStore.DeleteCountryFundamentalValue(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.Country.Id, dateTime1), "DeleteCountry did not return the correct number of rows removed");

      //check that countryFundamental data value1 are removed
      //first value1 must be removed
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value not removed from database.");

      //second value1 must be present
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value 2 present in database after delete operation.");
    }

    [TestMethod]
    public void DeleteInstrumentFundamentalValue_DataRemoved_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;

      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.Instrument.Id, dateTime1, value1);

      DateTime dateTime2 = DateTime.Now.ToUniversalTime();
      double value2 = 2.0;

      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.Instrument.Id, dateTime2, value2);

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value 1 not persisted to database.");

      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value 1 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_dataStore.DeleteInstrumentFundamentalValue(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.Instrument.Id, dateTime1), "DeleteInstrument did not return the correct number of rows removed");

      //check that countryFundamental data value1 is removed
      //first value1 must be removed
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value 1 not removed from database.");

      //second value1 must be present
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value 2 present in database after delete operation.");
    }

    [TestMethod]
    public void UpdateInstrumentGroup_ChangeParent_Success()
    {
      InstrumentGroup instrumentGroup1 = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1"); 
      InstrumentGroup instrumentGroup2 = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName2", "TestInstrumentGroupDescription2");

      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup1.Id, instrumentGroup1.Parent.Id, instrumentGroup1.NameTextId, instrumentGroup1.Name, instrumentGroup1.DescriptionTextId, instrumentGroup1.Description, new List<Guid> { m_instrument.Id }));
      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup2.Id, instrumentGroup2.Parent.Id, instrumentGroup2.NameTextId, instrumentGroup2.Name, instrumentGroup2.DescriptionTextId, instrumentGroup2.Description, new List<Guid> { m_instrument.Id }));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup1.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup1.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup1.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup1.DescriptionTextId.ToString()}'")
      , "Instrument group 1 not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup2.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup2.DescriptionTextId.ToString()}'")
      , "Instrument group 2 not persisted to database.");

      m_dataStore.UpdateInstrumentGroup(instrumentGroup1.Id, instrumentGroup2.Id);  //make instrument group 2 the parent of instrument group 1

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup1.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup1.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup1.DescriptionTextId.ToString()}'")
      , "Instrument group 1 not persisted to database.");
    }

    [TestMethod]
    public void DeleteInstrumentGroup_DeleteAndPersist_Success()
    {
      InstrumentGroup instrumentGroup1 = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1");
      InstrumentGroup instrumentGroup2 = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName2", "TestInstrumentGroupDescription2", instrumentGroup1);

      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup1.Id, instrumentGroup1.Parent.Id, instrumentGroup1.NameTextId, instrumentGroup1.Name, instrumentGroup1.DescriptionTextId, instrumentGroup1.Description, new List<Guid> { m_instrument.Id }));
      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup2.Id, instrumentGroup2.Parent.Id, instrumentGroup2.NameTextId, instrumentGroup2.Name, instrumentGroup2.DescriptionTextId, instrumentGroup2.Description, new List<Guid> { m_instrument.Id }));

      m_dataStore.DeleteInstrumentGroup(instrumentGroup1.Id);

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup1.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup1.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup1.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup1.DescriptionTextId.ToString()}'")
      , "Instrument group 1 not persisted to database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group 1 and instrument association not removed from database.");

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup2.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup2.DescriptionTextId.ToString()}'")
      , "Instrument group 2 not persisted to database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup2.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group 2 and instrument association not removed from database.");
    }

    [TestMethod]
    public void DeleteInstumentGroupChild_Update_Success()
    {
      InstrumentGroup instrumentGroup1 = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1");
      InstrumentGroup instrumentGroup2 = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName2", "TestInstrumentGroupDescription2", instrumentGroup1);

      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup1.Id, instrumentGroup1.Parent.Id, instrumentGroup1.NameTextId, instrumentGroup1.Name, instrumentGroup1.DescriptionTextId, instrumentGroup1.Description, new List<Guid> { m_instrument.Id }));
      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup2.Id, instrumentGroup2.Parent.Id, instrumentGroup2.NameTextId, instrumentGroup2.Name, instrumentGroup2.DescriptionTextId, instrumentGroup2.Description, new List<Guid> { m_instrument.Id }));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup2.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup2.DescriptionTextId.ToString()}'")
      , "Instrument group 2 not persisted to database.");

      m_dataStore.DeleteInstrumentGroupChild(instrumentGroup1.Id, instrumentGroup2.Id);

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{InstrumentGroupRoot.Instance.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup2.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup2.DescriptionTextId.ToString()}'")
      , "Instrument group 2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteInstrument_DataRemoved_Success()
    {
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not persisted to database.");

      m_dataStore.DeleteInstrument(m_instrument.Id, m_instrument.Ticker);

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not deleted from database.");
    }

    [TestMethod]
    public void DeleteInstrument_InstrumentAndRelatedDataRemoved_Success()
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();  //bar data must always be stored in UTC datetime
      BarData barData = new BarData(10);
      barData.DateTime = new List<DateTime> { dateTime.AddDays(1), dateTime.AddDays(2), dateTime.AddDays(3), dateTime.AddDays(4), dateTime.AddDays(5), dateTime.AddDays(6), dateTime.AddDays(7), dateTime.AddDays(8), dateTime.AddDays(9), dateTime.AddDays(10) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0, 211.0, 221.0, 231.0, 241.0, 251.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0, 212.0, 222.0, 232.0, 242.0, 252.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0, 213.0, 223.0, 233.0, 243.0, 253.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0, 214.0, 224.0, 234.0, 244.0, 254.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155, 215, 225, 235, 245, 255 };
      barData.Synthetic = new List<bool> { true, false, true, false, true, false, true, false, true, false };

      InstrumentGroup instrumentGroup = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName", "TestInstrumentGroupDescription");

      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup.Id, instrumentGroup.Parent.Id, instrumentGroup.NameTextId, instrumentGroup.Name, instrumentGroup.DescriptionTextId, instrumentGroup.Description, new List<Guid> { m_instrument.Id }));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));      
      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, Resolution.Day, barData);

      Fundamental fundamental = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental.Id, fundamental.NameTextId, fundamental.Name, fundamental.DescriptionTextId, fundamental.Description, fundamental.Category, fundamental.ReleaseInterval));

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      double value = 1.0;

      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.Instrument.Id, dateTime, value);

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not persisted to database.");
      Assert.AreEqual(5, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentData, Resolution.Day),
        $"Ticker = '{m_instrument.Ticker}'")
      , "Actual bar values from list not persisted to database.");
      Assert.AreEqual(5, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic, Resolution.Day),
        $"Ticker = '{m_instrument.Ticker}'")
      , "Synthetic bar values from list not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group and instrument association not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalAssociations),
        $"Id = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND InstrumentId = '{instrumentFundamental.Instrument.Id.ToString()}'")
      , "Instrument fundamental association not persisted to database.");
      Assert.AreEqual(1, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");

      Assert.AreEqual(14, m_dataStore.DeleteInstrument(m_instrument.Id, m_instrument.Ticker), "Delete instrument returned the incorrect number of rows removed");

      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchange.Id.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentData, Resolution.Day),
        $"Ticker = '{m_instrument.Ticker}'")
      , "Actual bar values from list not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentDataSynthetic, Resolution.Day),
        $"Ticker = '{m_instrument.Ticker}'")
      , "Synthetic bar values from list not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group and instrument association not deleted from database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalAssociations),
        $"Id = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND InstrumentId = '{instrumentFundamental.Instrument.Id.ToString()}'")
      , "Instrument fundamental association not persisted to database.");
      Assert.AreEqual(0, m_dataStore.GetRowCount(m_dataStore.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDataStoreService.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");
    }

    [TestMethod]
    public void GetHolidays_CountryReturnPersistedData_Success()
    {
      Holiday countryHolidayDayOfMonth = new Holiday(m_dataStore, m_dataManager.Object, m_country, "CountryDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);
      Holiday countryHolidayDayOfWeek = new Holiday(m_dataStore, m_dataManager.Object, m_country, "CountryDayOfWeek", Months.January, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);

      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(countryHolidayDayOfMonth.Id, m_country.Id, countryHolidayDayOfMonth.NameTextId, countryHolidayDayOfMonth.Name, countryHolidayDayOfMonth.Type, countryHolidayDayOfMonth.Month, countryHolidayDayOfMonth.DayOfMonth, 0, 0, countryHolidayDayOfMonth.MoveWeekendHoliday));
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(countryHolidayDayOfWeek.Id, m_country.Id, countryHolidayDayOfWeek.NameTextId, countryHolidayDayOfWeek.Name, countryHolidayDayOfWeek.Type, countryHolidayDayOfWeek.Month, 0, countryHolidayDayOfWeek.DayOfWeek, countryHolidayDayOfWeek.WeekOfMonth, countryHolidayDayOfWeek.MoveWeekendHoliday));

      IList<IDataStoreService.Holiday> holidays = m_dataStore.GetHolidays();
      Assert.AreEqual(2, holidays.Count, "Returned country holiday count is not correct.");
      Assert.IsNotNull(holidays.Where(x => x.Id == countryHolidayDayOfMonth.Id && x.NameTextId == countryHolidayDayOfMonth.NameTextId && x.Type == countryHolidayDayOfMonth.Type &&
                                    x.Month == countryHolidayDayOfMonth.Month && countryHolidayDayOfMonth.DayOfMonth == countryHolidayDayOfMonth.DayOfMonth && x.MoveWeekendHoliday == countryHolidayDayOfMonth.MoveWeekendHoliday).Single(), "countryHolidayDayOfMonth not returned in stored data.");
      Assert.IsNotNull(holidays.Where(x => x.Id == countryHolidayDayOfWeek.Id && x.NameTextId == countryHolidayDayOfWeek.NameTextId && x.Type == countryHolidayDayOfWeek.Type &&
                                    x.Month == countryHolidayDayOfWeek.Month && countryHolidayDayOfWeek.DayOfMonth == countryHolidayDayOfWeek.DayOfMonth && x.MoveWeekendHoliday == countryHolidayDayOfWeek.MoveWeekendHoliday).Single(), "countryHolidayDayOfWeek not returned in stored data.");
    }

    [TestMethod]
    public void GetHolidays_ExchangeReturnPersistedData_Success()
    {
      ExchangeHoliday exchangeHolidayDayOfMonth = new ExchangeHoliday(m_dataStore, m_dataManager.Object, m_exchange, "ExchangeDayOfMonth", Months.January, 1, MoveWeekendHoliday.DontAdjust);
      ExchangeHoliday exchangeHolidayDayOfWeek = new ExchangeHoliday(m_dataStore, m_dataManager.Object, m_exchange, "ExchangeDayOfWeek", Months.January, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);

      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(exchangeHolidayDayOfMonth.Id, m_country.Id, exchangeHolidayDayOfMonth.NameTextId, exchangeHolidayDayOfMonth.Name, exchangeHolidayDayOfMonth.Type, exchangeHolidayDayOfMonth.Month, exchangeHolidayDayOfMonth.DayOfMonth, 0, 0, exchangeHolidayDayOfMonth.MoveWeekendHoliday));
      m_dataStore.CreateHoliday(new IDataStoreService.Holiday(exchangeHolidayDayOfWeek.Id, m_country.Id, exchangeHolidayDayOfWeek.NameTextId, exchangeHolidayDayOfWeek.Name, exchangeHolidayDayOfWeek.Type, exchangeHolidayDayOfWeek.Month, 0, exchangeHolidayDayOfWeek.DayOfWeek, exchangeHolidayDayOfWeek.WeekOfMonth, exchangeHolidayDayOfWeek.MoveWeekendHoliday));

      IList<IDataStoreService.Holiday> holidays = m_dataStore.GetHolidays();
      Assert.AreEqual(2, holidays.Count, "Returned exchange holiday count is not correct.");
      Assert.IsNotNull(holidays.Where(x => x.Id == exchangeHolidayDayOfMonth.Id && x.NameTextId == exchangeHolidayDayOfMonth.NameTextId && x.Type == exchangeHolidayDayOfMonth.Type &&
                                    x.Month == exchangeHolidayDayOfMonth.Month && exchangeHolidayDayOfMonth.DayOfMonth == exchangeHolidayDayOfMonth.DayOfMonth && x.MoveWeekendHoliday == exchangeHolidayDayOfMonth.MoveWeekendHoliday).Single(), "exchangeHolidayDayOfMonth not returned in stored data.");
      Assert.IsNotNull(holidays.Where(x => x.Id == exchangeHolidayDayOfWeek.Id && x.NameTextId == exchangeHolidayDayOfWeek.NameTextId && x.Type == exchangeHolidayDayOfWeek.Type &&
                                    x.Month == exchangeHolidayDayOfWeek.Month && exchangeHolidayDayOfWeek.DayOfMonth == exchangeHolidayDayOfWeek.DayOfMonth && x.MoveWeekendHoliday == exchangeHolidayDayOfWeek.MoveWeekendHoliday).Single(), "exchangeHolidayDayOfWeek not returned in stored data.");
    }

    [TestMethod]
    public void GetExchanges_ReturnPersistedData_Success()
    {
      Exchange secondExchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "Second test exchange", m_timeZone);
      Exchange thirdExchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "Third test exchange", m_timeZone);

      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(secondExchange.Id, secondExchange.Country.Id, secondExchange.NameTextId, secondExchange.Name, secondExchange.TimeZone));
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(thirdExchange.Id, thirdExchange.Country.Id, thirdExchange.NameTextId, thirdExchange.Name, thirdExchange.TimeZone));

      IList<IDataStoreService.Exchange> exchanges = m_dataStore.GetExchanges();
      Assert.AreEqual(3, exchanges.Count, "Returned exchange holiday count is not correct.");
      Assert.IsNotNull(exchanges.Where(x => x.Id == m_exchange.Id && x.CountryId == m_exchange.Country.Id && x.TimeZone.Id == m_exchange.TimeZone.Id).Single(), "m_exchange not returned in stored data.");
      Assert.IsNotNull(exchanges.Where(x => x.Id == secondExchange.Id&& x.CountryId == secondExchange.Country.Id && x.TimeZone.Id == secondExchange.TimeZone.Id).Single(), "secondExchange not returned in stored data.");
      Assert.IsNotNull(exchanges.Where(x => x.Id == thirdExchange.Id && x.CountryId == thirdExchange.Country.Id && x.TimeZone.Id == thirdExchange.TimeZone.Id).Single(), "thirdExchange not returned in stored data.");
    }

    [TestMethod]
    public void GetSessions_ReturnPersistedData_Success()
    {
      TimeOnly preMarketStartTime = new TimeOnly(6, 0);
      TimeOnly preMarketEndTime = new TimeOnly(9, 29);
      TimeOnly mainStartTime = new TimeOnly(9, 30);
      TimeOnly mainEndTime = new TimeOnly(15, 59);
      TimeOnly postMarketStartTime = new TimeOnly(16, 0);
      TimeOnly postMarketEndTime = new TimeOnly(21, 00);

      Session preMarketSession = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "Pre-market Session", preMarketStartTime, preMarketEndTime);
      Session mainSession = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "Main Session", mainStartTime, mainEndTime);
      Session postMarketSession = new Session(m_dataStore, m_dataManager.Object, m_exchange, DayOfWeek.Monday, "Post-market Session", postMarketStartTime, postMarketEndTime);

      m_dataStore.CreateSession(new IDataStoreService.Session(preMarketSession.Id, preMarketSession.NameTextId, preMarketSession.Name, preMarketSession.Exchange.Id, preMarketSession.Day, preMarketSession.Start, preMarketSession.End));
      m_dataStore.CreateSession(new IDataStoreService.Session(mainSession.Id, mainSession.NameTextId, mainSession.Name, mainSession.Exchange.Id, mainSession.Day, mainSession.Start, mainSession.End));
      m_dataStore.CreateSession(new IDataStoreService.Session(postMarketSession.Id, postMarketSession.NameTextId, postMarketSession.Name, postMarketSession.Exchange.Id, postMarketSession.Day, postMarketSession.Start, postMarketSession.End));

      IList<IDataStoreService.Session> sessions = m_dataStore.GetSessions();
      Assert.AreEqual(3, sessions.Count, "Returned exchange holiday count is not correct.");
      Assert.IsNotNull(sessions.Where(x => x.Id == preMarketSession.Id && x.NameTextId == preMarketSession.NameTextId && x.ExchangeId == preMarketSession.Exchange.Id && x.DayOfWeek == preMarketSession.Day && x.Start == preMarketSession.Start && x.End == preMarketSession.End).Single(), "pre-market session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.Id == mainSession.Id && x.NameTextId == mainSession.NameTextId && x.ExchangeId == mainSession.Exchange.Id && x.DayOfWeek == mainSession.Day && x.Start == mainSession.Start && x.End == mainSession.End).Single(), "main session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.Id == postMarketSession.Id && x.NameTextId == postMarketSession.NameTextId && x.ExchangeId == postMarketSession.Exchange.Id && x.DayOfWeek == postMarketSession.Day && x.Start == postMarketSession.Start && x.End == postMarketSession.End).Single(), "post-market session not returned in stored data.");
    }

    [TestMethod]
    public void GetInstrumentGroupInstruments_ReturnPersistedData_Success()
    {
      InstrumentGroup instrumentGroup = new InstrumentGroup(m_dataStore, m_dataManager.Object, "TestInstrumentGroupName", "TestInstrumentGroupDescription");

      m_dataStore.CreateInstrumentGroup(new IDataStoreService.InstrumentGroup(instrumentGroup.Id, instrumentGroup.Parent.Id, instrumentGroup.NameTextId, instrumentGroup.Name, instrumentGroup.DescriptionTextId, instrumentGroup.Description, new List<Guid> { m_instrument.Id }));

      Assert.AreEqual(1, m_dataStore.GetRowCount(Data.SqliteDataStoreService.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup.Parent.Id.ToString()}' " +
        $"AND NameTextId = '{instrumentGroup.NameTextId.ToString()}' " +
        $"AND DescriptionTextId = '{instrumentGroup.DescriptionTextId.ToString()}'")
      , "Instrument group not persisted to database.");

      IList<Guid> instrumentIds = m_dataStore.GetInstrumentGroupInstruments(instrumentGroup.Id);
      Assert.AreEqual(1, instrumentIds.Count, "Number of returned instrument id's are incorrect.");
      Assert.IsNotNull(instrumentIds.Where(x => x == m_instrument.Id).Single(), "m_instrument not returned as child of instrument group.");
    }

    [TestMethod]
    public void GetInstruments_ReturnPersistedData_Success()
    {
      Instrument instrumentTest2 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Stock, "TEST2", "TestInstrument2", "TestInstrumentDescription2", DateTime.Now.ToUniversalTime().AddDays(1));
      Instrument instrumentTest3 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Stock, "TEST3", "TestInstrument3", "TestInstrumentDescription3", DateTime.Now.ToUniversalTime().AddDays(2));

      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(instrumentTest2.Id, instrumentTest2.Type, instrumentTest2.Ticker, instrumentTest2.NameTextId, instrumentTest2.Name, instrumentTest2.DescriptionTextId, instrumentTest2.Description, instrumentTest2.InceptionDate, new List<Guid>(), instrumentTest2.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(instrumentTest3.Id, instrumentTest3.Type, instrumentTest3.Ticker, instrumentTest3.NameTextId, instrumentTest3.Name, instrumentTest3.DescriptionTextId, instrumentTest3.Description, instrumentTest3.InceptionDate, new List<Guid>(), instrumentTest3.PrimaryExchange.Id, new List<Guid>()));

      IList<IDataStoreService.Instrument> instruments = m_dataStore.GetInstruments();

      Assert.AreEqual(3, instruments.Count, "Returned instrument count is not correct.");
      Assert.IsNotNull(instruments.Where(x => x.Id == m_instrument.Id && x.Type == m_instrument.Type && x.Ticker == m_instrument.Ticker && x.NameTextId == m_instrument.NameTextId && x.DescriptionTextId == m_instrument.DescriptionTextId && x.InceptionDate == m_instrument.InceptionDate).Single(), "m_instrument not found");
      Assert.IsNotNull(instruments.Where(x => x.Id == instrumentTest2.Id && x.Type == instrumentTest2.Type && x.Ticker == instrumentTest2.Ticker && x.NameTextId == instrumentTest2.NameTextId && x.DescriptionTextId == instrumentTest2.DescriptionTextId && x.InceptionDate == instrumentTest2.InceptionDate).Single(), "instrumentTest2 not found");
      Assert.IsNotNull(instruments.Where(x => x.Id == instrumentTest3.Id && x.Type == instrumentTest3.Type && x.Ticker == instrumentTest3.Ticker && x.NameTextId == instrumentTest3.NameTextId && x.DescriptionTextId == instrumentTest3.DescriptionTextId && x.InceptionDate == instrumentTest3.InceptionDate).Single(), "instrumentTest3 not found");
    }

    [TestMethod]
    public void GetInstruments_ReturnsSecondaryExchanges_Success()
    {
      Exchange secondExchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "Second test exchange", m_timeZone);
      Exchange thirdExchange = new Exchange(m_dataStore, m_dataManager.Object, m_country, "Third test exchange", m_timeZone);

      m_instrument.Add(secondExchange);
      m_instrument.Add(thirdExchange);

      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>(m_instrument.SecondaryExchanges.Select( x => x.Id ))));

      IList<IDataStoreService.Instrument> instruments = m_dataStore.GetInstruments();
      Assert.AreEqual(1, instruments.Count, "Returned instrument count is not correct.");
      Assert.AreEqual(2, instruments.ElementAt(0).SecondaryExchangeIds.Count, "Returned secondary exchanges count is not correct.");
      Assert.IsNotNull(instruments.Where(x => x.Id == m_instrument.Id && x.Type == m_instrument.Type && x.Ticker == m_instrument.Ticker).Single(), "m_instrument not found");
      Assert.IsNotNull(instruments.ElementAt(0).SecondaryExchangeIds.Where(x => x == secondExchange.Id).Single(), "secondExchange not returned as secondary exchange for instrument.");
      Assert.IsNotNull(instruments.ElementAt(0).SecondaryExchangeIds.Where(x => x == thirdExchange.Id).Single(), "thirdExchange not returned as secondary exhange for instrument.");
    }

    [TestMethod]
    public void GetInstruments_ByInstrumentType_Success()
    {
      Instrument stock2 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Stock, "STOCK2", "Stock2", "StockDescription2", DateTime.Now.ToUniversalTime().AddDays(1));
      Instrument stock3 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Stock, "STOCK3", "Stock3", "StockDescription3", DateTime.Now.ToUniversalTime().AddDays(2));

      Instrument forex1 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Forex, "FOREX1", "Forex1", "ForexDescription1", DateTime.Now.ToUniversalTime().AddDays(1));
      Instrument forex2 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Forex, "FOREX2", "Forex2", "ForexDescription2", DateTime.Now.ToUniversalTime().AddDays(2));
      Instrument forex3 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Forex, "FOREX3", "Forex3", "ForexDescription3", DateTime.Now.ToUniversalTime().AddDays(3));
      Instrument forex4 = new Instrument(m_dataStore, m_dataManager.Object, m_exchange, InstrumentType.Forex, "FOREX4", "Forex4", "ForexDescription4", DateTime.Now.ToUniversalTime().AddDays(4));

      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(stock2.Id, stock2.Type, stock2.Ticker, stock2.NameTextId, stock2.Name, stock2.DescriptionTextId, stock2.Description, stock2.InceptionDate, new List<Guid>(), stock2.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(stock3.Id, stock3.Type, stock3.Ticker, stock3.NameTextId, stock3.Name, stock3.DescriptionTextId, stock3.Description, stock3.InceptionDate, new List<Guid>(), stock3.PrimaryExchange.Id, new List<Guid>()));

      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(forex1.Id, forex1.Type, forex1.Ticker, forex1.NameTextId, forex1.Name, forex1.DescriptionTextId, forex1.Description, forex1.InceptionDate, new List<Guid>(), forex1.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(forex2.Id, forex2.Type, forex2.Ticker, forex2.NameTextId, forex2.Name, forex2.DescriptionTextId, forex2.Description, forex2.InceptionDate, new List<Guid>(), forex2.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(forex3.Id, forex3.Type, forex3.Ticker, forex3.NameTextId, forex3.Name, forex3.DescriptionTextId, forex3.Description, forex3.InceptionDate, new List<Guid>(), forex3.PrimaryExchange.Id, new List<Guid>()));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(forex4.Id, forex4.Type, forex4.Ticker, forex4.NameTextId, forex4.Name, forex4.DescriptionTextId, forex4.Description, forex4.InceptionDate, new List<Guid>(), forex4.PrimaryExchange.Id, new List<Guid>()));

      IList<IDataStoreService.Instrument> stocks = m_dataStore.GetInstruments(InstrumentType.Stock);

      Assert.AreEqual(3, stocks.Count, "Returned stock count is not correct.");
      Assert.IsNotNull(stocks.Where(x => x.Id == m_instrument.Id && x.Type == m_instrument.Type && x.Ticker == m_instrument.Ticker && x.NameTextId == m_instrument.NameTextId && x.DescriptionTextId == m_instrument.DescriptionTextId && x.InceptionDate == m_instrument.InceptionDate).Single(), "m_instrument not found");
      Assert.IsNotNull(stocks.Where(x => x.Id == stock2.Id && x.Type == stock2.Type && x.Ticker == stock2.Ticker && x.NameTextId == stock2.NameTextId && x.DescriptionTextId == stock2.DescriptionTextId && x.InceptionDate == stock2.InceptionDate).Single(), "stockTest2 not found");
      Assert.IsNotNull(stocks.Where(x => x.Id == stock3.Id && x.Type == stock3.Type && x.Ticker == stock3.Ticker && x.NameTextId == stock3.NameTextId && x.DescriptionTextId == stock3.DescriptionTextId && x.InceptionDate == stock3.InceptionDate).Single(), "stockTest3 not found");

      IList<IDataStoreService.Instrument> forex = m_dataStore.GetInstruments(InstrumentType.Forex);
      Assert.AreEqual(4, forex.Count, "Returned forex count is not correct.");
      Assert.IsNotNull(forex.Where(x => x.Id == forex1.Id && x.Type == forex1.Type && x.Ticker == forex1.Ticker && x.NameTextId == forex1.NameTextId && x.DescriptionTextId == forex1.DescriptionTextId && x.InceptionDate == forex1.InceptionDate).Single(), "forex1 not found");
      Assert.IsNotNull(forex.Where(x => x.Id == forex2.Id && x.Type == forex2.Type && x.Ticker == forex2.Ticker && x.NameTextId == forex2.NameTextId && x.DescriptionTextId == forex2.DescriptionTextId && x.InceptionDate == forex2.InceptionDate).Single(), "forex2 not found");
      Assert.IsNotNull(forex.Where(x => x.Id == forex3.Id && x.Type == forex3.Type && x.Ticker == forex3.Ticker && x.NameTextId == forex3.NameTextId && x.DescriptionTextId == forex3.DescriptionTextId && x.InceptionDate == forex3.InceptionDate).Single(), "forex3 not found");
      Assert.IsNotNull(forex.Where(x => x.Id == forex4.Id && x.Type == forex4.Type && x.Ticker == forex4.Ticker && x.NameTextId == forex4.NameTextId && x.DescriptionTextId == forex4.DescriptionTextId && x.InceptionDate == forex4.InceptionDate).Single(), "forex4 not found");
    }

    [TestMethod]
    public void GetFundmentals_ReturnPersistedData_Success()
    {
      Fundamental fundamental1 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental1", "TestCountryFundamentalDescription1", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      Fundamental fundamental2 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental2", "TestCountryFundamentalDescription2", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);
      Fundamental fundamental3 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental3", "TestCountryFundamentalDescription3", FundamentalCategory.Country, FundamentalReleaseInterval.Quarterly);
      Fundamental fundamental4 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental1", "TestInsrumentFundamentalDescription1", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      Fundamental fundamental5 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental2", "TestInsrumentFundamentalDescription2", FundamentalCategory.Instrument, FundamentalReleaseInterval.Monthly);
      Fundamental fundamental6 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental3", "TestInsrumentFundamentalDescription3", FundamentalCategory.Instrument, FundamentalReleaseInterval.Quarterly);

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental1.Id, fundamental1.NameTextId, fundamental1.Name, fundamental1.DescriptionTextId, fundamental1.Description, fundamental1.Category, fundamental1.ReleaseInterval));
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental2.Id, fundamental2.NameTextId, fundamental2.Name, fundamental2.DescriptionTextId, fundamental2.Description, fundamental2.Category, fundamental2.ReleaseInterval));
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental3.Id, fundamental3.NameTextId, fundamental3.Name, fundamental3.DescriptionTextId, fundamental3.Description, fundamental3.Category, fundamental3.ReleaseInterval));
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental4.Id, fundamental4.NameTextId, fundamental4.Name, fundamental4.DescriptionTextId, fundamental4.Description, fundamental4.Category, fundamental4.ReleaseInterval));
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental5.Id, fundamental5.NameTextId, fundamental5.Name, fundamental5.DescriptionTextId, fundamental5.Description, fundamental5.Category, fundamental5.ReleaseInterval));
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental6.Id, fundamental6.NameTextId, fundamental6.Name, fundamental6.DescriptionTextId, fundamental6.Description, fundamental6.Category, fundamental6.ReleaseInterval));

      IList<IDataStoreService.Fundamental> fundamentals = m_dataStore.GetFundamentals();

      Assert.AreEqual(6, fundamentals.Count, "Returned fundamentals count is not correct.");

      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental1.Id && x.NameTextId == fundamental1.NameTextId && x.DescriptionTextId == fundamental1.DescriptionTextId && x.Category == fundamental1.Category && x.Category == fundamental1.Category && x.ReleaseInterval == fundamental1.ReleaseInterval).Single(), "fundamental1 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental2.Id && x.NameTextId == fundamental2.NameTextId && x.DescriptionTextId == fundamental2.DescriptionTextId && x.Category == fundamental2.Category && x.Category == fundamental2.Category && x.ReleaseInterval == fundamental2.ReleaseInterval).Single(), "fundamental2 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental3.Id && x.NameTextId == fundamental3.NameTextId && x.DescriptionTextId == fundamental3.DescriptionTextId && x.Category == fundamental3.Category && x.Category == fundamental3.Category && x.ReleaseInterval == fundamental3.ReleaseInterval).Single(), "fundamental3 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental4.Id && x.NameTextId == fundamental4.NameTextId && x.DescriptionTextId == fundamental4.DescriptionTextId && x.Category == fundamental4.Category && x.Category == fundamental4.Category && x.ReleaseInterval == fundamental4.ReleaseInterval).Single(), "fundamental4 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental5.Id && x.NameTextId == fundamental5.NameTextId && x.DescriptionTextId == fundamental5.DescriptionTextId && x.Category == fundamental5.Category && x.Category == fundamental5.Category && x.ReleaseInterval == fundamental5.ReleaseInterval).Single(), "fundamental5 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental6.Id && x.NameTextId == fundamental6.NameTextId && x.DescriptionTextId == fundamental6.DescriptionTextId && x.Category == fundamental6.Category && x.Category == fundamental6.Category && x.ReleaseInterval == fundamental6.ReleaseInterval).Single(), "fundamental6 not found");
    }

    [TestMethod]
    public void GetCountryFundmentals_ReturnPersistedData_Success()
    {
      Fundamental fundamental1 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental1.Id, fundamental1.NameTextId, fundamental1.Name, fundamental1.DescriptionTextId, fundamental1.Description, fundamental1.Category, fundamental1.ReleaseInterval));

      CountryFundamental countryFundamental = new CountryFundamental(fundamental1, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;

      Fundamental fundamental2 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInsrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental2, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      DateTime instrumentDateTime = DateTime.Now.ToUniversalTime().AddDays(10);
      double instrumentValue = 10.0;

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental1.Id, fundamental1.NameTextId, fundamental1.Name, fundamental1.DescriptionTextId, fundamental1.Description, fundamental1.Category, fundamental1.ReleaseInterval));
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime.AddDays(1), value + 1.0);
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime.AddDays(2), value + 2.0);

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental2.Id, fundamental2.NameTextId, fundamental2.Name, fundamental2.DescriptionTextId, fundamental2.Description, fundamental2.Category, fundamental2.ReleaseInterval));
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, instrumentDateTime, instrumentValue);
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, instrumentDateTime.AddDays(1), instrumentValue + 1.0);
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, instrumentDateTime.AddDays(2), instrumentValue + 2.0);

      IList<IDataStoreService.CountryFundamental> countryFundamentalValues = m_dataStore.GetCountryFundamentals(m_dataProvider1.Object.Name);
      Assert.AreEqual(1, countryFundamentalValues.Count, "Returned country fundamental value count is not correct.");
      IDataStoreService.CountryFundamental countryFundamentalValue = countryFundamentalValues.ElementAt(0);
      Assert.IsNotNull(countryFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime && (double)x.Item2 == value), "Country fundamental value 1 not found");
      Assert.IsNotNull(countryFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(1) && (double)x.Item2 == value + 1.0), "Country fundamental value 2 not found");
      Assert.IsNotNull(countryFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(2) && (double)x.Item2 == value + 2.0), "Country fundamental value 3 not found");
    }

    [TestMethod]
    public void GetInstrumentFundmentals_ReturnPersistedData_Success()
    {
      Fundamental fundamental1 = new Fundamental(m_dataStore, m_dataManager.Object, "TestInstrumentFundamental", "TestInsrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(fundamental1, m_instrument);
      IDataStoreService.InstrumentFundamental sInstrumentFundamental = new IDataStoreService.InstrumentFundamental(m_dataManager.Object.DataProvider.Name, instrumentFundamental.Fundamental.Id, instrumentFundamental.Instrument.Id);
      m_dataStore.CreateInstrumentFundamental(ref sInstrumentFundamental);
      instrumentFundamental.AssociationId = sInstrumentFundamental.AssociationId;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;

      Fundamental fundamental2 = new Fundamental(m_dataStore, m_dataManager.Object, "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      CountryFundamental countryFundamental = new CountryFundamental(fundamental2, m_country);
      IDataStoreService.CountryFundamental sCountryFundamental = new IDataStoreService.CountryFundamental(m_dataManager.Object.DataProvider.Name, countryFundamental.Fundamental.Id, countryFundamental.Country.Id);
      m_dataStore.CreateCountryFundamental(ref sCountryFundamental);
      countryFundamental.AssociationId = sCountryFundamental.AssociationId;

      DateTime countryDateTime = DateTime.Now.ToUniversalTime().AddDays(10);
      double countryValue = 10.0;

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental1.Id, fundamental1.NameTextId, fundamental1.Name, fundamental1.DescriptionTextId, fundamental1.Description, fundamental1.Category, fundamental1.ReleaseInterval));

      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime, value);
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime.AddDays(1), value + 1.0);
      m_dataStore.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime.AddDays(2), value + 2.0);

      m_dataStore.CreateFundamental(new IDataStoreService.Fundamental(fundamental2.Id, fundamental2.NameTextId, fundamental2.Name, fundamental2.DescriptionTextId, fundamental2.Description, fundamental2.Category, fundamental2.ReleaseInterval));
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, countryDateTime, countryValue);
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, countryDateTime.AddDays(1), countryValue + 1.0);
      m_dataStore.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, countryDateTime.AddDays(2), countryValue + 2.0);

      IList<IDataStoreService.InstrumentFundamental> instrumentFundamentalValues = m_dataStore.GetInstrumentFundamentals(m_dataProvider1.Object.Name);
      Assert.AreEqual(1, instrumentFundamentalValues.Count, "Returned country fundamental value count is not correct.");
      IDataStoreService.InstrumentFundamental instrumentFundamentalValue = instrumentFundamentalValues.ElementAt(0);
      Assert.IsNotNull(instrumentFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime && (double)x.Item2 == value), "Instrument fundamental value 1 not found");
      Assert.IsNotNull(instrumentFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(1) && (double)x.Item2 == value + 1.0), "Instrument fundamental value 2 not found");
      Assert.IsNotNull(instrumentFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(2) && (double)x.Item2 == value + 2.0), "Instrument fundamental value 3 not found");

      //NOTE: We don't check the country fundamentals, just making sure ONLY instrument fundamentals are returned.

    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    public void GetBarData_ReturnsActualVsSyntheticBarData_Success(Resolution resolution, PriceDataType priceDataType)
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      BarData barData = new IDataStoreService.BarData(10);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5),
                                              dateTime.AddMinutes(6), dateTime.AddMinutes(7), dateTime.AddMinutes(8), dateTime.AddMinutes(9), dateTime.AddMinutes(10) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0, 211.0, 221.0, 231.0, 241.0, 251.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0, 212.0, 222.0, 232.0, 242.0, 252.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0, 213.0, 223.0, 233.0, 243.0, 253.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0, 214.0, 224.0, 234.0, 244.0, 254.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155, 215, 225, 235, 245, 255 };
      barData.Synthetic = new List<bool> { true, false, true, false, true, false, true, false, true, false };

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      DataCache dataResult = m_dataStore.GetBarData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, dateTime, dateTime.AddMinutes(10), resolution, priceDataType);
      Assert.AreEqual(barData.Count / 2, dataResult.Count, "GetBarData did not return the correct number of bars.");

      BarData dataResultDetails = (IDataStoreService.BarData)dataResult.Data;
      for (int index = 0; index < barData.Count; index++) 
      {
        if ((priceDataType == PriceDataType.Synthetic && !barData.Synthetic[index]) || (priceDataType == PriceDataType.Actual && barData.Synthetic[index])) continue;
        Assert.IsTrue(dataResultDetails.DateTime.Contains(barData.DateTime[index]), string.Format("Bar data {0} not found.", barData.DateTime[index]));
      }
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    public void GetBarData_ReturnsActualAndSyntheticBarData_Success(Resolution resolution)
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();  //bar data must always be stored in UTC datetime
      BarData barData = new IDataStoreService.BarData(10);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5),
                                              dateTime.AddMinutes(6), dateTime.AddMinutes(7), dateTime.AddMinutes(8), dateTime.AddMinutes(9), dateTime.AddMinutes(10) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0, 211.0, 221.0, 231.0, 241.0, 251.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0, 212.0, 222.0, 232.0, 242.0, 252.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0, 213.0, 223.0, 233.0, 243.0, 253.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0, 214.0, 224.0, 234.0, 244.0, 254.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155, 215, 225, 235, 245, 255 };
      barData.Synthetic = new List<bool> { true, false, true, false, true, false, true, false, true, false };

      m_dataStore.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      DataCache dataResult = m_dataStore.GetBarData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, dateTime, dateTime.AddMinutes(10), resolution, PriceDataType.Both);
      Assert.AreEqual(barData.Count, dataResult.Count, "GetBarData did not return the correct number of bars.");

      BarData dataResultDetails = (IDataStoreService.BarData)dataResult.Data;
      for (int index = 0; index < barData.Count; index++)
        Assert.IsTrue(dataResultDetails.DateTime.Contains(barData.DateTime[index]), string.Format("Bar data {0} not found.", barData.DateTime[index]));
    }
  }
}
