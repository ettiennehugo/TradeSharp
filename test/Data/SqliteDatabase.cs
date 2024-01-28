using System.Globalization;
using System.Text.RegularExpressions;
using TradeSharp.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Data.Testing
{
  [TestClass]
  public class SqliteDatabase
  {
    //constants


    //enums


    //types


    //attributes
    private Mock<IConfigurationService> m_configuration;
    private ILoggerFactory m_loggerFactory;
    private Dictionary<string, object> m_generalConfiguration;
    private Mock<IDataProvider> m_dataProvider1;
    private Mock<IDataProvider> m_dataProvider2;
    private IList<IDataProvider> m_dataProviders;
    private CultureInfo m_cultureEnglish;
    private RegionInfo m_regionInfo;
    private Data.SqliteDatabase m_database;
    private Country m_country;
    private TimeZoneInfo m_timeZone;
    private Exchange m_exchange;
    private Instrument m_instrument;

    //constructors
    public SqliteDatabase()
    {
      //create instance components
      m_cultureEnglish = CultureInfo.GetCultureInfo("en-US");
      m_regionInfo = new RegionInfo(m_cultureEnglish.Name);

      m_configuration = new Mock<IConfigurationService>(MockBehavior.Strict);
      m_configuration.Setup(x => x.CultureInfo).Returns(m_cultureEnglish);
      m_configuration.Setup(x => x.RegionInfo).Returns(m_regionInfo);
      Type testDataProviderType = typeof(TradeSharp.Data.Testing.TestDataProvider);
      m_configuration.Setup(x => x.DataProviders).Returns(new Dictionary<string, IPluginConfiguration>() { 
        { "TestDataProvider1", new PluginConfiguration("TestDataProvider1", "", new List<IPluginConfigurationProfile>()) }, 
        { "TestDataProvider2", new PluginConfiguration("TestDataProvider2", "", new List<IPluginConfigurationProfile>()) } 
      });

      m_generalConfiguration = new Dictionary<string, object>() {
          { IConfigurationService.GeneralConfiguration.TimeZone, (object)IConfigurationService.TimeZone.Local },
          { IConfigurationService.GeneralConfiguration.Database, new DataStoreConfiguration(typeof(TradeSharp.Data.SqliteDatabase).ToString(), "TradeSharpTest.db") }
      };

      m_configuration.Setup(x => x.General).Returns(m_generalConfiguration);

      m_dataProvider1 = new Mock<IDataProvider>().SetupAllProperties();
      m_dataProvider1.SetupGet(x => x.Name).Returns("TestDataProvider1");
      m_dataProvider2 = new Mock<IDataProvider>().SetupAllProperties();
      m_dataProvider2.SetupGet(x => x.Name).Returns("TestDataProvider2");
      m_dataProviders = new List<IDataProvider>();
      m_dataProviders.Add(m_dataProvider1.Object);
      m_dataProviders.Add(m_dataProvider2.Object);

      m_loggerFactory = new LoggerFactory();
      m_database = new TradeSharp.Data.SqliteDatabase(m_configuration.Object, new Logger<TradeSharp.Data.SqliteDatabase>(m_loggerFactory));

      //remove stale data from previous tests - this is to ensure proper test isolation and create the default objects used by the database
      m_database.ClearDatabase();
      m_database.CreateDefaultObjects();

      //create common attributes used for testing
      m_country = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, "TagValue", "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "TestExchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_instrument = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "TEST", "TestInstrument", "TestInstrumentDescription", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>()); //database layer stores dates in UTC
    }

    //finalizers
    ~SqliteDatabase()
    {
      m_database.EndTransaction(true);  //end any open transactions
      m_database.DropSchema(); //erase test database
      m_database.Dispose();
    }

    //interface implementations


    //properties


    //methods
    [TestMethod]
    public void CreateDefaultObjects_InternationalCountry_Success()
    {
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableCountry,
        $"Id = '{Country.InternationalId.ToString()}'")
      , "International country default object not created.");
    }

    [TestMethod]
    public void CreateDefaultObjects_InternationalExchange_Success()
    {
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{Exchange.InternationalId.ToString()}'")
      , "International exchange default object not created.");

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Monday}")
      , "International exchange default Monday session object not created.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Tuesday}")
      , "International exchange default Tuesday session object not created.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Wednesday}")
      , "International exchange default Wednesday session object not created.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Thursday}")
      , "International exchange default Thursday session object not created.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Friday}")
      , "International exchange default Friday session object not created.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Saturday}")
      , "International exchange default Saturday session object not created.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"ExchangeId = '{Exchange.InternationalId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Sunday}")
      , "International exchange default Sunday session object not created.");
    }

    [TestMethod]
    public void CreateCountryHoliday_DayOfMonth_Success()
    {
      Holiday holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);

      m_database.CreateHoliday(holiday);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND Tag = '{holiday.Tag}' " +
        $"AND ParentId = '{holiday.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)holiday.Type} " +
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
      Holiday holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfWeek", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);

      m_database.CreateHoliday(holiday);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND Tag = '{holiday.Tag}' " +
        $"AND ParentId = '{holiday.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)holiday.Type} " +
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
      m_database.CreateExchange(m_exchange);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND CountryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");
    }

    [TestMethod]
    public void CreateHoliday_ExchangeDayOfMonth_Success()
    {
      Holiday holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);

      m_database.CreateHoliday(holiday);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND Tag = '{holiday.Tag}' " +
        $"AND ParentId = '{holiday.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)holiday.Type} " +
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
      Holiday holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfWeek", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);

      m_database.CreateHoliday(holiday);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{holiday.Id.ToString()}' " +
        $"AND Tag = '{holiday.Tag}' " +
        $"AND ParentId = '{holiday.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)holiday.Type} " +
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
      Session session = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "TestSession", m_exchange.Id, DayOfWeek.Monday, startTime, endTime);

      m_database.CreateSession(session);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND Tag = '{session.Tag}' " +
        $"AND ExchangeId = '{session.ExchangeId.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = {session.Start.Ticks} " +
        $"AND EndTime = {session.End.Ticks} ")
      , "Session not persisted to database.");
    }

    [TestMethod]
    public void CreateCountryFundamental_PersistAssociation_Success()
    {
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);
      countryFundamental.AssociationId = countryFundamental.AssociationId;

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalAssociations),
        $"Id = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{countryFundamental.FundamentalId.ToString()}' " +
        $"AND CountryId = '{countryFundamental.CountryId.ToString()}'")
      , "Country fundamental association not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrumentFundamental_PersistAssociation_Success()
    {
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);
      instrumentFundamental.AssociationId = instrumentFundamental.AssociationId;

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalAssociations),
        $"Id = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{instrumentFundamental.FundamentalId.ToString()}' " +
        $"AND InstrumentId = '{instrumentFundamental.InstrumentId.ToString()}'")
      , "Instrument fundamental association not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrumentGroup_PersistData_Success()
    {
      InstrumentGroup instrumentGroup = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName", "TestInstrumentGroupDescription", new List<Guid> { m_instrument.Id });

      m_database.CreateInstrumentGroup(instrumentGroup);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup.Id.ToString()}' " +
        $"AND Tag = '{instrumentGroup.Tag}' " +
        $"AND ParentId = '{instrumentGroup.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup.Name}' " +
        $"AND Description = '{instrumentGroup.Description}'")
      , "Instrument group not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group and instrument association not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrument_PersistData_Success()
    {
      m_database.CreateInstrument(m_instrument);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Tag = '{m_instrument.Tag}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not persisted to database.");
    }

    [TestMethod]
    public void CreateInstrument_AdditionalExchangePersistData_Success()
    {
      Exchange exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "SecondaryTestExchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_database.AddInstrumentToExchange(m_instrument.Id, exchange.Id);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentSecondaryExchange,
        $"InstrumentId = '{m_instrument.Id.ToString()}' " +
        $"AND ExchangeId = '{exchange.Id.ToString()}' ")
      , "Secondary exchange not persisted.");
    }

    [TestMethod]
    public void CreateFundamental_PersistData_Success()
    {
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      m_database.CreateFundamental(fundamental);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableFundamentals,
        $"Id = '{fundamental.Id.ToString()}' " +
        $"AND Tag = '{fundamental.Tag}' " +
        $"AND Category = {(int)fundamental.Category} " +
        $"AND ReleaseInterval = {(int)fundamental.ReleaseInterval}")
      , "Fundamental not persisted to database.");
    }

    [TestMethod]
    public void UpdateHoliday_PersistChanges_Success()
    {
      Holiday holiday = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "TestHoliday", HolidayType.DayOfMonth, Months.January, 1, 0, 0, MoveWeekendHoliday.DontAdjust);

      m_database.CreateHoliday(holiday);

      holiday.ParentId = Guid.NewGuid();
      holiday.Tag = "NewTagValue";
      holiday.Month = Months.February;
      holiday.DayOfMonth = 2;
      holiday.DayOfWeek = DayOfWeek.Monday;
      holiday.WeekOfMonth = WeekOfMonth.Second;
      holiday.MoveWeekendHoliday = MoveWeekendHoliday.PreviousBusinessDay;

      m_database.UpdateHoliday(holiday);

      Holiday? holidayFromDB = m_database.GetHoliday(holiday.Id);

      Assert.IsNotNull(holidayFromDB, "Holiday returned as null");
      Assert.AreEqual(holiday.ParentId, holidayFromDB.ParentId, "Holiday parent not updated in database.");
      Assert.AreEqual(holiday.Tag, holidayFromDB.Tag, "Holiday tag not updated in database.");
      Assert.AreEqual(holiday.Month, holidayFromDB.Month, "Holiday month not updated in database.");
      Assert.AreEqual(holiday.DayOfMonth, holidayFromDB.DayOfMonth, "Holiday day of month not updated in database.");
      Assert.AreEqual(holiday.DayOfWeek, holidayFromDB.DayOfWeek, "Holiday day of week not updated in database.");
      Assert.AreEqual(holiday.WeekOfMonth, holidayFromDB.WeekOfMonth, "Holiday week of month not updated in database.");
      Assert.AreEqual(holiday.MoveWeekendHoliday, holidayFromDB.MoveWeekendHoliday, "Holiday move weekend holiday not updated in database.");
    }

    [TestMethod]
    public void UpdateExchange_PersistChanges_Success()
    {
      m_database.CreateExchange(m_exchange);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND CountryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");

      Country germany = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, "TagValue", "de-DE");
      TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

      m_exchange.Name = "New Exchange";
      m_exchange.Tag = "NewTagValue";
      m_exchange.CountryId = germany.Id;
      m_exchange.TimeZone = timeZone;

      m_database.UpdateExchange(m_exchange);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND Tag = '{m_exchange.Tag}' " +
        $"AND Name = '{m_exchange.Name}' " +
        $"AND CountryId = '{germany.Id.ToString()}' " +
        $"AND TimeZone = '{timeZone.ToSerializedString()}'")
      , "Exchange not updated in database.");
    }

    [TestMethod]
    public void UpdateSession_ChangeDayAndTime_Success()
    {
      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "TestSession", m_exchange.Id, DayOfWeek.Monday, startTime, endTime);

      m_database.CreateSession(session);

      session.Name = "New Name";
      session.Tag = "NewTagValue";
      session.DayOfWeek = DayOfWeek.Tuesday;
      session.Start = startTime.AddMinutes(5);
      session.End = endTime.AddMinutes(5);

      m_database.UpdateSession(session);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND Tag = '{m_database.SqlSafeString(session.Tag)}' " +
        $"AND Name = '{m_database.SqlSafeString(session.Name)}' " +
        $"AND ExchangeId = '{session.ExchangeId.ToString()}' " +
        $"AND DayOfWeek = {(int)DayOfWeek.Tuesday} " +
        $"AND StartTime = {startTime.AddMinutes(5).Ticks} " +
        $"AND EndTime = {endTime.AddMinutes(5).Ticks} ")
      , "Session not updated in database.");
    }

    [TestMethod]
    public void UpdateInstrument_ChangeAllAttributes_Success()
    {
      Exchange exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "SecondaryTestExchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      DateTime dateTime = DateTime.Now.AddDays(3);

      m_database.CreateInstrument(m_instrument);

      Exchange secondExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Second test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      Exchange thirdExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Third test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);

      m_instrument.Ticker = "NEW";
      m_instrument.Tag = "NewTagValue";
      m_instrument.PrimaryExchangeId = exchange.Id;
      m_instrument.InceptionDate = dateTime;
      m_instrument.SecondaryExchangeIds = new List<Guid> { secondExchange.Id, thirdExchange.Id };

      m_database.UpdateInstrument(m_instrument);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Tag = '{m_instrument.Tag}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = 'NEW' " +
        $"AND PrimaryExchangeId = '{exchange.Id.ToString()}' " +
        $"AND InceptionDate = {dateTime.ToUniversalTime().ToBinary()}")
      , "Instrument not updated in database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentSecondaryExchange,
        $"InstrumentId = '{m_instrument.Id.ToString()}' " +
        $"AND ExchangeId = '{secondExchange.Id.ToString()}'")
      , "Instrument not associated with exchange 2.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentSecondaryExchange,
        $"InstrumentId = '{m_instrument.Id.ToString()}' " +
        $"AND ExchangeId = '{thirdExchange.Id.ToString()}'")
      , "Instrument not associated with exchange 3.");
    }

    [TestMethod]
    public void UpdateCountryFundamental_PersistData_Success()
    {
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");
    }

    [TestMethod]
    public void UpdateInstrumentFundamental_PersistData_Success()
    {
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, fundamental.Id, m_instrument.Id, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    [DataRow(Resolution.Week)]
    [DataRow(Resolution.Month)]
    public void UpdateData_SingleBarDataPersist_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double open = 1.0;
      double high = 2.0;
      double low = 3.0;
      double close = 4.0;
      long volume = 5;

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, open, high, low, close, volume);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, resolution),
        $"Ticker = '{m_instrument.Ticker}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Open = {open.ToString()} " +
        $"AND High = {high.ToString()} " +
        $"AND Low = {low.ToString()} " +
        $"AND Close = {close.ToString()} " +
        $"AND Volume = {volume.ToString()} ")
      , "Bar value not persisted to database.");
    }

    [TestMethod]
    public void UpdateData_SingleLevel1DataPersist_Success()
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double bid = 1.0;
      long bidSize = 2;
      double ask = 3.0;
      long askSize = 4;
      double last = 5.0;
      long lastSize = 6;

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, dateTime, bid, bidSize, ask, askSize, last, lastSize);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, Resolution.Level1),
        $"Ticker = '{m_instrument.Ticker}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Bid = {bid.ToString()} " +
        $"AND BidSize = {bidSize.ToString()} " +
        $"AND Ask = {ask.ToString()} " +
        $"AND AskSize = {askSize.ToString()} " +
        $"AND Last = {last.ToString()} " +
        $"AND LastSize = {lastSize.ToString()} ")
      , "Bar value not persisted to database.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    [DataRow(Resolution.Week)]
    [DataRow(Resolution.Month)]
    public void UpdateData_SingleBarDataUpdate_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double open = 1.0;
      double high = 2.0;
      double low = 3.0;
      double close = 4.0;
      long volume = 5;

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, open, high, low, close, volume);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, resolution),
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
      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, open, high, low, close, volume);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, resolution),
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
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    [DataRow(Resolution.Week)]
    [DataRow(Resolution.Month)]
    public void UpdateData_RangeBarDataPersist_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      DataCacheBars barData = new DataCacheBars(5);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155 };

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      for (int index = 0; index < barData.Count; index++)
      {
        Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, resolution),
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
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    [DataRow(Resolution.Week)]
    [DataRow(Resolution.Month)]
    public void UpdateData_RangeBarDataUpdate_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      DataCacheBars barData = new DataCacheBars(5);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155 };

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      for (int index = 0; index < barData.Count; index++)
      {
        Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, resolution),
          $"Ticker = '{m_instrument.Ticker}' " +
          $"AND DateTime = {barData.DateTime[index].ToBinary()} " +
          $"AND Open = {barData.Open[index].ToString()} " +
          $"AND High = {barData.High[index].ToString()} " +
          $"AND Low = {barData.Low[index].ToString()} " +
          $"AND Close = {barData.Close[index].ToString()} " +
          $"AND Volume = {barData.Volume[index].ToString()} ")
        , "Original bar value from list not persisted to database.");
      }

      barData = new DataCacheBars(5);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5) };
      barData.Open = new List<double> { 211.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 222.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 233.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 244.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 555 };

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      for (int index = 0; index < barData.Count; index++)
      {
        Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, resolution),
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
      m_database.CreateCountry(m_country);
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.IsoCode}'"), "Country data not persisted to database.");
      Assert.AreEqual(1, m_database.DeleteCountry(m_country.Id), "DeleteCountry did not return the correct number of rows removed");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.IsoCode}'"), "Country data not persisted to database.");
    }

    [TestMethod]
    public void DeleteCountry_CountryAndRelatedDataRemoved_Success()
    {
      //create country and a related objects
      m_database.CreateCountry(m_country);
      Holiday countryHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(countryHolidayDayOfMonth);

      m_database.CreateExchange(m_exchange);
      Holiday exchangeHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(exchangeHolidayDayOfMonth);

      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "TestSession", m_exchange.Id, DayOfWeek.Monday, startTime, endTime);
      m_database.CreateSession(session);
      m_database.CreateInstrument(m_instrument);

      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);

      //confirm that data was correctly persisted
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.IsoCode}'"), "Country data not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{countryHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{countryHolidayDayOfMonth.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)countryHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)countryHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {countryHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)countryHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)countryHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Country holiday for day of month not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND CountryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{exchangeHolidayDayOfMonth.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)exchangeHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)exchangeHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {exchangeHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)exchangeHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)exchangeHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Exchange holiday for day of month not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}'")
      , "Instrument not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalAssociations),
        $"Id = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{countryFundamental.FundamentalId.ToString()}' " +
        $"AND CountryId = '{countryFundamental.CountryId.ToString()}'")
      , "Country fundamental association not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");

      //delete country
      Assert.AreEqual(8, m_database.DeleteCountry(m_country.Id), "DeleteCountry did not return the correct number of rows removed");

      //check that country and all it's related data was removed
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableCountry, $"Id = '{m_country.Id}' AND IsoCode = '{m_country.IsoCode}'"), "Country data not persisted to database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{countryHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{countryHolidayDayOfMonth.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)countryHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)countryHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {countryHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)countryHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)countryHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Country holiday for day of month not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{exchangeHolidayDayOfMonth.ParentId.ToString()}' " +
        $"AND HolidayType = {(int)exchangeHolidayDayOfMonth.Type} " +
        $"AND Month = {(int)exchangeHolidayDayOfMonth.Month} " +
        $"AND DayOfMonth = {exchangeHolidayDayOfMonth.DayOfMonth} " +
        $"AND DayOfWeek = {(int)exchangeHolidayDayOfMonth.DayOfWeek} " +
        $"AND MoveWeekendHoliday = {(int)exchangeHolidayDayOfMonth.MoveWeekendHoliday}")
      , "Exchange holiday for day of month not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}'")
      , "Instrument not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalAssociations),
        $"Id = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND FundamentalId = '{countryFundamental.FundamentalId.ToString()}' " +
        $"AND CountryId = '{countryFundamental.CountryId.ToString()}'")
      , "Country fundamental association not persisted to database.");
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");
    }

    [TestMethod]
    public void DeleteExchange_DataRemoved_Success()
    {
      m_database.CreateExchange(m_exchange);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");

      Assert.AreEqual(1, m_database.DeleteExchange(m_exchange.Id), "DeleteExchange did not return the correct number of rows removed");

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not removed from database.");
    }

    [TestMethod]
    public void DeleteExchange_ExchangeAndRelatedDataRemoved_Success()
    {
      //create exchange and related objects
      m_database.CreateExchange(m_exchange);
      Holiday exchangeHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(exchangeHolidayDayOfMonth);

      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "TestSession", m_exchange.Id, DayOfWeek.Monday, startTime, endTime);
      m_database.CreateSession(session);

      m_database.CreateInstrument(m_instrument);

      //check that data was properly persisted
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{m_exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)HolidayType.DayOfMonth} " +
        $"AND Month = {(int)Months.January} " +
        $"AND DayOfMonth = {1} " +
        $"AND MoveWeekendHoliday = {(int)MoveWeekendHoliday.DontAdjust}")
      , "Exchange holiday for day of month not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}'")
      , "Instrument not persisted to database.");

      //delete exchange
      Assert.AreEqual(4, m_database.DeleteExchange(m_exchange.Id), "DeleteExchange did not return the correct number of rows removed");

      //check that exchange data and all related objects were removed
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableExchange,
        $"Id = '{m_exchange.Id.ToString()}' " +
        $"AND countryId = '{m_country.Id.ToString()}' " +
        $"AND TimeZone = '{m_timeZone.ToSerializedString()}'")
      , "Exchange not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableHoliday,
        $"Id = '{exchangeHolidayDayOfMonth.Id.ToString()}' " +
        $"AND ParentId = '{m_exchange.Id.ToString()}' " +
        $"AND HolidayType = {(int)HolidayType.DayOfMonth} " +
        $"AND Month = {(int)Months.January} " +
        $"AND DayOfMonth = {1} " +
        $"AND MoveWeekendHoliday = {(int)MoveWeekendHoliday.DontAdjust}")
      , "Exchange holiday for day of month not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = '{session.Start.Ticks}' " +
        $"AND EndTime = '{session.End.Ticks}' ")
      , "Session not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}'")
      , "Instrument not deleted from database.");
    }

    [TestMethod]
    public void DeleteSession_DataRemoved_Success()
    {
      TimeOnly startTime = new TimeOnly(9, 30);
      TimeOnly endTime = new TimeOnly(16, 0);
      Session session = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "TestSession", m_exchange.Id, DayOfWeek.Monday, startTime, endTime);

      m_database.CreateSession(session);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = {session.Start.Ticks} " +
        $"AND EndTime = {session.End.Ticks} ")
      , "Session not persisted to database.");

      Assert.AreEqual(1, m_database.DeleteSession(session.Id), "DeleteSession did not return the correct number of rows removed");

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableSession,
        $"Id = '{session.Id.ToString()}' " +
        $"AND ExchangeId = '{m_exchange.Id.ToString()}' " +
        $"AND DayOfWeek = {(int)session.DayOfWeek} " +
        $"AND StartTime = {session.Start.Ticks} " +
        $"AND EndTime = {session.End.Ticks} ")
      , "Session not removed from database.");
    }

    [TestMethod]
    public void DeleteFundamental_DataRemoved_Success()
    {
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestFundamental", "TestFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);

      m_database.CreateFundamental(fundamental);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableFundamentals,
        $"Id = '{fundamental.Id.ToString()}' " +
        $"AND Category = {(int)fundamental.Category} " +
        $"AND ReleaseInterval = {(int)fundamental.ReleaseInterval}")
      , "Fundamental not persisted to database.");

      Assert.AreEqual(1, m_database.DeleteFundamental(fundamental.Id), "DeleteFundamental did not return the correct number of rows removed");

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableFundamentals,
        $"Id = '{fundamental.Id.ToString()}' " +
        $"AND Category = {(int)fundamental.Category} " +
        $"AND ReleaseInterval = {(int)fundamental.ReleaseInterval}")
      , "Fundamental not removed from database.");
    }

    [TestMethod]
    public void DeleteFundamental_CountryFundamentalAndRelatedDataRemoved_Success()
    {
      //create countryFundamental and associated value1
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(2, m_database.DeleteFundamental(fundamental.Id), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed as well
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value not persisted to database.");
    }

    [TestMethod]
    public void DeleteFundamental_CountryForDataProvider_Success()
    {
      Fundamental fundamental1 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental1", "TestCountryFundamentalDescription1", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental1);
      Fundamental fundamental2 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental2", "TestCountryFundamentalDescription2", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental2);

      CountryFundamental countryFundamental1 = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental1.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental1);

      CountryFundamental countryFundamental2 = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental2.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental2);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental1.FundamentalId, countryFundamental1.CountryId, dateTime, value);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental2.FundamentalId, countryFundamental2.CountryId, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value2 not persisted to database.");

      m_database.DeleteCountryFundamental(m_dataProvider1.Object.Name, fundamental1.Id, m_country.Id);

      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value1 not removed from database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Country fundamental value2 incorrectly removed from database.");
    }

    [TestMethod]
    public void DeleteFundamentalValue_CountryForDataProvider_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;
      DateTime dateTime2 = DateTime.Now.AddDays(1).ToUniversalTime();
      double value2 = 2.0;

      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime1, value1);
      m_database.UpdateCountryFundamental(m_dataProvider2.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime2, value2);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value2 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_database.DeleteFundamentalValues(m_dataProvider1.Object.Name, fundamental.Id), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed for data provider 1 but that data provider 2's data is still present
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value not removed from database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteInstrumentFundamental_ForDataProvider_Success()
    {
      Fundamental fundamental1 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental1", "TestInstrumentFundamentalDescription1", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental1);
      Fundamental fundamental2 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental2", "TestInstrumentFundamentalDescription2", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental2);

      InstrumentFundamental instrumentFundamental1 = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental1.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental1);

      InstrumentFundamental instrumentFundamental2 = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental2.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental2);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental1.FundamentalId, instrumentFundamental1.InstrumentId, dateTime, value);
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental2.FundamentalId, instrumentFundamental2.InstrumentId, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value2 not persisted to database.");

      m_database.DeleteInstrumentFundamental(m_dataProvider1.Object.Name, fundamental1.Id, m_instrument.Id);

      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental1.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value1 not removed from database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental2.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value2 incorrectly removed from database.");
    }

    [TestMethod]
    public void DeleteFundamentalValues_InstrumentFundamentalAndRelatedDataRemoved_Success()
    {
      //create instrumentFundamental and associated values
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;
      DateTime dateTime2 = DateTime.Now.AddDays(1).ToUniversalTime();
      double value2 = 2.0;

      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime1, value1);
      m_database.UpdateInstrumentFundamental(m_dataProvider2.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime2, value2);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value1 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value2 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_database.DeleteFundamentalValues(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed for data provider 1 but not data provider 2
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value not removed from database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider2.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteFundamentalValues_InstrumentForDataProvider_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;

      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_database.DeleteFundamentalValues(instrumentFundamental.FundamentalId), "DeleteFundamental did not return the correct number of rows removed");

      //check that countryFundamental data values are removed as well
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not removed from database.");
    }

    [TestMethod]
    public void DeleteCountryFundamentalValue_DataRemoved_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;

      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime1, value1);

      DateTime dateTime2 = DateTime.Now.ToUniversalTime();
      double value2 = 2.0;

      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime2, value2);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value 1 not persisted to database.");

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value 2 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_database.DeleteCountryFundamentalValue(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime1), "DeleteCountry did not return the correct number of rows removed");

      //check that countryFundamental data value1 are removed
      //first value1 must be removed
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Country fundamental value not removed from database.");

      //second value1 must be present
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableCountryFundamentalValues),
        $"AssociationId = '{countryFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Country fundamental value 2 present in database after delete operation.");
    }

    [TestMethod]
    public void DeleteInstrumentFundamentalValue_DataRemoved_Success()
    {
      //create countryFundamental and associated values
      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      DateTime dateTime1 = DateTime.Now.ToUniversalTime();
      double value1 = 1.0;

      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime1, value1);

      DateTime dateTime2 = DateTime.Now.ToUniversalTime();
      double value2 = 2.0;

      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime2, value2);

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value 1 not persisted to database.");

      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value 1 not persisted to database.");

      //delete the countryFundamental
      Assert.AreEqual(1, m_database.DeleteInstrumentFundamentalValue(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime1), "DeleteInstrument did not return the correct number of rows removed");

      //check that countryFundamental data value1 is removed
      //first value1 must be removed
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime1.ToBinary()} " +
        $"AND Value = {value1.ToString()}")
      , "Instrument fundamental value 1 not removed from database.");

      //second value1 must be present
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime2.ToBinary()} " +
        $"AND Value = {value2.ToString()}")
      , "Instrument fundamental value 2 present in database after delete operation.");
    }

    [TestMethod]
    public void UpdateInstrumentGroup_ChangeParent_Success()
    {
      InstrumentGroup instrumentGroup1 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1", Array.Empty<Guid>());
      InstrumentGroup instrumentGroup2 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName2", "TestInstrumentGroupDescription2", Array.Empty<Guid>());

      m_database.CreateInstrumentGroup(instrumentGroup1);
      m_database.CreateInstrumentGroup(instrumentGroup2);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup1.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup1.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup1.Name}' " +
        $"AND Description = '{instrumentGroup1.Description}'")
      , "Instrument group 1 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup2.Name}' " +
        $"AND Description = '{instrumentGroup2.Description}'")
      , "Instrument group 2 not persisted to database.");

      instrumentGroup1.ParentId = instrumentGroup2.Id;
      instrumentGroup1.Name = "New Group1 Name";
      instrumentGroup1.Description = "New Group1 Description";
      m_database.UpdateInstrumentGroup(instrumentGroup1);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup1.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.Id.ToString()}' " +
        $"AND Name = '{instrumentGroup1.Name}' " +
        $"AND Description = '{instrumentGroup1.Description}'")
      , "Instrument group 1 not persisted to database.");
    }

    [TestMethod]
    public void UpdateInstrumentGroup_ChangeInstruments_Success()
    {
      Instrument stock2 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "STOCK2", "Stock2", "StockDescription2", DateTime.Now.ToUniversalTime().AddDays(1), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument stock3 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "STOCK3", "Stock3", "StockDescription3", DateTime.Now.ToUniversalTime().AddDays(2), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());

      Instrument forex1 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Forex, "FOREX1", "Forex1", "ForexDescription1", DateTime.Now.ToUniversalTime().AddDays(1), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument forex2 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Forex, "FOREX2", "Forex2", "ForexDescription2", DateTime.Now.ToUniversalTime().AddDays(2), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());

      InstrumentGroup instrumentGroup1 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1", new List<Guid> { stock2.Id, stock3.Id });

      m_database.CreateInstrumentGroup(instrumentGroup1);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{stock2.Id.ToString()}'")
      , "Stock 2 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{stock3.Id.ToString()}'")
      , "Stock 3 not persisted to database.");

      instrumentGroup1.Instruments = new List<Guid> { forex1.Id, forex2.Id };

      m_database.UpdateInstrumentGroup(instrumentGroup1);

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{stock2.Id.ToString()}'")
      , "Stock 2 not removed from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{stock3.Id.ToString()}'")
      , "Stock 3 not removed from database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{forex1.Id.ToString()}'")
      , "Forex 1 not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{forex2.Id.ToString()}'")
      , "Forex 2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteInstrumentGroup_DeleteAndPersist_Success()
    {
      InstrumentGroup instrumentGroup1 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1", Array.Empty<Guid>());
      InstrumentGroup instrumentGroup2 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", instrumentGroup1.Id, "TestInstrumentGroupName2", "TestInstrumentGroupDescription2", Array.Empty<Guid>());

      m_database.CreateInstrumentGroup(instrumentGroup1);
      m_database.CreateInstrumentGroup(instrumentGroup2);

      m_database.DeleteInstrumentGroup(instrumentGroup1.Id);

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup1.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup1.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup1.Name}' " +
        $"AND Description = '{instrumentGroup1.Description}'")
      , "Instrument group 1 not persisted to database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup1.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group 1 and instrument association not removed from database.");

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup2.Name}' " +
        $"AND Description = '{instrumentGroup2.Description}'")
      , "Instrument group 2 not persisted to database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup2.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group 2 and instrument association not removed from database.");
    }

    [TestMethod]
    public void DeleteInstumentGroupChild_Update_Success()
    {
      InstrumentGroup instrumentGroup1 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName1", "TestInstrumentGroupDescription1", Array.Empty<Guid>());
      InstrumentGroup instrumentGroup2 = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", instrumentGroup1.Id, "TestInstrumentGroupName2", "TestInstrumentGroupDescription2", Array.Empty<Guid>());

      m_database.CreateInstrumentGroup(instrumentGroup1);
      m_database.CreateInstrumentGroup(instrumentGroup2);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup2.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup2.Name}' " +
        $"AND Description = '{instrumentGroup2.Description}'")
      , "Instrument group 2 not persisted to database.");

      m_database.DeleteInstrumentGroupChild(instrumentGroup1.Id, instrumentGroup2.Id);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup2.Id.ToString()}' " +
        $"AND ParentId = '{InstrumentGroup.InstrumentGroupRoot.ToString()}' " +
        $"AND Name = '{instrumentGroup2.Name}' " +
        $"AND Description = '{instrumentGroup2.Description}'")
      , "Instrument group 2 not persisted to database.");
    }

    [TestMethod]
    public void DeleteInstrument_DataRemoved_Success()
    {
      m_database.CreateInstrument(m_instrument);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not persisted to database.");

      m_database.DeleteInstrument(m_instrument);

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not deleted from database.");
    }

    [TestMethod]
    public void DeleteInstrument_InstrumentAndRelatedDataRemoved_Success()
    {
      DateTime dateTime = DateTime.Now.ToUniversalTime();  //bar data must always be stored in UTC datetime
      DataCacheBars barData = new DataCacheBars(10);
      barData.DateTime = new List<DateTime> { dateTime.AddDays(1), dateTime.AddDays(2), dateTime.AddDays(3), dateTime.AddDays(4), dateTime.AddDays(5), dateTime.AddDays(6), dateTime.AddDays(7), dateTime.AddDays(8), dateTime.AddDays(9), dateTime.AddDays(10) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0, 211.0, 221.0, 231.0, 241.0, 251.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0, 212.0, 222.0, 232.0, 242.0, 252.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0, 213.0, 223.0, 233.0, 243.0, 253.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0, 214.0, 224.0, 234.0, 244.0, 254.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155, 215, 225, 235, 245, 255 };

      InstrumentGroup instrumentGroup = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName", "TestInstrumentGroupDescription", new List<Guid> { m_instrument.Id });

      m_database.CreateInstrumentGroup(instrumentGroup);
      m_database.CreateInstrument(m_instrument);
      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, Resolution.Day, barData);

      Fundamental fundamental = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInstrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental);

      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      double value = 1.0;

      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime, value);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not persisted to database.");
      Assert.AreEqual(10, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, Resolution.Day),
        $"Ticker = '{m_instrument.Ticker}'")
      , "Actual bar values from list not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group and instrument association not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalAssociations),
        $"Id = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND InstrumentId = '{instrumentFundamental.InstrumentId.ToString()}'")
      , "Instrument fundamental association not persisted to database.");
      Assert.AreEqual(1, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");

      Assert.AreEqual(14, m_database.DeleteInstrument(m_instrument), "Delete instrument returned the incorrect number of rows removed");

      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrument,
        $"Id = '{m_instrument.Id.ToString()}' " +
        $"AND Type = {(int)m_instrument.Type} " +
        $"AND Ticker = '{m_instrument.Ticker}' " +
        $"AND PrimaryExchangeId = '{m_instrument.PrimaryExchangeId.ToString()}' " +
        $"AND InceptionDate = {m_instrument.InceptionDate.ToUniversalTime().ToBinary()}")
      , "Instrument not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentData, Resolution.Day),
        $"Ticker = '{m_instrument.Ticker}'")
      , "Actual bar values from list not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroupInstrument,
        $"InstrumentGroupId = '{instrumentGroup.Id.ToString()}' " +
        $"AND InstrumentId = '{m_instrument.Id.ToString()}'")
      , "Instrument group and instrument association not deleted from database.");
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalAssociations),
        $"Id = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND InstrumentId = '{instrumentFundamental.InstrumentId.ToString()}'")
      , "Instrument fundamental association not persisted to database.");
      Assert.AreEqual(0, m_database.GetRowCount(m_database.GetDataProviderDBName(m_dataProvider1.Object.Name, Data.SqliteDatabase.c_TableInstrumentFundamentalValues),
        $"AssociationId = '{instrumentFundamental.AssociationId.ToString()}' " +
        $"AND DateTime = {dateTime.ToBinary()} " +
        $"AND Value = {value.ToString()}")
      , "Instrument fundamental value not persisted to database.");
    }

    [TestMethod]
    public void GetCountry_ExistingCountry_Success()
    {
      m_database.CreateCountry(m_country);
      Country? country = m_database.GetCountry(m_country.Id);
      Assert.IsNotNull(country, "Country returned null");
      Assert.AreEqual(m_country.Id, country.Id, "Country Id mismatch");
      Assert.AreEqual(m_country.IsoCode, country.IsoCode, "IsoCode mismatch");
    }

    [TestMethod]
    public void GetCountry_NonExistingCountry_Success()
    {
      Assert.IsNull(m_database.GetCountry(Guid.Empty), "Invalid GUID did not return null");
    }

    [TestMethod]
    public void GetHoliday_ReturnExistingHoliday_Success()
    {
      Holiday countryHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, 0, 0, MoveWeekendHoliday.DontAdjust);
      Holiday countryHolidayDayOfWeek = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfWeek", HolidayType.DayOfWeek, Months.January, 0, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(countryHolidayDayOfMonth);
      m_database.CreateHoliday(countryHolidayDayOfWeek);

      Holiday? countryHolidayOfMonthReturned = m_database.GetHoliday(countryHolidayDayOfMonth.Id);
      Assert.IsNotNull(countryHolidayOfMonthReturned, "CountryDayOfMonth not returned");
      Assert.AreEqual(countryHolidayDayOfMonth.Id, countryHolidayOfMonthReturned.Id, "Holiday Id mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.ParentId, countryHolidayOfMonthReturned.ParentId, "Parent Id mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.Name, countryHolidayOfMonthReturned.Name, "Name mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.Tag, countryHolidayOfMonthReturned.Tag, "Tag mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.Type, countryHolidayOfMonthReturned.Type, "Type mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.Month, countryHolidayOfMonthReturned.Month, "Month mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.DayOfMonth, countryHolidayOfMonthReturned.DayOfMonth, "DayOfMonth mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.DayOfWeek, countryHolidayOfMonthReturned.DayOfWeek, "DayOfWeek mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.WeekOfMonth, countryHolidayOfMonthReturned.WeekOfMonth, "WeekOfMonth mismatch");
      Assert.AreEqual(countryHolidayDayOfMonth.MoveWeekendHoliday, countryHolidayOfMonthReturned.MoveWeekendHoliday, "MoveWeekendHoliday mismatch");

      Holiday? countryHolidayOfWeekReturned = m_database.GetHoliday(countryHolidayDayOfWeek.Id);
      Assert.IsNotNull(countryHolidayOfWeekReturned, "CountryDayOfWeek not returned");
      Assert.AreEqual(countryHolidayDayOfWeek.Id, countryHolidayOfWeekReturned.Id, "Holiday Id mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.ParentId, countryHolidayOfWeekReturned.ParentId, "Parent Id mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.Tag, countryHolidayOfWeekReturned.Tag, "Tag mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.Name, countryHolidayOfWeekReturned.Name, "Name mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.Type, countryHolidayOfWeekReturned.Type, "Type mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.Month, countryHolidayOfWeekReturned.Month, "Month mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.DayOfMonth, countryHolidayOfWeekReturned.DayOfMonth, "DayOfMonth mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.DayOfWeek, countryHolidayOfWeekReturned.DayOfWeek, "DayOfWeek mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.WeekOfMonth, countryHolidayOfWeekReturned.WeekOfMonth, "WeekOfMonth mismatch");
      Assert.AreEqual(countryHolidayDayOfWeek.MoveWeekendHoliday, countryHolidayOfWeekReturned.MoveWeekendHoliday, "MoveWeekendHoliday mismatch");
    }

    [TestMethod]
    public void GetHolidays_CountryReturnPersistedData_Success()
    {
      Holiday countryHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, 0, 0, MoveWeekendHoliday.DontAdjust);
      Holiday countryHolidayDayOfWeek = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfWeek", HolidayType.DayOfWeek, Months.January, 0, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(countryHolidayDayOfMonth);
      m_database.CreateHoliday(countryHolidayDayOfWeek);

      IList<Holiday> holidays = m_database.GetHolidays();
      Assert.AreEqual(2, holidays.Count, "Returned country holiday count is not correct.");
      Assert.IsNotNull(holidays.Where(x => x.Id == countryHolidayDayOfMonth.Id && x.Name == countryHolidayDayOfMonth.Name && x.Type == countryHolidayDayOfMonth.Type &&
                                    x.Month == countryHolidayDayOfMonth.Month && countryHolidayDayOfMonth.DayOfMonth == countryHolidayDayOfMonth.DayOfMonth && x.MoveWeekendHoliday == countryHolidayDayOfMonth.MoveWeekendHoliday).Single(), "countryHolidayDayOfMonth not returned in stored data.");
      Assert.IsNotNull(holidays.Where(x => x.Id == countryHolidayDayOfWeek.Id && x.Name == countryHolidayDayOfWeek.Name && x.Type == countryHolidayDayOfWeek.Type &&
                                    x.Month == countryHolidayDayOfWeek.Month && countryHolidayDayOfWeek.DayOfMonth == countryHolidayDayOfWeek.DayOfMonth && x.MoveWeekendHoliday == countryHolidayDayOfWeek.MoveWeekendHoliday).Single(), "countryHolidayDayOfWeek not returned in stored data.");
    }

    [TestMethod]
    public void GetHolidays_ExchangeReturnPersistedData_Success()
    {
      Holiday exchangeHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      Holiday exchangeHolidayDayOfWeek = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfWeek", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(exchangeHolidayDayOfMonth);
      m_database.CreateHoliday(exchangeHolidayDayOfWeek);

      IList<Holiday> holidays = m_database.GetHolidays();
      Assert.AreEqual(2, holidays.Count, "Returned exchange holiday count is not correct.");
      Assert.IsNotNull(holidays.Where(x => x.Id == exchangeHolidayDayOfMonth.Id && x.Name == exchangeHolidayDayOfMonth.Name && x.Type == exchangeHolidayDayOfMonth.Type &&
                                    x.Month == exchangeHolidayDayOfMonth.Month && exchangeHolidayDayOfMonth.DayOfMonth == exchangeHolidayDayOfMonth.DayOfMonth && x.MoveWeekendHoliday == exchangeHolidayDayOfMonth.MoveWeekendHoliday).Single(), "exchangeHolidayDayOfMonth not returned in stored data.");
      Assert.IsNotNull(holidays.Where(x => x.Id == exchangeHolidayDayOfWeek.Id && x.Name == exchangeHolidayDayOfWeek.Name && x.Type == exchangeHolidayDayOfWeek.Type &&
                                    x.Month == exchangeHolidayDayOfWeek.Month && exchangeHolidayDayOfWeek.DayOfMonth == exchangeHolidayDayOfWeek.DayOfMonth && x.MoveWeekendHoliday == exchangeHolidayDayOfWeek.MoveWeekendHoliday).Single(), "exchangeHolidayDayOfWeek not returned in stored data.");
    }

    [TestMethod]
    public void GetHolidays_ReturnHolidaysForParent_Success()
    {
      Holiday countryHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, 0, 0, MoveWeekendHoliday.DontAdjust);
      Holiday countryHolidayDayOfWeek = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_country.Id, "CountryDayOfWeek", HolidayType.DayOfWeek, Months.January, 0, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(countryHolidayDayOfMonth);
      m_database.CreateHoliday(countryHolidayDayOfWeek);

      Holiday exchangeHolidayDayOfMonth = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfMonth", HolidayType.DayOfMonth, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      Holiday exchangeHolidayDayOfWeek = new Holiday(Guid.NewGuid(), Holiday.DefaultAttributeSet, "TagValue", m_exchange.Id, "ExchangeDayOfWeek", HolidayType.DayOfWeek, Months.January, 1, DayOfWeek.Monday, WeekOfMonth.First, MoveWeekendHoliday.DontAdjust);
      m_database.CreateHoliday(exchangeHolidayDayOfMonth);
      m_database.CreateHoliday(exchangeHolidayDayOfWeek);

      IList<Holiday> holidays = m_database.GetHolidays(m_exchange.Id);
      Assert.AreEqual(2, holidays.Count, "Returned exchange parent holiday count is not correct.");
      Assert.IsNotNull(holidays.Where(x => x.Id == exchangeHolidayDayOfMonth.Id && x.Name == exchangeHolidayDayOfMonth.Name && x.Type == exchangeHolidayDayOfMonth.Type &&
                                    x.Month == exchangeHolidayDayOfMonth.Month && exchangeHolidayDayOfMonth.DayOfMonth == exchangeHolidayDayOfMonth.DayOfMonth && x.MoveWeekendHoliday == exchangeHolidayDayOfMonth.MoveWeekendHoliday).Single(), "exchangeHolidayDayOfMonth not returned in stored data.");
      Assert.IsNotNull(holidays.Where(x => x.Id == exchangeHolidayDayOfWeek.Id && x.Name == exchangeHolidayDayOfWeek.Name && x.Type == exchangeHolidayDayOfWeek.Type &&
                                    x.Month == exchangeHolidayDayOfWeek.Month && exchangeHolidayDayOfWeek.DayOfMonth == exchangeHolidayDayOfWeek.DayOfMonth && x.MoveWeekendHoliday == exchangeHolidayDayOfWeek.MoveWeekendHoliday).Single(), "exchangeHolidayDayOfWeek not returned in stored data.");
    }

    [TestMethod]
    public void GetExchange_ReturnsExistingExchange_Success()
    {
      m_database.CreateExchange(m_exchange);
      Exchange? exchange = m_database.GetExchange(m_exchange.Id);
      Assert.IsNotNull(exchange, "Exchange returned null");
      Assert.AreEqual(m_exchange.Id, exchange.Id, "Exchange Id mismatch");
      Assert.AreEqual(m_exchange.CountryId, exchange.CountryId, "Country Id mismatch");
      Assert.AreEqual(m_exchange.Name, exchange.Name, "Name mismatch");
      Assert.AreEqual(m_exchange.Name, exchange.Name, "Name mismatch");
      Assert.AreEqual(m_exchange.TimeZone.Id, exchange.TimeZone.Id, "TimeZone Id mismatch");
    }

    [TestMethod]
    public void GetExchanges_ReturnPersistedData_Success()
    {
      Exchange secondExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Second test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      Exchange thirdExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Third test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);

      m_database.CreateExchange(m_exchange);
      m_database.CreateExchange(secondExchange);
      m_database.CreateExchange(thirdExchange);

      IList<Exchange> exchanges = m_database.GetExchanges();
      Assert.AreEqual(4, exchanges.Count, "Returned exchange holiday count is not correct.");  //+1 for the Global Exchange
      Assert.IsNotNull(exchanges.Where(x => x.Id == m_exchange.Id && x.CountryId == m_exchange.CountryId && x.TimeZone.Id == m_exchange.TimeZone.Id).Single(), "m_exchange not returned in stored data.");
      Assert.IsNotNull(exchanges.Where(x => x.Id == secondExchange.Id && x.CountryId == secondExchange.CountryId && x.TimeZone.Id == secondExchange.TimeZone.Id).Single(), "secondExchange not returned in stored data.");
      Assert.IsNotNull(exchanges.Where(x => x.Id == thirdExchange.Id && x.CountryId == thirdExchange.CountryId && x.TimeZone.Id == thirdExchange.TimeZone.Id).Single(), "thirdExchange not returned in stored data.");
    }

    [TestMethod]
    public void GetSession_ReturnsPersistedData_Success()
    {
      TimeOnly preMarketStartTime = new TimeOnly(6, 0);
      TimeOnly preMarketEndTime = new TimeOnly(9, 29);
      TimeOnly mainStartTime = new TimeOnly(9, 30);
      TimeOnly mainEndTime = new TimeOnly(15, 59);
      TimeOnly postMarketStartTime = new TimeOnly(16, 0);
      TimeOnly postMarketEndTime = new TimeOnly(21, 00);

      Session preMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Pre-market Session", m_exchange.Id, DayOfWeek.Monday, preMarketStartTime, preMarketEndTime);
      Session mainSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Main Session", m_exchange.Id, DayOfWeek.Monday, mainStartTime, mainEndTime);
      Session postMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Post-market Session", m_exchange.Id, DayOfWeek.Monday, postMarketStartTime, postMarketEndTime);

      m_database.CreateSession(preMarketSession);
      m_database.CreateSession(mainSession);
      m_database.CreateSession(postMarketSession);

      Session? mainPersistedSession = m_database.GetSession(mainSession.Id);

      Assert.IsNotNull(mainPersistedSession, "Data store did not return main session.");
      Assert.AreEqual(mainSession.Id, mainPersistedSession.Id, "Id is not the same");
      Assert.AreEqual(mainSession.ExchangeId, mainPersistedSession.ExchangeId, "ExchangeId is not the same");
      Assert.AreEqual(mainSession.Name, mainPersistedSession.Name, "Name is not the same");
      Assert.AreEqual(mainSession.DayOfWeek, mainPersistedSession.DayOfWeek, "DayOfWeek is not the same");
      Assert.AreEqual(mainSession.Start, mainPersistedSession.Start, "Start time is not the same");
      Assert.AreEqual(mainSession.End, mainPersistedSession.End, "End time is not the same");
    }


    [TestMethod]
    public void GetSessions_ReturnsSessionsByExchange_Success()
    {
      TimeOnly preMarketStartTime = new TimeOnly(6, 0);
      TimeOnly preMarketEndTime = new TimeOnly(9, 29);
      TimeOnly mainStartTime = new TimeOnly(9, 30);
      TimeOnly mainEndTime = new TimeOnly(15, 59);
      TimeOnly postMarketStartTime = new TimeOnly(16, 0);
      TimeOnly postMarketEndTime = new TimeOnly(21, 00);

      Exchange secondExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Second test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      Exchange thirdExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Third test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);

      Session preFirstMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Pre-market Session", m_exchange.Id, DayOfWeek.Monday, preMarketStartTime, preMarketEndTime);
      Session mainFirstSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Main Session", m_exchange.Id, DayOfWeek.Monday, mainStartTime, mainEndTime);
      Session postFirstMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Post-market Session", m_exchange.Id, DayOfWeek.Monday, postMarketStartTime, postMarketEndTime);

      Session preSecondMondayMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Pre-market Session", secondExchange.Id, DayOfWeek.Monday, preMarketStartTime, preMarketEndTime);
      Session mainSecondMondaySession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Main Session", secondExchange.Id, DayOfWeek.Monday, mainStartTime, mainEndTime);
      Session postSecondMondayMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Post-market Session", secondExchange.Id, DayOfWeek.Monday, postMarketStartTime, postMarketEndTime);
      Session preSecondTuesdayMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Pre-market Session", secondExchange.Id, DayOfWeek.Tuesday, preMarketStartTime, preMarketEndTime);
      Session mainSecondTuesdaySession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Main Session", secondExchange.Id, DayOfWeek.Tuesday, mainStartTime, mainEndTime);
      Session postSecondTuesdayMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Post-market Session", secondExchange.Id, DayOfWeek.Tuesday, postMarketStartTime, postMarketEndTime);

      Session preThirdMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Pre-market Session", thirdExchange.Id, DayOfWeek.Monday, preMarketStartTime, preMarketEndTime);
      Session mainThirdSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Main Session", thirdExchange.Id, DayOfWeek.Monday, mainStartTime, mainEndTime);
      Session postThirdMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Post-market Session", thirdExchange.Id, DayOfWeek.Monday, postMarketStartTime, postMarketEndTime);

      m_database.CreateSession(preFirstMarketSession);
      m_database.CreateSession(mainFirstSession);
      m_database.CreateSession(postFirstMarketSession);

      m_database.CreateSession(preSecondMondayMarketSession);
      m_database.CreateSession(mainSecondMondaySession);
      m_database.CreateSession(postSecondMondayMarketSession);
      m_database.CreateSession(preSecondTuesdayMarketSession);
      m_database.CreateSession(mainSecondTuesdaySession);
      m_database.CreateSession(postSecondTuesdayMarketSession);

      m_database.CreateSession(preThirdMarketSession);
      m_database.CreateSession(mainThirdSession);
      m_database.CreateSession(postThirdMarketSession);

      IList<Session> sessions = m_database.GetSessions(secondExchange.Id);
      Assert.AreEqual(6, sessions.Count, "Returned exchange session count is not correct.");
      Assert.IsNotNull(sessions.Where(x => x.ExchangeId == secondExchange.Id && x.DayOfWeek == DayOfWeek.Monday && x.Name == "Pre-market Session").Single(), "Pre-market Monday session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.ExchangeId == secondExchange.Id && x.DayOfWeek == DayOfWeek.Monday && x.Name == "Main Session").Single(), "Main Monday session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.ExchangeId == secondExchange.Id && x.DayOfWeek == DayOfWeek.Monday && x.Name == "Post-market Session").Single(), "Post-market Monday session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.ExchangeId == secondExchange.Id && x.DayOfWeek == DayOfWeek.Tuesday && x.Name == "Pre-market Session").Single(), "Pre-market Tuesday session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.ExchangeId == secondExchange.Id && x.DayOfWeek == DayOfWeek.Tuesday && x.Name == "Main Session").Single(), "Main Tuesday session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.ExchangeId == secondExchange.Id && x.DayOfWeek == DayOfWeek.Tuesday && x.Name == "Post-market Session").Single(), "Post-market Tuesday session not returned in stored data.");
    }

    [TestMethod]
    public void GetSessions_ReturnsAllPersistedSessions_Success()
    {
      TimeOnly preMarketStartTime = new TimeOnly(6, 0);
      TimeOnly preMarketEndTime = new TimeOnly(9, 29);
      TimeOnly mainStartTime = new TimeOnly(9, 30);
      TimeOnly mainEndTime = new TimeOnly(15, 59);
      TimeOnly postMarketStartTime = new TimeOnly(16, 0);
      TimeOnly postMarketEndTime = new TimeOnly(21, 00);

      Session preMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Pre-market Session", m_exchange.Id, DayOfWeek.Monday, preMarketStartTime, preMarketEndTime);
      Session mainSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Main Session", m_exchange.Id, DayOfWeek.Monday, mainStartTime, mainEndTime);
      Session postMarketSession = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", "Post-market Session", m_exchange.Id, DayOfWeek.Monday, postMarketStartTime, postMarketEndTime);

      m_database.CreateSession(preMarketSession);
      m_database.CreateSession(mainSession);
      m_database.CreateSession(postMarketSession);

      IList<Session> sessions = m_database.GetSessions();
      Assert.AreEqual(10, sessions.Count, "Returned session count is not correct.");    //+7 for the global exchange that also has sessions for all week days for 24/7 trading
      Assert.IsNotNull(sessions.Where(x => x.Id == preMarketSession.Id && x.Name == preMarketSession.Name && x.ExchangeId == preMarketSession.ExchangeId && x.DayOfWeek == preMarketSession.DayOfWeek && x.Start == preMarketSession.Start && x.End == preMarketSession.End).Single(), "pre-market session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.Id == mainSession.Id && x.Name == mainSession.Name && x.ExchangeId == mainSession.ExchangeId && x.DayOfWeek == mainSession.DayOfWeek && x.Start == mainSession.Start && x.End == mainSession.End).Single(), "main session not returned in stored data.");
      Assert.IsNotNull(sessions.Where(x => x.Id == postMarketSession.Id && x.Name == postMarketSession.Name && x.ExchangeId == postMarketSession.ExchangeId && x.DayOfWeek == postMarketSession.DayOfWeek && x.Start == postMarketSession.Start && x.End == postMarketSession.End).Single(), "post-market session not returned in stored data.");
    }

    [TestMethod]
    public void GetInstrumentGroupInstruments_ReturnPersistedData_Success()
    {
      InstrumentGroup instrumentGroup = new InstrumentGroup(Guid.NewGuid(), InstrumentGroup.DefaultAttributeSet, "TagValue", InstrumentGroup.InstrumentGroupRoot, "TestInstrumentGroupName", "TestInstrumentGroupDescription", new List<Guid> { m_instrument.Id });
      m_database.CreateInstrumentGroup(instrumentGroup);

      Assert.AreEqual(1, m_database.GetRowCount(Data.SqliteDatabase.c_TableInstrumentGroup,
        $"Id = '{instrumentGroup.Id.ToString()}' " +
        $"AND ParentId = '{instrumentGroup.ParentId.ToString()}' " +
        $"AND Name = '{instrumentGroup.Name}' " +
        $"AND Description = '{instrumentGroup.Description}'")
      , "Instrument group not persisted to database.");

      IList<Guid> instrumentIds = m_database.GetInstrumentGroupInstruments(instrumentGroup.Id);
      Assert.AreEqual(1, instrumentIds.Count, "Number of returned instrument id's are incorrect.");
      Assert.IsNotNull(instrumentIds.Where(x => x == m_instrument.Id).Single(), "m_instrument not returned as child of instrument group.");
    }

    [TestMethod]
    public void GetInstruments_ReturnPersistedData_Success()
    {
      Instrument instrumentTest2 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "TEST2", "TestInstrument2", "TestInstrumentDescription2", DateTime.Now.ToUniversalTime().AddDays(1), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument instrumentTest3 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "TEST3", "TestInstrument3", "TestInstrumentDescription3", DateTime.Now.ToUniversalTime().AddDays(2), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      m_database.CreateInstrument(m_instrument);
      m_database.CreateInstrument(instrumentTest2);
      m_database.CreateInstrument(instrumentTest3);

      IList<Instrument> instruments = m_database.GetInstruments();

      Assert.AreEqual(3, instruments.Count, "Returned instrument count is not correct.");
      Assert.IsNotNull(instruments.Where(x => x.Id == m_instrument.Id && x.Type == m_instrument.Type && x.Ticker == m_instrument.Ticker && x.Name == m_instrument.Name && x.Description == m_instrument.Description && x.InceptionDate == m_instrument.InceptionDate).Single(), "m_instrument not found");
      Assert.IsNotNull(instruments.Where(x => x.Id == instrumentTest2.Id && x.Type == instrumentTest2.Type && x.Ticker == instrumentTest2.Ticker && x.Name == instrumentTest2.Name && x.Description == instrumentTest2.Description && x.InceptionDate == instrumentTest2.InceptionDate).Single(), "instrumentTest2 not found");
      Assert.IsNotNull(instruments.Where(x => x.Id == instrumentTest3.Id && x.Type == instrumentTest3.Type && x.Ticker == instrumentTest3.Ticker && x.Name == instrumentTest3.Name && x.Description == instrumentTest3.Description && x.InceptionDate == instrumentTest3.InceptionDate).Single(), "instrumentTest3 not found");
    }

    [TestMethod]
    public void GetInstruments_ReturnsSecondaryExchanges_Success()
    {
      Exchange secondExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Second test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      Exchange thirdExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Third test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_instrument = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "TEST", "TestInstrument", "TestInstrumentDescription", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, new List<Guid> { secondExchange.Id, thirdExchange.Id });
      m_database.CreateInstrument(m_instrument);

      IList<Instrument> instruments = m_database.GetInstruments();
      Assert.AreEqual(1, instruments.Count, "Returned instrument count is not correct.");
      Assert.AreEqual(2, instruments.ElementAt(0).SecondaryExchangeIds.Count, "Returned secondary exchanges count is not correct.");
      Assert.IsNotNull(instruments.Where(x => x.Id == m_instrument.Id && x.Type == m_instrument.Type && x.Ticker == m_instrument.Ticker).Single(), "m_instrument not found");
      Assert.IsNotNull(instruments.ElementAt(0).SecondaryExchangeIds.Where(x => x == secondExchange.Id).Single(), "secondExchange not returned as secondary exchange for instrument.");
      Assert.IsNotNull(instruments.ElementAt(0).SecondaryExchangeIds.Where(x => x == thirdExchange.Id).Single(), "thirdExchange not returned as secondary exhange for instrument.");
    }

    [TestMethod]
    public void GetInstruments_ByInstrumentType_Success()
    {
      Instrument stock2 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "STOCK2", "Stock2", "StockDescription2", DateTime.Now.ToUniversalTime().AddDays(1), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument stock3 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "STOCK3", "Stock3", "StockDescription3", DateTime.Now.ToUniversalTime().AddDays(2), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());

      Instrument forex1 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Forex, "FOREX1", "Forex1", "ForexDescription1", DateTime.Now.ToUniversalTime().AddDays(1), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument forex2 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Forex, "FOREX2", "Forex2", "ForexDescription2", DateTime.Now.ToUniversalTime().AddDays(2), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument forex3 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Forex, "FOREX3", "Forex3", "ForexDescription3", DateTime.Now.ToUniversalTime().AddDays(3), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      Instrument forex4 = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Forex, "FOREX4", "Forex4", "ForexDescription4", DateTime.Now.ToUniversalTime().AddDays(4), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());

      m_database.CreateInstrument(m_instrument);
      m_database.CreateInstrument(stock2);
      m_database.CreateInstrument(stock3);

      m_database.CreateInstrument(forex1);
      m_database.CreateInstrument(forex2);
      m_database.CreateInstrument(forex3);
      m_database.CreateInstrument(forex4);

      IList<Instrument> stocks = m_database.GetInstruments(InstrumentType.Stock);

      Assert.AreEqual(3, stocks.Count, "Returned stock count is not correct.");
      Assert.IsNotNull(stocks.Where(x => x.Id == m_instrument.Id && x.Type == m_instrument.Type && x.Ticker == m_instrument.Ticker && x.Name == m_instrument.Name && x.Description == m_instrument.Description && x.InceptionDate == m_instrument.InceptionDate).Single(), "m_instrument not found");
      Assert.IsNotNull(stocks.Where(x => x.Id == stock2.Id && x.Type == stock2.Type && x.Ticker == stock2.Ticker && x.Name == stock2.Name && x.Description == stock2.Description && x.InceptionDate == stock2.InceptionDate).Single(), "stockTest2 not found");
      Assert.IsNotNull(stocks.Where(x => x.Id == stock3.Id && x.Type == stock3.Type && x.Ticker == stock3.Ticker && x.Name == stock3.Name && x.Description == stock3.Description && x.InceptionDate == stock3.InceptionDate).Single(), "stockTest3 not found");

      IList<Instrument> forex = m_database.GetInstruments(InstrumentType.Forex);
      Assert.AreEqual(4, forex.Count, "Returned forex count is not correct.");
      Assert.IsNotNull(forex.Where(x => x.Id == forex1.Id && x.Type == forex1.Type && x.Ticker == forex1.Ticker && x.Name == forex1.Name && x.Description == forex1.Description && x.InceptionDate == forex1.InceptionDate).Single(), "forex1 not found");
      Assert.IsNotNull(forex.Where(x => x.Id == forex2.Id && x.Type == forex2.Type && x.Ticker == forex2.Ticker && x.Name == forex2.Name && x.Description == forex2.Description && x.InceptionDate == forex2.InceptionDate).Single(), "forex2 not found");
      Assert.IsNotNull(forex.Where(x => x.Id == forex3.Id && x.Type == forex3.Type && x.Ticker == forex3.Ticker && x.Name == forex3.Name && x.Description == forex3.Description && x.InceptionDate == forex3.InceptionDate).Single(), "forex3 not found");
      Assert.IsNotNull(forex.Where(x => x.Id == forex4.Id && x.Type == forex4.Type && x.Ticker == forex4.Ticker && x.Name == forex4.Name && x.Description == forex4.Description && x.InceptionDate == forex4.InceptionDate).Single(), "forex4 not found");
    }

    private static string wildCardToRegex(string value)
    {
      return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }

    /// <summary>
    /// Create test instruments with a specific format to ensure that the instruments retrieved by database select statements would order the instruments correctly. (Need to use the format 'STOCK001' instead of 'STOCK1' since the latter would order the instruments incorrectly in string sorts).
    /// </summary>
    private void createTestInstrumentsForGetMethods()
    {
      for (int i = 0; i < 200; i++)
      {
        string formattedNumber = i.ToString("D3");
        m_database.CreateInstrument(new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, $"STOCK{formattedNumber}", InstrumentType.Stock, $"STOCK{formattedNumber}", $"Stock Name {formattedNumber}", $"Stock Description {formattedNumber}", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>()));  //database layer stores dates in UTC
      }
      for (int i = 0; i < 200; i++)
      {
        string formattedNumber = i.ToString("D3");
        m_database.CreateInstrument(new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, $"FOREX{formattedNumber}", InstrumentType.Forex, $"FOREX{formattedNumber}", $"Forex Name {formattedNumber}", $"Forex Description {formattedNumber}", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>()));  //database layer stores dates in UTC
      }
      for (int i = 0; i < 200; i++)
      {
        string formattedNumber = i.ToString("D3");
        m_database.CreateInstrument(new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, $"FUTURE{formattedNumber}", InstrumentType.Future, $"FUTURE{formattedNumber}", $"Future Name {formattedNumber}", $"Future Description {formattedNumber}", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>()));  //database layer stores dates in UTC
      }
      for (int i = 0; i < 200; i++)
      {
        string formattedNumber = i.ToString("D3");
        m_database.CreateInstrument(new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, $"CRYPTO{formattedNumber}", InstrumentType.Crypto, $"CRYPTO{formattedNumber}", $"Crypto Name {formattedNumber}", $"Crypto Description {formattedNumber}", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>()));  //database layer stores dates in UTC
      }
    }

    [TestMethod]
    [DataRow(InstrumentType.Stock, "", "", "", 200)]
    [DataRow(InstrumentType.Stock, "*100", "", "", 1)]
    [DataRow(InstrumentType.Stock, "", "*100", "", 1)]
    [DataRow(InstrumentType.Stock, "", "", "*100", 1)]
    [DataRow(InstrumentType.Stock, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Stock, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Stock, "", "", "*?9?", 20)]
    [DataRow(InstrumentType.Forex, "", "", "", 200)]
    [DataRow(InstrumentType.Forex, "*100", "", "", 1)]
    [DataRow(InstrumentType.Forex, "", "*100", "", 1)]
    [DataRow(InstrumentType.Forex, "", "", "*100", 1)]
    [DataRow(InstrumentType.Forex, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Forex, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Forex, "", "", "*?9?", 20)]
    [DataRow(InstrumentType.Future, "", "", "", 200)]
    [DataRow(InstrumentType.Future, "*100", "", "", 1)]
    [DataRow(InstrumentType.Future, "", "*100", "", 1)]
    [DataRow(InstrumentType.Future, "", "", "*100", 1)]
    [DataRow(InstrumentType.Future, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Future, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Future, "", "", "*?9?", 20)]
    [DataRow(InstrumentType.Crypto, "", "", "", 200)]
    [DataRow(InstrumentType.Crypto, "*100", "", "", 1)]
    [DataRow(InstrumentType.Crypto, "", "*100", "", 1)]
    [DataRow(InstrumentType.Crypto, "", "", "*100", 1)]
    [DataRow(InstrumentType.Crypto, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Crypto, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Crypto, "", "", "*?9?", 20)]
    public void GetInstruments_WithFilter_Success(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int expectedCount)
    {
      createTestInstrumentsForGetMethods();
      IList<Instrument> actualInstruments = m_database.GetInstruments(instrumentType, tickerFilter, nameFilter, descriptionFilter);
      Assert.AreEqual(expectedCount, actualInstruments.Count, "Expected and actual returned count differs.");

      string tickerRegex = wildCardToRegex(tickerFilter);
      string nameRegex = wildCardToRegex(nameFilter);
      string descriptionRegex = wildCardToRegex(descriptionFilter);

      foreach (Instrument instrument in actualInstruments)
      {
        Assert.AreEqual(instrumentType, instrument.Type, $"Instrument {instrument.Ticker} did not have the correct type");
        if (tickerFilter != string.Empty) Assert.IsTrue(Regex.IsMatch(instrument.Ticker, tickerRegex));
        if (nameFilter != string.Empty) Assert.IsTrue(Regex.IsMatch(instrument.Name, nameRegex));
        if (descriptionFilter != string.Empty) Assert.IsTrue(Regex.IsMatch(instrument.Description, descriptionRegex));
      }
    }

    [TestMethod]
    [DataRow(InstrumentType.Stock, "", "", "", 0, 10, 10, "STOCK000")]
    [DataRow(InstrumentType.Stock, "", "", "", 1, 10, 10, "STOCK010")]
    [DataRow(InstrumentType.Stock, "", "", "", 2, 10, 10, "STOCK020")]
    [DataRow(InstrumentType.Stock, "", "", "", 0, 50, 50, "STOCK000")]
    [DataRow(InstrumentType.Stock, "", "", "", 1, 50, 50, "STOCK050")]
    [DataRow(InstrumentType.Stock, "", "", "", 2, 50, 50, "STOCK100")]
    [DataRow(InstrumentType.Stock, "*100", "", "", 0, 10, 1, "STOCK100")]
    [DataRow(InstrumentType.Stock, "", "*100", "", 0, 10, 1, "STOCK100")]
    [DataRow(InstrumentType.Stock, "", "", "*100", 0, 10, 1, "STOCK100")]
    [DataRow(InstrumentType.Stock, "*?9?", "", "", 0, 10, 10, "STOCK090")]
    [DataRow(InstrumentType.Stock, "", "*?9?", "", 0, 10, 10, "STOCK090")]
    [DataRow(InstrumentType.Stock, "", "", "*?9?", 0, 10, 10, "STOCK090")]
    [DataRow(InstrumentType.Forex, "", "", "", 0, 10, 10, "FOREX000")]
    [DataRow(InstrumentType.Forex, "", "", "", 1, 10, 10, "FOREX010")]
    [DataRow(InstrumentType.Forex, "", "", "", 2, 10, 10, "FOREX020")]
    [DataRow(InstrumentType.Forex, "", "", "", 0, 50, 50, "FOREX000")]
    [DataRow(InstrumentType.Forex, "", "", "", 1, 50, 50, "FOREX050")]
    [DataRow(InstrumentType.Forex, "", "", "", 2, 50, 50, "FOREX100")]
    [DataRow(InstrumentType.Forex, "*?9?", "", "", 0, 10, 10, "FOREX090")]
    [DataRow(InstrumentType.Forex, "", "*?9?", "", 0, 10, 10, "FOREX090")]
    [DataRow(InstrumentType.Forex, "", "", "*?9?", 0, 10, 10, "FOREX090")]
    [DataRow(InstrumentType.Forex, "*100", "", "", 0, 10, 1, "FOREX100")]
    [DataRow(InstrumentType.Forex, "", "*100", "", 0, 10, 1, "FOREX100")]
    [DataRow(InstrumentType.Forex, "", "", "*100", 0, 10, 1, "FOREX100")]
    [DataRow(InstrumentType.Forex, "*?9?", "", "", 0, 10, 10, "FOREX090")]
    [DataRow(InstrumentType.Forex, "", "*?9?", "", 0, 10, 10, "FOREX090")]
    [DataRow(InstrumentType.Forex, "", "", "*?9?", 0, 10, 10, "FOREX090")]
    [DataRow(InstrumentType.Future, "", "", "", 0, 10, 10, "FUTURE000")]
    [DataRow(InstrumentType.Future, "", "", "", 1, 10, 10, "FUTURE010")]
    [DataRow(InstrumentType.Future, "", "", "", 2, 10, 10, "FUTURE020")]
    [DataRow(InstrumentType.Future, "", "", "", 0, 50, 50, "FUTURE000")]
    [DataRow(InstrumentType.Future, "", "", "", 1, 50, 50, "FUTURE050")]
    [DataRow(InstrumentType.Future, "", "", "", 2, 50, 50, "FUTURE100")]
    [DataRow(InstrumentType.Future, "*?9?", "", "", 0, 10, 10, "FUTURE090")]
    [DataRow(InstrumentType.Future, "", "*?9?", "", 0, 10, 10, "FUTURE090")]
    [DataRow(InstrumentType.Future, "", "", "*?9?", 0, 10, 10, "FUTURE090")]
    [DataRow(InstrumentType.Future, "*100", "", "", 0, 10, 1, "FUTURE100")]
    [DataRow(InstrumentType.Future, "", "*100", "", 0, 10, 1, "FUTURE100")]
    [DataRow(InstrumentType.Future, "", "", "*100", 0, 10, 1, "FUTURE100")]
    [DataRow(InstrumentType.Crypto, "", "", "", 0, 10, 10, "CRYPTO000")]
    [DataRow(InstrumentType.Crypto, "", "", "", 1, 10, 10, "CRYPTO010")]
    [DataRow(InstrumentType.Crypto, "", "", "", 2, 10, 10, "CRYPTO020")]
    [DataRow(InstrumentType.Crypto, "", "", "", 0, 50, 50, "CRYPTO000")]
    [DataRow(InstrumentType.Crypto, "", "", "", 1, 50, 50, "CRYPTO050")]
    [DataRow(InstrumentType.Crypto, "", "", "", 2, 50, 50, "CRYPTO100")]
    [DataRow(InstrumentType.Crypto, "*100", "", "", 0, 10, 1, "CRYPTO100")]
    [DataRow(InstrumentType.Crypto, "", "*100", "", 0, 10, 1, "CRYPTO100")]
    [DataRow(InstrumentType.Crypto, "", "", "*100", 0, 10, 1, "CRYPTO100")]
    [DataRow(InstrumentType.Crypto, "*?9?", "", "", 0, 10, 10, "CRYPTO090")]
    [DataRow(InstrumentType.Crypto, "", "*?9?", "", 0, 10, 10, "CRYPTO090")]
    [DataRow(InstrumentType.Crypto, "", "", "*?9?", 0, 10, 10, "CRYPTO090")]
    public void GetInstruments_WithFilterAndPage_Success(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int pageIndex, int pageSize, int expectedCount, string expectedFirstTicker)
    {
      createTestInstrumentsForGetMethods();
      IList<Instrument> actualInstruments = m_database.GetInstrumentsPage(instrumentType, tickerFilter, nameFilter, descriptionFilter, pageIndex, pageSize);
      Assert.AreEqual(expectedCount, actualInstruments.Count, "Expected and actual returned count differs.");

      string tickerRegex = wildCardToRegex(tickerFilter);
      string nameRegex = wildCardToRegex(nameFilter);
      string descriptionRegex = wildCardToRegex(descriptionFilter);

      Instrument firstActualInstrument = actualInstruments[0];
      Assert.AreEqual(expectedFirstTicker, firstActualInstrument.Ticker, "First expected ticker did not match.");

      foreach (Instrument instrument in actualInstruments)
      {
        Assert.AreEqual(instrumentType, instrument.Type, $"Instrument {instrument.Ticker} did not have the correct type");
        if (tickerFilter != string.Empty) Assert.IsTrue(Regex.IsMatch(instrument.Ticker, tickerRegex));
        if (nameFilter != string.Empty) Assert.IsTrue(Regex.IsMatch(instrument.Name, nameRegex));
        if (descriptionFilter != string.Empty) Assert.IsTrue(Regex.IsMatch(instrument.Description, descriptionRegex));
      }
    }

    [TestMethod]
    public void GetInstrumentCount_NoParameters_Success()
    {
      createTestInstrumentsForGetMethods();
      int actualCount = m_database.GetInstrumentCount();
      Assert.AreEqual(800, actualCount);
    }

    [TestMethod]
    [DataRow(InstrumentType.Stock, 200)]
    [DataRow(InstrumentType.Forex, 200)]
    [DataRow(InstrumentType.Future, 200)]
    [DataRow(InstrumentType.Crypto, 200)]
    public void GetInstrumentCount_InstrumentType_Success(InstrumentType instrumentType, int expectedCount)
    {
      createTestInstrumentsForGetMethods();
      int actualCount = m_database.GetInstrumentCount(instrumentType);
      Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    [DataRow(InstrumentType.Stock, "", "", "", 200)]
    [DataRow(InstrumentType.Stock, "*100", "", "", 1)]
    [DataRow(InstrumentType.Stock, "", "*100", "", 1)]
    [DataRow(InstrumentType.Stock, "", "", "*100", 1)]
    [DataRow(InstrumentType.Stock, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Stock, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Stock, "", "", "*?9?", 20)]
    [DataRow(InstrumentType.Forex, "", "", "", 200)]
    [DataRow(InstrumentType.Forex, "*100", "", "", 1)]
    [DataRow(InstrumentType.Forex, "", "*100", "", 1)]
    [DataRow(InstrumentType.Forex, "", "", "*100", 1)]
    [DataRow(InstrumentType.Forex, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Forex, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Forex, "", "", "*?9?", 20)]
    [DataRow(InstrumentType.Future, "", "", "", 200)]
    [DataRow(InstrumentType.Future, "*100", "", "", 1)]
    [DataRow(InstrumentType.Future, "", "*100", "", 1)]
    [DataRow(InstrumentType.Future, "", "", "*100", 1)]
    [DataRow(InstrumentType.Future, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Future, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Future, "", "", "*?9?", 20)]
    [DataRow(InstrumentType.Crypto, "", "", "", 200)]
    [DataRow(InstrumentType.Crypto, "*100", "", "", 1)]
    [DataRow(InstrumentType.Crypto, "", "*100", "", 1)]
    [DataRow(InstrumentType.Crypto, "", "", "*100", 1)]
    [DataRow(InstrumentType.Crypto, "*?9?", "", "", 20)]
    [DataRow(InstrumentType.Crypto, "", "*?9?", "", 20)]
    [DataRow(InstrumentType.Crypto, "", "", "*?9?", 20)]
    public void GetInstrumentCount_InstrumentTypeWithFilters_Success(InstrumentType instrumentType, string tickerFilter, string nameFilter, string descriptionFilter, int expectedCount)
    {
      createTestInstrumentsForGetMethods();
      int actualCount = m_database.GetInstrumentCount(instrumentType, tickerFilter, nameFilter, descriptionFilter);
      Assert.AreEqual(expectedCount, actualCount);
    }

    [TestMethod]
    public void GetInstrument_ById_Success()
    {
      Exchange secondExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Second test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      Exchange thirdExchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "Third test exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      Instrument stock = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "STOCK", "Stock", "StockDescription", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, new List<Guid> { secondExchange.Id, thirdExchange.Id });

      m_database.CreateInstrument(stock);

      Instrument? retrievedStock = m_database.GetInstrument(stock.Id);

      Assert.IsNotNull(retrievedStock, "Data store did not return the stock.");
      Assert.IsNotNull(retrievedStock.SecondaryExchangeIds.Where(x => x == secondExchange.Id).Single());
      Assert.IsNotNull(retrievedStock.SecondaryExchangeIds.Where(x => x == thirdExchange.Id).Single());
    }

    [TestMethod]
    public void GetFundmentals_ReturnPersistedData_Success()
    {
      Fundamental fundamental1 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental1", "TestCountryFundamentalDescription1", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      Fundamental fundamental2 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental2", "TestCountryFundamentalDescription2", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);
      Fundamental fundamental3 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental3", "TestCountryFundamentalDescription3", FundamentalCategory.Country, FundamentalReleaseInterval.Quarterly);
      Fundamental fundamental4 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental1", "TestInsrumentFundamentalDescription1", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      Fundamental fundamental5 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental2", "TestInsrumentFundamentalDescription2", FundamentalCategory.Instrument, FundamentalReleaseInterval.Monthly);
      Fundamental fundamental6 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental3", "TestInsrumentFundamentalDescription3", FundamentalCategory.Instrument, FundamentalReleaseInterval.Quarterly);

      m_database.CreateFundamental(fundamental1);
      m_database.CreateFundamental(fundamental2);
      m_database.CreateFundamental(fundamental3);
      m_database.CreateFundamental(fundamental4);
      m_database.CreateFundamental(fundamental5);
      m_database.CreateFundamental(fundamental6);

      IList<Fundamental> fundamentals = m_database.GetFundamentals();

      Assert.AreEqual(6, fundamentals.Count, "Returned fundamentals count is not correct.");

      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental1.Id && x.Name == fundamental1.Name && x.Description == fundamental1.Description && x.Category == fundamental1.Category && x.Category == fundamental1.Category && x.ReleaseInterval == fundamental1.ReleaseInterval).Single(), "fundamental1 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental2.Id && x.Name == fundamental2.Name && x.Description == fundamental2.Description && x.Category == fundamental2.Category && x.Category == fundamental2.Category && x.ReleaseInterval == fundamental2.ReleaseInterval).Single(), "fundamental2 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental3.Id && x.Name == fundamental3.Name && x.Description == fundamental3.Description && x.Category == fundamental3.Category && x.Category == fundamental3.Category && x.ReleaseInterval == fundamental3.ReleaseInterval).Single(), "fundamental3 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental4.Id && x.Name == fundamental4.Name && x.Description == fundamental4.Description && x.Category == fundamental4.Category && x.Category == fundamental4.Category && x.ReleaseInterval == fundamental4.ReleaseInterval).Single(), "fundamental4 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental5.Id && x.Name == fundamental5.Name && x.Description == fundamental5.Description && x.Category == fundamental5.Category && x.Category == fundamental5.Category && x.ReleaseInterval == fundamental5.ReleaseInterval).Single(), "fundamental5 not found");
      Assert.IsNotNull(fundamentals.Where(x => x.Id == fundamental6.Id && x.Name == fundamental6.Name && x.Description == fundamental6.Description && x.Category == fundamental6.Category && x.Category == fundamental6.Category && x.ReleaseInterval == fundamental6.ReleaseInterval).Single(), "fundamental6 not found");
    }

    [TestMethod]
    public void GetCountryFundmentals_ReturnPersistedData_Success()
    {
      Fundamental fundamental1 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      m_database.CreateFundamental(fundamental1);

      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental1.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;

      Fundamental fundamental2 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInsrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental2.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      DateTime instrumentDateTime = DateTime.Now.ToUniversalTime().AddDays(10);
      double instrumentValue = 10.0;

      m_database.CreateFundamental(fundamental1);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime, value);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime.AddDays(1), value + 1.0);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, dateTime.AddDays(2), value + 2.0);

      m_database.CreateFundamental(fundamental2);
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, instrumentDateTime, instrumentValue);
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, instrumentDateTime.AddDays(1), instrumentValue + 1.0);
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, instrumentDateTime.AddDays(2), instrumentValue + 2.0);

      IList<CountryFundamental> countryFundamentalValues = m_database.GetCountryFundamentals(m_dataProvider1.Object.Name);
      Assert.AreEqual(1, countryFundamentalValues.Count, "Returned country fundamental value count is not correct.");
      CountryFundamental countryFundamentalValue = countryFundamentalValues.ElementAt(0);
      Assert.IsNotNull(countryFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime && (double)x.Item2 == value), "Country fundamental value 1 not found");
      Assert.IsNotNull(countryFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(1) && (double)x.Item2 == value + 1.0), "Country fundamental value 2 not found");
      Assert.IsNotNull(countryFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(2) && (double)x.Item2 == value + 2.0), "Country fundamental value 3 not found");
    }

    [TestMethod]
    public void GetInstrumentFundmentals_ReturnPersistedData_Success()
    {
      Fundamental fundamental1 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestInstrumentFundamental", "TestInsrumentFundamentalDescription", FundamentalCategory.Instrument, FundamentalReleaseInterval.Daily);
      InstrumentFundamental instrumentFundamental = new InstrumentFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental1.Id, m_instrument.Id);
      m_database.CreateInstrumentFundamental(instrumentFundamental);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 1.0;

      Fundamental fundamental2 = new Fundamental(Guid.NewGuid(), Fundamental.DefaultAttributeSet, "TagValue", "TestCountryFundamental", "TestCountryFundamentalDescription", FundamentalCategory.Country, FundamentalReleaseInterval.Daily);
      CountryFundamental countryFundamental = new CountryFundamental(m_dataProvider1.Object.Name, Guid.NewGuid(), fundamental2.Id, m_country.Id);
      m_database.CreateCountryFundamental(countryFundamental);

      DateTime countryDateTime = DateTime.Now.ToUniversalTime().AddDays(10);
      double countryValue = 10.0;

      m_database.CreateFundamental(fundamental1);

      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime, value);
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime.AddDays(1), value + 1.0);
      m_database.UpdateInstrumentFundamental(m_dataProvider1.Object.Name, instrumentFundamental.FundamentalId, instrumentFundamental.InstrumentId, dateTime.AddDays(2), value + 2.0);

      m_database.CreateFundamental(fundamental2);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, countryDateTime, countryValue);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, countryDateTime.AddDays(1), countryValue + 1.0);
      m_database.UpdateCountryFundamental(m_dataProvider1.Object.Name, countryFundamental.FundamentalId, countryFundamental.CountryId, countryDateTime.AddDays(2), countryValue + 2.0);

      IList<InstrumentFundamental> instrumentFundamentalValues = m_database.GetInstrumentFundamentals(m_dataProvider1.Object.Name);
      Assert.AreEqual(1, instrumentFundamentalValues.Count, "Returned country fundamental value count is not correct.");
      InstrumentFundamental instrumentFundamentalValue = instrumentFundamentalValues.ElementAt(0);
      Assert.IsNotNull(instrumentFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime && (double)x.Item2 == value), "Instrument fundamental value 1 not found");
      Assert.IsNotNull(instrumentFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(1) && (double)x.Item2 == value + 1.0), "Instrument fundamental value 2 not found");
      Assert.IsNotNull(instrumentFundamentalValue.Values.FirstOrDefault(x => x.Item1 == dateTime.AddDays(2) && (double)x.Item2 == value + 2.0), "Instrument fundamental value 3 not found");

      //NOTE: We don't check the country fundamentals, just making sure ONLY instrument fundamentals are returned.

    }

    private void generateDataBars(Resolution resolution, int count, string fromDateTime = "")
    {
      DateTime dateTime = fromDateTime != string.Empty ? DateTime.Parse(fromDateTime).ToUniversalTime() : DateTime.Now.ToUniversalTime();
      TimeSpan? dateTimeDelta = null;

      switch (resolution)
      {
        case Resolution.Level1:
          dateTimeDelta = new TimeSpan(0, 0, 1);
          break;
        case Resolution.Minute:
          dateTimeDelta = new TimeSpan(0, 1, 0);
          break;
        case Resolution.Hour:
          dateTimeDelta = new TimeSpan(1, 0, 0);
          break;
        case Resolution.Day:
          dateTimeDelta = new TimeSpan(1, 0, 0, 0);
          break;
        case Resolution.Week:
          dateTimeDelta = new TimeSpan(7, 0, 0, 0);
          break;
        case Resolution.Month:
          dateTimeDelta = new TimeSpan(31, 0, 0, 0);
          break;
      }

      if (resolution != Resolution.Level1)
      {
        List<IBarData> data = new List<IBarData>();

        for (int i = 0; i < count; i++)
        {
          double value = i;
          data.Add(new BarData(resolution, dateTime, value, value * 2.0, value * 3.0, value * 4.0, i * 5));
          dateTime = dateTime.Add(dateTimeDelta!.Value);
        }

        m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, data);
      }
      else
      {
        List<ILevel1Data> data = new List<ILevel1Data>();

        for (int i = 0; i < count; i++)
        {
          double value = i;
          data.Add(new Level1Data(dateTime, value, i * 2, value * 3.0, i * 4, value * 5.0, i * 6));
          dateTime = dateTime.Add(dateTimeDelta!.Value);
        }

        m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, data);
      }
    }

    [TestMethod]
    [DataRow(Resolution.Level1, 500)]
    [DataRow(Resolution.Minute, 500)]
    [DataRow(Resolution.Hour, 500)]
    [DataRow(Resolution.Day, 500)]
    [DataRow(Resolution.Week, 500)]
    [DataRow(Resolution.Month, 500)]
    public void GetDataCount_AnyResolution_Success(Resolution resolution, int expectedCount)
    {
      generateDataBars(resolution, 500);
      Assert.AreEqual(expectedCount, m_database.GetDataCount(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution));
    }

    [TestMethod]
    [DataRow(Resolution.Minute, 500)]
    [DataRow(Resolution.Day, 500)]
    public void GetBarDataCount_NoDateFilter_Success(Resolution resolution, int expectedCount)
    {
      generateDataBars(resolution, 500);
      Assert.AreEqual(expectedCount, m_database.GetDataCount(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution));
    }

    [TestMethod]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T03:30:00Z", 151)]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 122)]
    public void GetBarDataCount_WithDateFilter_Success(Resolution resolution, string generatedFromDate, string fromDate, string toDate, int expectedCount)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime from = DateTime.Parse(fromDate).ToUniversalTime();
      DateTime to = DateTime.Parse(toDate).ToUniversalTime();
      generateDataBars(resolution, 500, generatedFromDate);
      Assert.AreEqual(expectedCount, m_database.GetDataCount(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, from, to));
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    public void GetBarData_ReturnsBarData_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      DataCacheBars barData = new DataCacheBars(10);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5),
                                              dateTime.AddMinutes(6), dateTime.AddMinutes(7), dateTime.AddMinutes(8), dateTime.AddMinutes(9), dateTime.AddMinutes(10) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0, 211.0, 221.0, 231.0, 241.0, 251.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0, 212.0, 222.0, 232.0, 242.0, 252.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0, 213.0, 223.0, 233.0, 243.0, 253.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0, 214.0, 224.0, 234.0, 244.0, 254.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155, 215, 225, 235, 245, 255 };

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      IList<IBarData> dataResult = m_database.GetBarData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, dateTime.AddMinutes(10));
      Assert.AreEqual(barData.Count, dataResult.Count, "GetBarData did not return the correct number of bars.");

      for (int index = 0; index < dataResult.Count; index++)
        Assert.IsTrue(barData.DateTime.Contains(dataResult[index].DateTime), string.Format("Bar data {0} not found.", dataResult[index].DateTime));
    }

    [TestMethod]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T05:00:00Z", 241)]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 122)]
    public void GetBarData_FilterByDate_Success(Resolution resolution, string generatedFromDate, string fromDate, string toDate, int expectedCount)
    {
      DateTime from = DateTime.Parse(fromDate).ToUniversalTime();
      DateTime to = DateTime.Parse(toDate).ToUniversalTime();
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      generateDataBars(resolution, 500, generatedFromDate);
      IList<IBarData> barData = m_database.GetBarData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, from, to);
      Assert.AreEqual(expectedCount, barData.Count, "GetBarData did not return the correct number of bars.");

      for (int i = 0; i < expectedCount; i++)
        Assert.IsTrue(barData[i].DateTime >= from && barData[i].DateTime <= to, $"Bar {i} data returned is outside of the requested date range.");      
    }

    [TestMethod]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", 0, 10, 10, "2023-12-31T00:00:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", 10, 10, 10, "2023-12-31T00:10:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", 20, 10, 10, "2023-12-31T00:20:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", 0, 20, 20, "2023-12-31T00:00:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", 20, 20, 20, "2023-12-31T00:20:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", 40, 20, 20, "2023-12-31T00:40:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", 0, 10, 10, "2023-12-31T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", 10, 10, 10, "2024-01-10T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", 20, 10, 10, "2024-01-20T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", 0, 20, 20, "2023-12-31T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", 20, 20, 20, "2024-01-20T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", 40, 20, 20, "2024-02-09T00:00:00Z")]
    public void GetBarData_FilterByPage_Success(Resolution resolution, string generatedFromDate, int index, int count, int expectedCount, string expectedFromDate)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      generateDataBars(resolution, 500, generatedFromDate);
      IList<IBarData> barData = m_database.GetBarData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, index, count);
      Assert.AreEqual(expectedCount, barData.Count, "GetBarData did not return the correct number of bars.");
      Assert.AreEqual(DateTime.Parse(expectedFromDate).ToUniversalTime(), barData[0].DateTime, "Expected from date was not returned.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T02:00:00Z", 0, 10, 10, "2023-12-31T01:00:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T02:00:00Z", 10, 10, 10, "2023-12-31T01:10:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T02:00:00Z", 20, 10, 10, "2023-12-31T01:20:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T02:00:00Z", 0, 20, 20, "2023-12-31T01:00:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T02:00:00Z", 20, 20, 20, "2023-12-31T01:20:00Z")]
    [DataRow(Resolution.Minute, "2023-12-31T00:00:00Z", "2023-12-31T01:00:00Z", "2023-12-31T02:00:00Z", 40, 20, 20, "2023-12-31T01:40:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 0, 10, 10, "2024-06-01T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 10, 10, 10, "2024-06-11T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 20, 10, 10, "2024-06-21T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 0, 20, 20, "2024-06-01T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 20, 20, 20, "2024-06-21T00:00:00Z")]
    [DataRow(Resolution.Day, "2023-12-31T00:00:00Z", "2024-06-01T00:00:00Z", "2024-09-30T00:00:00Z", 40, 20, 20, "2024-07-11T00:00:00Z")]
    public void GetBarData_FilterByDateAndPage_Success(Resolution resolution, string generatedFromDate, string fromDate, string toDate, int index, int count, int expectedCount, string expectedFromDate)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime from = DateTime.Parse(fromDate).ToUniversalTime();
      DateTime to = DateTime.Parse(toDate).ToUniversalTime();
      generateDataBars(resolution, 500, generatedFromDate);
      IList<IBarData> barData = m_database.GetBarData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, from, to, index, count);
      Assert.AreEqual(expectedCount, barData.Count, "GetBarData did not return the correct number of bars.");
      Assert.AreEqual(DateTime.Parse(expectedFromDate).ToUniversalTime(), barData[0].DateTime, "Expected from date was not returned.");
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    public void GetDataCache_ReturnsBarData_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = (object)IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();  //bar data must always be stored in UTC datetime
      DataCacheBars barData = new DataCacheBars(10);
      barData.DateTime = new List<DateTime> { dateTime.AddMinutes(1), dateTime.AddMinutes(2), dateTime.AddMinutes(3), dateTime.AddMinutes(4), dateTime.AddMinutes(5),
                                              dateTime.AddMinutes(6), dateTime.AddMinutes(7), dateTime.AddMinutes(8), dateTime.AddMinutes(9), dateTime.AddMinutes(10) };
      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0, 211.0, 221.0, 231.0, 241.0, 251.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0, 212.0, 222.0, 232.0, 242.0, 252.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0, 213.0, 223.0, 233.0, 243.0, 253.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0, 214.0, 224.0, 234.0, 244.0, 254.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155, 215, 225, 235, 245, 255 };

      m_database.UpdateData(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);

      DataCache dataResult = m_database.GetDataCache(m_dataProvider1.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, dateTime, dateTime.AddMinutes(10));
      Assert.AreEqual(barData.Count, dataResult.Count, "GetDataCache did not return the correct number of bars.");

      DataCacheBars dataResultDetails = (DataCacheBars)dataResult.Data;
      for (int index = 0; index < barData.Count; index++)
        Assert.IsTrue(dataResultDetails.DateTime.Contains(barData.DateTime[index]), string.Format("Bar data {0} not found.", barData.DateTime[index]));
    }
  }
}
