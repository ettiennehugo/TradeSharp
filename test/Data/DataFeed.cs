using TradeSharp.Common;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Data.Testing
{
  [TestClass]
  public class DataFeed
  {
    //constants


    //enums


    //types


    //attributes
    private Mock<IConfigurationService> m_configuration;
    private ILoggerFactory m_loggerFactory;
    private Dictionary<string, object> m_generalConfiguration;
    private CultureInfo m_cultureEnglish;
    private RegionInfo m_regionInfo;
    private Mock<IDataProvider> m_dataProvider;
    private TradeSharp.Data.SqliteDatabase m_database;
    private Country m_country;
    private TimeZoneInfo m_timeZone;
    private Exchange m_exchange;
    private TradeSharp.Data.Instrument m_instrument;
    private DateTime m_instrumentInceptionDate;
    private DateTime m_fromDateTime;
    private DateTime m_toDateTime;
    private Dictionary<Resolution, DataCacheBars> m_testBarData;
    private Dictionary<Resolution, DataCacheBars> m_testBarDataReversed;
    private DataCacheLevel1 m_level1TestData;
    private DataCacheLevel1 m_level1TestDataReversed;

    //constructors
    public DataFeed()
    {
      m_cultureEnglish = CultureInfo.GetCultureInfo("en-US");
      m_regionInfo = new RegionInfo(m_cultureEnglish.LCID);

      m_dataProvider = new Mock<IDataProvider>().SetupAllProperties();
      m_dataProvider.SetupGet(x => x.Name).Returns("TestDataProvider");

      m_configuration = new Mock<IConfigurationService>(MockBehavior.Strict);
      m_configuration.Setup(x => x.CultureInfo).Returns(m_cultureEnglish);
      m_configuration.Setup(x => x.RegionInfo).Returns(m_regionInfo);
      Type testDataProviderType = typeof(TestDataProvider);
      m_configuration.Setup(x => x.DataProviders).Returns(new Dictionary<string, IPluginConfiguration>() { { "TestDataProvider", new PluginConfiguration(testDataProviderType.AssemblyQualifiedName!, "", new List<IPluginConfigurationProfile>()) } });

      m_generalConfiguration = new Dictionary<string, object>() {
          { IConfigurationService.GeneralConfiguration.TimeZone, (object)IConfigurationService.TimeZone.Local },
          { IConfigurationService.GeneralConfiguration.Database, new DataStoreConfiguration(typeof(TradeSharp.Data.SqliteDatabase).ToString(), "TradeSharpTest.db") }
      };

      m_configuration.Setup(x => x.General).Returns(m_generalConfiguration);

      m_loggerFactory = new LoggerFactory();
      m_database = new TradeSharp.Data.SqliteDatabase(m_configuration.Object, new Logger<TradeSharp.Data.SqliteDatabase>(m_loggerFactory));

      //remove stale data from previous tests - this is to ensure proper test isolation and create the default objects used by the database
      m_database.ClearDatabase();
      m_database.CreateDefaultObjects();

      //create common attributes used for testing
      m_country = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, "TagValue", "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "TestExchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_instrumentInceptionDate = DateTime.Now.ToUniversalTime();
      m_instrument = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "TagValue", InstrumentType.Stock, "TEST", Array.Empty<string>(), "TestInstrument", "TestInstrumentDescription", m_instrumentInceptionDate, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>()); //database layer stores dates in UTC

      //create some test data for the instrument
      m_testBarData = new Dictionary<Resolution, DataCacheBars>();
      m_testBarDataReversed = new Dictionary<Resolution, DataCacheBars>();

      //create required exchange in the database for the instrument in question
      m_database.CreateExchange(m_exchange);
      m_database.CreateInstrument(m_instrument);
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    /// <summary>
    /// Generates some test price data for the instrument and persist the data to the data store. All dates are stored in UTC as expected by the data store.
    /// </summary>
    protected void createTestDataWithPersist(DateTime from, int count)
    {
      m_fromDateTime = from;

      //create level 1 test data
      double price = 0.0;
      long size = 0;
      m_level1TestData = new DataCacheLevel1(0);
      m_level1TestData.Count = count;
      m_level1TestData.DateTime = new List<DateTime>(m_level1TestData.Count); for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.DateTime.Add(m_fromDateTime.AddSeconds(i)); }
      m_level1TestData.Bid = new List<double>(m_level1TestData.Count); price = 100.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Bid.Add(price); price += 1.0; }
      m_level1TestData.BidSize = new List<long>(m_level1TestData.Count); size = 200; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.BidSize.Add(size); size += 1; }
      m_level1TestData.Ask = new List<double>(m_level1TestData.Count); price = 300.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Ask.Add(price); price += 1.0; }
      m_level1TestData.AskSize = new List<long>(m_level1TestData.Count); size = 400; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.AskSize.Add(size); size += 1; }
      m_level1TestData.Last = new List<double>(m_level1TestData.Count); price = 500.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Last.Add(price); price += 1.0; }
      m_level1TestData.LastSize = new List<long>(m_level1TestData.Count); size = 600; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.LastSize.Add(size); size += 1; }

      m_database.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, m_level1TestData);

      //data feed would reverse the data according to date/time so we need to reverse it here to match
      m_level1TestDataReversed = new DataCacheLevel1(0);
      m_level1TestDataReversed.Count = count;
      m_level1TestDataReversed.DateTime = m_level1TestData.DateTime.Reverse().ToArray();
      m_level1TestDataReversed.Bid = m_level1TestData.Bid.Reverse().ToArray();
      m_level1TestDataReversed.BidSize = m_level1TestData.BidSize.Reverse().ToArray();
      m_level1TestDataReversed.Ask = m_level1TestData.Ask.Reverse().ToArray();
      m_level1TestDataReversed.AskSize = m_level1TestData.AskSize.Reverse().ToArray();
      m_level1TestDataReversed.Last = m_level1TestData.Last.Reverse().ToArray();
      m_level1TestDataReversed.LastSize = m_level1TestData.LastSize.Reverse().ToArray();

      //create bar resolution test data
      foreach (Resolution resolution in m_database.SupportedDataResolutions)
      {
        DataCacheBars barData = new DataCacheBars(0);
        barData.Count = count;

        switch (resolution)
        {
          case Resolution.Level1:
            continue;
          case Resolution.Minute:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddMinutes(i));
            break;
          case Resolution.Hour:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddHours(i));
            break;
          case Resolution.Day:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddDays(i));
            break;
          case Resolution.Week:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddDays(i * 7));
            break;
          case Resolution.Month:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddMonths(i));
            break;
        }

        barData.Open = new List<double>(barData.Count); price = 200.0; for (int i = 0; i < barData.Count; i++) { barData.Open.Add(price); price += 1.0; }
        barData.High = new List<double>(barData.Count); price = 400.0; for (int i = 0; i < barData.Count; i++) { barData.High.Add(price); price += 1.0; }
        barData.Low = new List<double>(barData.Count); price = 100.0; for (int i = 0; i < barData.Count; i++) { barData.Low.Add(price); price += 1.0; }
        barData.Close = new List<double>(barData.Count); price = 300.0; for (int i = 0; i < barData.Count; i++) { barData.Close.Add(price); price += 1.0; }
        barData.Volume = new List<long>(barData.Count); size = 500; for (int i = 0; i < barData.Count; i++) { barData.Volume.Add(size); size += 1; }

        m_toDateTime = m_fromDateTime.AddMonths(count); //just use the longest resolution for the to-date time
        m_database.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, barData);
        m_testBarData.Add(resolution, barData);

        //data feed would reverse the data according to date/time so we need to reverse it here to match
        DataCacheBars reversedBarData = new DataCacheBars(0);
        reversedBarData.Count = count;
        reversedBarData.DateTime = barData.DateTime.Reverse().ToArray();
        reversedBarData.Open = barData.Open.Reverse().ToArray();
        reversedBarData.High = barData.High.Reverse().ToArray();
        reversedBarData.Low = barData.Low.Reverse().ToArray();
        reversedBarData.Close = barData.Close.Reverse().ToArray();
        reversedBarData.Volume = barData.Volume.Reverse().ToArray();

        m_testBarDataReversed.Add(resolution, reversedBarData);
      }
    }

    /// <summary>
    /// Generates some test price data for an instrument. No persistance is performed so that unit tests can persist the data as desired (e.g. in parts for testing).
    /// </summary>
    protected void createTestDataNoPersist(DateTime from, int count)
    {
      m_fromDateTime = from;

      //create level 1 test data
      double price = 0.0;
      long size = 0;
      m_level1TestData = new DataCacheLevel1(0);
      m_level1TestData.Count = count;
      m_level1TestData.DateTime = new List<DateTime>(m_level1TestData.Count); for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.DateTime.Add(m_fromDateTime.AddSeconds(i)); }
      m_level1TestData.Bid = new List<double>(m_level1TestData.Count); price = 100.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Bid.Add(price); price += 1.0; }
      m_level1TestData.BidSize = new List<long>(m_level1TestData.Count); size = 200; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.BidSize.Add(size); size += 1; }
      m_level1TestData.Ask = new List<double>(m_level1TestData.Count); price = 300.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Ask.Add(price); price += 1.0; }
      m_level1TestData.AskSize = new List<long>(m_level1TestData.Count); size = 400; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.AskSize.Add(size); size += 1; }
      m_level1TestData.Last = new List<double>(m_level1TestData.Count); price = 500.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Last.Add(price); price += 1.0; }
      m_level1TestData.LastSize = new List<long>(m_level1TestData.Count); size = 600; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.LastSize.Add(size); size += 1; }

      //data feed would reverse the data according to date/time so we need to reverse it here to match
      m_level1TestDataReversed = new DataCacheLevel1(0);
      m_level1TestDataReversed.Count = count;
      m_level1TestDataReversed.DateTime = m_level1TestData.DateTime.Reverse().ToArray();
      m_level1TestDataReversed.Bid = m_level1TestData.Bid.Reverse().ToArray();
      m_level1TestDataReversed.BidSize = m_level1TestData.BidSize.Reverse().ToArray();
      m_level1TestDataReversed.Ask = m_level1TestData.Ask.Reverse().ToArray();
      m_level1TestDataReversed.AskSize = m_level1TestData.AskSize.Reverse().ToArray();
      m_level1TestDataReversed.Last = m_level1TestData.Last.Reverse().ToArray();
      m_level1TestDataReversed.LastSize = m_level1TestData.LastSize.Reverse().ToArray();

      //create bar resolution test data
      foreach (Resolution resolution in m_database.SupportedDataResolutions)
      {
        DataCacheBars barData = new DataCacheBars(0);
        barData.Count = count;

        switch (resolution)
        {
          case Resolution.Level1:
            continue;
          case Resolution.Minute:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddMinutes(i));
            break;
          case Resolution.Hour:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddHours(i));
            break;
          case Resolution.Day:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddDays(i));
            break;
          case Resolution.Week:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddDays(i * 7));
            break;
          case Resolution.Month:
            barData.DateTime = new List<DateTime>(barData.Count); for (int i = 0; i < barData.Count; i++) barData.DateTime.Add(m_fromDateTime.AddMonths(i));
            break;
        }

        barData.Open = new List<double>(barData.Count); price = 200.0; for (int i = 0; i < barData.Count; i++) { barData.Open.Add(price); price += 1.0; }
        barData.High = new List<double>(barData.Count); price = 400.0; for (int i = 0; i < barData.Count; i++) { barData.High.Add(price); price += 1.0; }
        barData.Low = new List<double>(barData.Count); price = 100.0; for (int i = 0; i < barData.Count; i++) { barData.Low.Add(price); price += 1.0; }
        barData.Close = new List<double>(barData.Count); price = 300.0; for (int i = 0; i < barData.Count; i++) { barData.Close.Add(price); price += 1.0; }
        barData.Volume = new List<long>(barData.Count); size = 500; for (int i = 0; i < barData.Count; i++) { barData.Volume.Add(size); size += 1; }

        m_toDateTime = m_fromDateTime.AddMonths(count); //just use the longest resolution for the to-date time
        m_testBarData.Add(resolution, barData);

        //data feed would reverse the data according to date/time so we need to reverse it here to match
        DataCacheBars reversedBarData = new DataCacheBars(0);
        reversedBarData.Count = count;
        reversedBarData.DateTime = barData.DateTime.Reverse().ToArray();
        reversedBarData.Open = barData.Open.Reverse().ToArray();
        reversedBarData.High = barData.High.Reverse().ToArray();
        reversedBarData.Low = barData.Low.Reverse().ToArray();
        reversedBarData.Close = barData.Close.Reverse().ToArray();
        reversedBarData.Volume = barData.Volume.Reverse().ToArray();

        m_testBarDataReversed.Add(resolution, reversedBarData);
      }
    }

    /// <summary>
    /// Merges the data from the barData into a new set of bars up to the given count using the given interval. and returns the expected results.
    /// </summary>
    protected (int, DateTime[], double[], double[], double[], double[], long[]) mergeBarTestData(Resolution resolution, int count, int interval)
    {
      int expectedBarCount = (int)Math.Ceiling((double)count / interval);
      if (resolution == Resolution.Minute && m_fromDateTime.Minute % interval != 0) expectedBarCount++; //add additional bar for the partial bar generated when the fromDateTime does not align by an exact minute boundary

      DataCacheBars originalBarData = m_testBarDataReversed[resolution];
      Assert.IsTrue(count <= originalBarData.Count, "Count should be less than generated set of bars.");

      DateTime currentDateTime = new DateTime(m_fromDateTime.Ticks);
      DateTime[] expectedDateTime = new DateTime[expectedBarCount];
      double[] expectedOpen = new double[expectedBarCount];
      double[] expectedHigh = new double[expectedBarCount];
      double[] expectedLow = new double[expectedBarCount];
      double[] expectedClose = new double[expectedBarCount];
      long[] expectedVolume = new long[expectedBarCount];
      int index = 0;
      int subBarIndex = 0;

      for (int i = 0; i < m_testBarData[resolution].Count; i++)
      {
        if (subBarIndex == 0)
        {
          expectedDateTime[index] = currentDateTime;

          if (i == 0 && resolution == Resolution.Minute && interval > 1)
          {
            subBarIndex = currentDateTime.Minute % interval;
            expectedDateTime[index] = expectedDateTime[index].AddMinutes(-subBarIndex); //align by minute boundary
          }

          expectedOpen[index] = m_testBarData[resolution].Open[i];
          expectedHigh[index] = m_testBarData[resolution].High[i];
          expectedLow[index] = m_testBarData[resolution].Low[i];
          expectedVolume[index] = m_testBarData[resolution].Volume[i];
          //NOTE: We don't have to do anything with the subBarIndex when the from date/time is aligned by a minute boundary.
        }
        else
        {
          expectedHigh[index] = Math.Max(expectedHigh[index], m_testBarData[resolution].High[i]);
          expectedLow[index] = Math.Min(expectedLow[index], m_testBarData[resolution].Low[i]);
          expectedVolume[index] += m_testBarData[resolution].Volume[i];
        }

        expectedClose[index] = m_testBarData[resolution].Close[i];

        subBarIndex++;

        if (subBarIndex == interval)
        {
          subBarIndex = 0;
          index++;
        }

        switch (resolution)
        {
          case Resolution.Minute:
            currentDateTime = currentDateTime.AddMinutes(1);
            break;
          case Resolution.Hour:
            currentDateTime = currentDateTime.AddHours(1);
            break;
          case Resolution.Day:
            currentDateTime = currentDateTime.AddDays(1);
            break;
          case Resolution.Week:
            currentDateTime = currentDateTime.AddDays(7);
            break;
          case Resolution.Month:
            currentDateTime = currentDateTime.AddMonths(1);
            break;
        }
      }

      //reverse the expected data arrays so they are in the same order as the data feed
      expectedDateTime = expectedDateTime.Reverse().ToArray();
      expectedOpen = expectedOpen.Reverse().ToArray();
      expectedHigh = expectedHigh.Reverse().ToArray();
      expectedLow = expectedLow.Reverse().ToArray();
      expectedClose = expectedClose.Reverse().ToArray();
      expectedVolume = expectedVolume.Reverse().ToArray();

      return (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume);
    }

    /// <summary>
    /// Merges the data from the barData into a new set of bars up to the given count using the given interval. and returns the expected results.
    /// </summary>
    protected (int, DateTime[], double[], double[], double[], double[], long[]) mergeL1TestData(int count, int interval)
    {
      Assert.IsTrue(count <= m_level1TestDataReversed.Count, "Count should be less than generated set of bars.");

      int expectedBarCount = (int)Math.Ceiling((double)count / interval);

      DateTime currentDateTime = new DateTime(m_fromDateTime.Ticks);
      DateTime[] expectedDateTime = new DateTime[expectedBarCount];
      double[] expectedOpen = new double[expectedBarCount];
      double[] expectedHigh = new double[expectedBarCount];
      double[] expectedLow = new double[expectedBarCount];
      double[] expectedClose = new double[expectedBarCount];
      long[] expectedVolume = new long[expectedBarCount];
      int index = 0;
      int subBarIndex = 0;

      for (int i = 0; i < m_level1TestData.Count; i++)
      {
        if (subBarIndex == 0)
        {
          expectedDateTime[index] = currentDateTime;
          expectedOpen[index] = m_level1TestData.Last[i];
          expectedHigh[index] = m_level1TestData.Last[i];
          expectedLow[index] = m_level1TestData.Last[i];
          expectedVolume[index] = m_level1TestData.LastSize[i];
        }
        else
        {
          expectedHigh[index] = Math.Max(expectedHigh[index], m_level1TestData.Last[i]);
          expectedLow[index] = Math.Min(expectedLow[index], m_level1TestData.Last[i]);
          expectedVolume[index] += m_level1TestData.LastSize[i];
        }

        expectedClose[index] = m_level1TestData.Last[i];

        subBarIndex++;

        if (subBarIndex == interval)
        {
          subBarIndex = 0;
          index++;
        }

        currentDateTime = currentDateTime.AddSeconds(1);
      }

      //reverse the expected data arrays so they are in the same order as the data feed
      expectedDateTime = expectedDateTime.Reverse().ToArray();
      expectedOpen = expectedOpen.Reverse().ToArray();
      expectedHigh = expectedHigh.Reverse().ToArray();
      expectedLow = expectedLow.Reverse().ToArray();
      expectedClose = expectedClose.Reverse().ToArray();
      expectedVolume = expectedVolume.Reverse().ToArray();

      return (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume);
    }

    [TestMethod]
    [DataRow(Resolution.Minute, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Hour, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Day, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Week, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Month, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Minute, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Hour, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Day, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Week, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Month, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Minute, IConfigurationService.TimeZone.UTC)]
    [DataRow(Resolution.Hour, IConfigurationService.TimeZone.UTC)]
    [DataRow(Resolution.Day, IConfigurationService.TimeZone.UTC)]
    [DataRow(Resolution.Week, IConfigurationService.TimeZone.UTC)]
    [DataRow(Resolution.Month, IConfigurationService.TimeZone.UTC)]
    public void GetDataFeed_BarDataConversionByTimeZone_CheckData(Resolution resolution, IConfigurationService.TimeZone timeZone)
    {
      createTestDataWithPersist(DateTime.Now.ToUniversalTime(), 30);
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = timeZone;
      Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_database, m_dataProvider.Object, m_instrument, resolution, 1, m_fromDateTime, m_toDateTime, ToDateMode.Pinned);
      
      Data.DataCacheBars barData = m_testBarDataReversed[resolution];

      for (int i = 0; i < dataFeed.Count; i++)
      {
        switch (timeZone)
        {
          case IConfigurationService.TimeZone.Local:
            Assert.AreEqual(barData.DateTime[i].ToLocalTime(), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
            break;
          case IConfigurationService.TimeZone.Exchange:
            Assert.AreEqual(barData.DateTime[i].Add(m_exchange.TimeZone.BaseUtcOffset), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
            break;
          case IConfigurationService.TimeZone.UTC:
            Assert.AreEqual(barData.DateTime[i].ToUniversalTime(), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
            break;
        }

        dataFeed.Next();
      }
    }

    [TestMethod]
    [DataRow(Resolution.Level1, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Level1, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Level1, IConfigurationService.TimeZone.UTC)]
    [DataRow(Resolution.Minute, IConfigurationService.TimeZone.Local)]
    [DataRow(Resolution.Minute, IConfigurationService.TimeZone.Exchange)]
    [DataRow(Resolution.Minute, IConfigurationService.TimeZone.UTC)]
    public void GetDataFeed_Level1DateTimeConversionByTimeZone_CheckData(Resolution resolution, IConfigurationService.TimeZone timeZone)
    {
      createTestDataWithPersist(DateTime.Now.ToUniversalTime(), 30);
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = timeZone;
      Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_database, m_dataProvider.Object, m_instrument, resolution, 1, m_fromDateTime, m_toDateTime, ToDateMode.Pinned);

      switch (resolution)
      {
        case Resolution.Level1:
          for (int i = 0; i < dataFeed.Count; i++)
          {
            switch (timeZone)
            {
              case IConfigurationService.TimeZone.Local:
                Assert.AreEqual(m_level1TestDataReversed.DateTime[i].ToLocalTime(), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
                break;
              case IConfigurationService.TimeZone.Exchange:
                Assert.AreEqual(m_level1TestDataReversed.DateTime[i].Add(m_exchange.TimeZone.BaseUtcOffset), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
                break;
              case IConfigurationService.TimeZone.UTC:
                Assert.AreEqual(m_level1TestDataReversed.DateTime[i].ToUniversalTime(), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone  {timeZone.ToString()}  is not correct");
                break;
            }

            dataFeed.Next();
          }
          break;
        case Resolution.Minute:
          DataCacheBars barData = m_testBarDataReversed[resolution];

          for (int i = 0; i < dataFeed.Count; i++)
          {
            switch (timeZone)
            {
              case IConfigurationService.TimeZone.Local:
                Assert.AreEqual(barData.DateTime[i].ToLocalTime(), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
                break;
              case IConfigurationService.TimeZone.Exchange:
                Assert.AreEqual(barData.DateTime[i].Add(m_exchange.TimeZone.BaseUtcOffset), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
                break;
              case IConfigurationService.TimeZone.UTC:
                Assert.AreEqual(barData.DateTime[i].ToUniversalTime(), dataFeed.DateTime[0], $"DateTime at index {i}, {resolution.ToString()} resolution, time zone {timeZone.ToString()} is not correct");
                break;
            }

            dataFeed.Next();
          }
          break;
        default:
          throw new NotImplementedException("No other bar data types needs to be tested beyond minute resolution.");
      }
    }

    [TestMethod]
    [DataRow(Resolution.Minute, 1)]
    [DataRow(Resolution.Minute, 2)]
    [DataRow(Resolution.Minute, 3)]
    [DataRow(Resolution.Minute, 4)]
    [DataRow(Resolution.Minute, 5)]
    [DataRow(Resolution.Minute, 6)]
    [DataRow(Resolution.Minute, 7)]
    [DataRow(Resolution.Minute, 8)]
    [DataRow(Resolution.Minute, 9)]
    [DataRow(Resolution.Minute, 10)]
    [DataRow(Resolution.Minute, 11)]
    [DataRow(Resolution.Minute, 12)]
    [DataRow(Resolution.Minute, 13)]
    [DataRow(Resolution.Minute, 14)]
    [DataRow(Resolution.Minute, 15)]
    [DataRow(Resolution.Minute, 16)]
    [DataRow(Resolution.Minute, 17)]
    [DataRow(Resolution.Minute, 18)]
    [DataRow(Resolution.Minute, 19)]
    [DataRow(Resolution.Minute, 20)]
    [DataRow(Resolution.Minute, 21)]
    [DataRow(Resolution.Minute, 22)]
    [DataRow(Resolution.Minute, 23)]
    [DataRow(Resolution.Minute, 24)]
    [DataRow(Resolution.Minute, 25)]
    [DataRow(Resolution.Minute, 26)]
    [DataRow(Resolution.Minute, 27)]
    [DataRow(Resolution.Minute, 28)]
    [DataRow(Resolution.Minute, 29)]
    [DataRow(Resolution.Minute, 30)]
    [DataRow(Resolution.Minute, 31)]
    [DataRow(Resolution.Minute, 32)]
    [DataRow(Resolution.Minute, 33)]
    [DataRow(Resolution.Minute, 34)]
    [DataRow(Resolution.Minute, 35)]
    [DataRow(Resolution.Minute, 36)]
    [DataRow(Resolution.Minute, 37)]
    [DataRow(Resolution.Minute, 38)]
    [DataRow(Resolution.Minute, 39)]
    [DataRow(Resolution.Minute, 40)]
    [DataRow(Resolution.Minute, 41)]
    [DataRow(Resolution.Minute, 42)]
    [DataRow(Resolution.Minute, 43)]
    [DataRow(Resolution.Minute, 44)]
    [DataRow(Resolution.Minute, 45)]
    [DataRow(Resolution.Minute, 46)]
    [DataRow(Resolution.Minute, 47)]
    [DataRow(Resolution.Minute, 48)]
    [DataRow(Resolution.Minute, 49)]
    [DataRow(Resolution.Minute, 50)]
    [DataRow(Resolution.Minute, 51)]
    [DataRow(Resolution.Minute, 52)]
    [DataRow(Resolution.Minute, 53)]
    [DataRow(Resolution.Minute, 54)]
    [DataRow(Resolution.Minute, 55)]
    [DataRow(Resolution.Minute, 56)]
    [DataRow(Resolution.Minute, 57)]
    [DataRow(Resolution.Minute, 58)]
    [DataRow(Resolution.Minute, 59)]
    [DataRow(Resolution.Hour, 1)]
    [DataRow(Resolution.Hour, 2)]
    [DataRow(Resolution.Hour, 3)]
    [DataRow(Resolution.Hour, 4)]
    [DataRow(Resolution.Hour, 5)]
    [DataRow(Resolution.Hour, 6)]
    [DataRow(Resolution.Hour, 7)]
    [DataRow(Resolution.Hour, 8)]
    [DataRow(Resolution.Hour, 9)]
    [DataRow(Resolution.Hour, 10)]
    [DataRow(Resolution.Hour, 11)]
    [DataRow(Resolution.Hour, 12)]
    [DataRow(Resolution.Day, 1)]
    [DataRow(Resolution.Day, 2)]
    [DataRow(Resolution.Day, 3)]
    [DataRow(Resolution.Day, 4)]
    [DataRow(Resolution.Day, 5)]
    [DataRow(Resolution.Day, 6)]
    [DataRow(Resolution.Day, 7)]
    [DataRow(Resolution.Day, 8)]
    [DataRow(Resolution.Day, 9)]
    [DataRow(Resolution.Day, 10)]
    [DataRow(Resolution.Day, 11)]
    [DataRow(Resolution.Day, 12)]
    [DataRow(Resolution.Day, 13)]
    [DataRow(Resolution.Day, 14)]
    [DataRow(Resolution.Week, 1)]
    [DataRow(Resolution.Week, 2)]
    [DataRow(Resolution.Week, 3)]
    [DataRow(Resolution.Week, 4)]
    [DataRow(Resolution.Week, 5)]
    [DataRow(Resolution.Week, 6)]
    [DataRow(Resolution.Week, 7)]
    [DataRow(Resolution.Week, 8)]
    [DataRow(Resolution.Week, 9)]
    [DataRow(Resolution.Week, 10)]
    [DataRow(Resolution.Week, 11)]
    [DataRow(Resolution.Week, 12)]
    [DataRow(Resolution.Month, 1)]
    [DataRow(Resolution.Month, 2)]
    [DataRow(Resolution.Month, 3)]
    [DataRow(Resolution.Month, 4)]
    [DataRow(Resolution.Month, 5)]
    [DataRow(Resolution.Month, 6)]
    [DataRow(Resolution.Month, 7)]
    [DataRow(Resolution.Month, 8)]
    [DataRow(Resolution.Month, 9)]
    [DataRow(Resolution.Month, 10)]
    [DataRow(Resolution.Month, 11)]
    [DataRow(Resolution.Month, 12)]
    public void GetDataFeed_BarDataConvertByInterval_AlignedByMinute(Resolution resolution, int interval)
    {
      //number of bars created when the from/to time aligns by an exact divisible minute boundary should not result in
      //partial data bars returned for any resolution so we can test them by simply dividing the number of generated bars
      //by the interval
      int generatedBarCount = interval <= 30 ? 30 : interval;
      DateTime fromDateTime = new DateTime(2023, 1, 1, 1, 0, 0);
      createTestDataWithPersist(fromDateTime, generatedBarCount);
      Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_database, m_dataProvider.Object, m_instrument, resolution, interval, m_fromDateTime, m_toDateTime, ToDateMode.Pinned);

      int expectedBarCount;
      DateTime[] expectedDateTime;
      double[] expectedOpen;
      double[] expectedHigh;
      double[] expectedLow;
      double[] expectedClose;
      long[] expectedVolume;

      (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeBarTestData(resolution, generatedBarCount, interval);

      Assert.AreEqual(expectedBarCount, dataFeed.Count, "Number of bars aligned by exact minute boundary not correctly returned");

      for (int i = 0; i < expectedBarCount; i++)
      {
        Assert.AreEqual(expectedDateTime[i], dataFeed.DateTime[0], $"DateTime at index {i} is not correct");
        Assert.AreEqual(expectedOpen[i], dataFeed.Open[0], $"Open at index {i} is not correct");
        Assert.AreEqual(expectedHigh[i], dataFeed.High[0], $"High at index {i} is not correct");
        Assert.AreEqual(expectedLow[i], dataFeed.Low[0], $"Low at index {i} is not correct");
        Assert.AreEqual(expectedClose[i], dataFeed.Close[0], $"Close at index {i} is not correct");
        Assert.AreEqual(expectedVolume[i], dataFeed.Volume[0], $"Volume at index {i} is not correct");
        dataFeed.Next();
      }
    }

    [TestMethod]
    [DataRow(Resolution.Minute, 1)]
    [DataRow(Resolution.Minute, 2)]
    [DataRow(Resolution.Minute, 3)]
    [DataRow(Resolution.Minute, 4)]
    [DataRow(Resolution.Minute, 5)]
    [DataRow(Resolution.Minute, 6)]
    [DataRow(Resolution.Minute, 7)]
    [DataRow(Resolution.Minute, 8)]
    [DataRow(Resolution.Minute, 9)]
    [DataRow(Resolution.Minute, 10)]
    [DataRow(Resolution.Minute, 11)]
    [DataRow(Resolution.Minute, 12)]
    [DataRow(Resolution.Minute, 13)]
    [DataRow(Resolution.Minute, 14)]
    [DataRow(Resolution.Minute, 15)]
    [DataRow(Resolution.Minute, 16)]
    [DataRow(Resolution.Minute, 17)]
    [DataRow(Resolution.Minute, 18)]
    [DataRow(Resolution.Minute, 19)]
    [DataRow(Resolution.Minute, 20)]
    [DataRow(Resolution.Minute, 21)]
    [DataRow(Resolution.Minute, 22)]
    [DataRow(Resolution.Minute, 23)]
    [DataRow(Resolution.Minute, 24)]
    [DataRow(Resolution.Minute, 25)]
    [DataRow(Resolution.Minute, 26)]
    [DataRow(Resolution.Minute, 27)]
    [DataRow(Resolution.Minute, 28)]
    [DataRow(Resolution.Minute, 29)]
    [DataRow(Resolution.Minute, 30)]
    [DataRow(Resolution.Minute, 31)]
    [DataRow(Resolution.Minute, 32)]
    [DataRow(Resolution.Minute, 33)]
    [DataRow(Resolution.Minute, 34)]
    [DataRow(Resolution.Minute, 35)]
    [DataRow(Resolution.Minute, 36)]
    [DataRow(Resolution.Minute, 37)]
    [DataRow(Resolution.Minute, 38)]
    [DataRow(Resolution.Minute, 39)]
    [DataRow(Resolution.Minute, 40)]
    [DataRow(Resolution.Minute, 41)]
    [DataRow(Resolution.Minute, 42)]
    [DataRow(Resolution.Minute, 43)]
    [DataRow(Resolution.Minute, 44)]
    [DataRow(Resolution.Minute, 45)]
    [DataRow(Resolution.Minute, 46)]
    [DataRow(Resolution.Minute, 47)]
    [DataRow(Resolution.Minute, 48)]
    [DataRow(Resolution.Minute, 49)]
    [DataRow(Resolution.Minute, 50)]
    [DataRow(Resolution.Minute, 51)]
    [DataRow(Resolution.Minute, 52)]
    [DataRow(Resolution.Minute, 53)]
    [DataRow(Resolution.Minute, 54)]
    [DataRow(Resolution.Minute, 55)]
    [DataRow(Resolution.Minute, 56)]
    [DataRow(Resolution.Minute, 57)]
    [DataRow(Resolution.Minute, 58)]
    [DataRow(Resolution.Minute, 59)]
    [DataRow(Resolution.Hour, 1)]
    [DataRow(Resolution.Hour, 2)]
    [DataRow(Resolution.Hour, 3)]
    [DataRow(Resolution.Hour, 4)]
    [DataRow(Resolution.Hour, 5)]
    [DataRow(Resolution.Hour, 6)]
    [DataRow(Resolution.Hour, 7)]
    [DataRow(Resolution.Hour, 8)]
    [DataRow(Resolution.Hour, 9)]
    [DataRow(Resolution.Hour, 10)]
    [DataRow(Resolution.Hour, 11)]
    [DataRow(Resolution.Hour, 12)]
    [DataRow(Resolution.Day, 1)]
    [DataRow(Resolution.Day, 2)]
    [DataRow(Resolution.Day, 3)]
    [DataRow(Resolution.Day, 4)]
    [DataRow(Resolution.Day, 5)]
    [DataRow(Resolution.Day, 6)]
    [DataRow(Resolution.Day, 7)]
    [DataRow(Resolution.Day, 8)]
    [DataRow(Resolution.Day, 9)]
    [DataRow(Resolution.Day, 10)]
    [DataRow(Resolution.Day, 11)]
    [DataRow(Resolution.Day, 12)]
    [DataRow(Resolution.Day, 13)]
    [DataRow(Resolution.Day, 14)]
    [DataRow(Resolution.Week, 1)]
    [DataRow(Resolution.Week, 2)]
    [DataRow(Resolution.Week, 3)]
    [DataRow(Resolution.Week, 4)]
    [DataRow(Resolution.Week, 5)]
    [DataRow(Resolution.Week, 6)]
    [DataRow(Resolution.Week, 7)]
    [DataRow(Resolution.Week, 8)]
    [DataRow(Resolution.Week, 9)]
    [DataRow(Resolution.Week, 10)]
    [DataRow(Resolution.Week, 11)]
    [DataRow(Resolution.Week, 12)]
    [DataRow(Resolution.Month, 1)]
    [DataRow(Resolution.Month, 2)]
    [DataRow(Resolution.Month, 3)]
    [DataRow(Resolution.Month, 4)]
    [DataRow(Resolution.Month, 5)]
    [DataRow(Resolution.Month, 6)]
    [DataRow(Resolution.Month, 7)]
    [DataRow(Resolution.Month, 8)]
    [DataRow(Resolution.Month, 9)]
    [DataRow(Resolution.Month, 10)]
    [DataRow(Resolution.Month, 11)]
    [DataRow(Resolution.Month, 12)]
    public void GetDataFeed_BarDataConvertByInterval_MisalignedByMinute(Resolution resolution, int interval)
    {
      //number of bars created when the from/to time do not align by an exact divisible minute boundary should result in
      //partial data bars returned on the minute/hour resolutions so we need to specifically
      int generatedBarCount = interval <= 30 ? 30 : interval;
      DateTime fromDateTime = new DateTime(2023, 1, 1, 1, 3, 0);
      createTestDataWithPersist(fromDateTime, generatedBarCount);
      Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_database, m_dataProvider.Object, m_instrument, resolution, interval, m_fromDateTime, m_toDateTime, ToDateMode.Pinned);

      int expectedBarCount;
      DateTime[] expectedDateTime;
      double[] expectedOpen;
      double[] expectedHigh;
      double[] expectedLow;
      double[] expectedClose;
      long[] expectedVolume;

      (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeBarTestData(resolution, generatedBarCount, interval);

      Assert.AreEqual(expectedBarCount, dataFeed.Count, "Number of bars aligned by exact minute boundary not correctly returned");

      for (int i = 0; i < expectedBarCount; i++)
      {
        Assert.AreEqual(expectedDateTime[i], dataFeed.DateTime[0], $"DateTime at index {i} is not correct");
        Assert.AreEqual(expectedOpen[i], dataFeed.Open[0], $"Open at index {i} is not correct");
        Assert.AreEqual(expectedHigh[i], dataFeed.High[0], $"High at index {i} is not correct");
        Assert.AreEqual(expectedLow[i], dataFeed.Low[0], $"Low at index {i} is not correct");
        Assert.AreEqual(expectedClose[i], dataFeed.Close[0], $"Close at index {i} is not correct");
        Assert.AreEqual(expectedVolume[i], dataFeed.Volume[0], $"Volume at index {i} is not correct");
        dataFeed.Next();
      }
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(10)]
    [DataRow(11)]
    [DataRow(12)]
    [DataRow(13)]
    [DataRow(14)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(18)]
    [DataRow(19)]
    [DataRow(20)]
    public void GetDataFeed_Level1DataConvertByInterval_MergeBars(int interval)
    {
      //number of bars created when the from/to time aligns by an exact divisible minute boundary should not result in
      //partial data bars returned for any resolution so we can test them by simply dividing the number of generated bars
      //by the interval
      int generatedBarCount = interval * 10;
      DateTime fromDateTime = new DateTime(2023, 1, 1, 1, 0, 0);
      createTestDataWithPersist(fromDateTime, generatedBarCount);
      Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_database, m_dataProvider.Object, m_instrument, Resolution.Level1, interval, m_fromDateTime, m_toDateTime, ToDateMode.Pinned);

      int expectedBarCount;
      DateTime[] expectedDateTime;
      double[] expectedOpen;
      double[] expectedHigh;
      double[] expectedLow;
      double[] expectedClose;
      long[] expectedVolume;

      (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeL1TestData(generatedBarCount, interval);

      Assert.AreEqual(expectedBarCount, dataFeed.Count, "Number of bars aligned by exact minute boundary not correctly returned");

      for (int i = 0; i < expectedBarCount; i++)
      {
        Assert.AreEqual(expectedDateTime[i], dataFeed.DateTime[0], $"DateTime at index {i} is not correct");
        Assert.AreEqual(expectedOpen[i], dataFeed.Open[0], $"Open at index {i} is not correct");
        Assert.AreEqual(expectedHigh[i], dataFeed.High[0], $"High at index {i} is not correct");
        Assert.AreEqual(expectedLow[i], dataFeed.Low[0], $"Low at index {i} is not correct");
        Assert.AreEqual(expectedClose[i], dataFeed.Close[0], $"Close at index {i} is not correct");
        Assert.AreEqual(expectedVolume[i], dataFeed.Volume[0], $"Volume at index {i} is not correct");
        dataFeed.Next();
      }
    }



    //TODO: Look how an observer pattern can be implemented for the IDataStoreService and the IDataFeed classes.



    //[TestMethod]
    //[DataRow(Resolution.Minute, 1)]
    //[DataRow(Resolution.Minute, 2)]
    //[DataRow(Resolution.Minute, 3)]
    //[DataRow(Resolution.Minute, 4)]
    //[DataRow(Resolution.Minute, 5)]
    //[DataRow(Resolution.Minute, 6)]
    //[DataRow(Resolution.Minute, 7)]
    //[DataRow(Resolution.Minute, 8)]
    //[DataRow(Resolution.Minute, 9)]
    //[DataRow(Resolution.Minute, 10)]
    //[DataRow(Resolution.Minute, 11)]
    //[DataRow(Resolution.Minute, 12)]
    //[DataRow(Resolution.Minute, 13)]
    //[DataRow(Resolution.Minute, 14)]
    //[DataRow(Resolution.Minute, 15)]
    //[DataRow(Resolution.Minute, 16)]
    //[DataRow(Resolution.Minute, 17)]
    //[DataRow(Resolution.Minute, 18)]
    //[DataRow(Resolution.Minute, 19)]
    //[DataRow(Resolution.Minute, 20)]
    //[DataRow(Resolution.Minute, 21)]
    //[DataRow(Resolution.Minute, 22)]
    //[DataRow(Resolution.Minute, 23)]
    //[DataRow(Resolution.Minute, 24)]
    //[DataRow(Resolution.Minute, 25)]
    //[DataRow(Resolution.Minute, 26)]
    //[DataRow(Resolution.Minute, 27)]
    //[DataRow(Resolution.Minute, 28)]
    //[DataRow(Resolution.Minute, 29)]
    //[DataRow(Resolution.Minute, 30)]
    //[DataRow(Resolution.Minute, 31)]
    //[DataRow(Resolution.Minute, 32)]
    //[DataRow(Resolution.Minute, 33)]
    //[DataRow(Resolution.Minute, 34)]
    //[DataRow(Resolution.Minute, 35)]
    //[DataRow(Resolution.Minute, 36)]
    //[DataRow(Resolution.Minute, 37)]
    //[DataRow(Resolution.Minute, 38)]
    //[DataRow(Resolution.Minute, 39)]
    //[DataRow(Resolution.Minute, 40)]
    //[DataRow(Resolution.Minute, 41)]
    //[DataRow(Resolution.Minute, 42)]
    //[DataRow(Resolution.Minute, 43)]
    //[DataRow(Resolution.Minute, 44)]
    //[DataRow(Resolution.Minute, 45)]
    //[DataRow(Resolution.Minute, 46)]
    //[DataRow(Resolution.Minute, 47)]
    //[DataRow(Resolution.Minute, 48)]
    //[DataRow(Resolution.Minute, 49)]
    //[DataRow(Resolution.Minute, 50)]
    //[DataRow(Resolution.Minute, 51)]
    //[DataRow(Resolution.Minute, 52)]
    //[DataRow(Resolution.Minute, 53)]
    //[DataRow(Resolution.Minute, 54)]
    //[DataRow(Resolution.Minute, 55)]
    //[DataRow(Resolution.Minute, 56)]
    //[DataRow(Resolution.Minute, 57)]
    //[DataRow(Resolution.Minute, 58)]
    //[DataRow(Resolution.Minute, 59)]
    //[DataRow(Resolution.Hour, 1)]
    //[DataRow(Resolution.Hour, 2)]
    //[DataRow(Resolution.Hour, 3)]
    //[DataRow(Resolution.Hour, 4)]
    //[DataRow(Resolution.Hour, 5)]
    //[DataRow(Resolution.Hour, 6)]
    //[DataRow(Resolution.Hour, 7)]
    //[DataRow(Resolution.Hour, 8)]
    //[DataRow(Resolution.Hour, 9)]
    //[DataRow(Resolution.Hour, 10)]
    //[DataRow(Resolution.Hour, 11)]
    //[DataRow(Resolution.Hour, 12)]
    //[DataRow(Resolution.Day, 1)]
    //[DataRow(Resolution.Day, 2)]
    //[DataRow(Resolution.Day, 3)]
    //[DataRow(Resolution.Day, 4)]
    //[DataRow(Resolution.Day, 5)]
    //[DataRow(Resolution.Day, 6)]
    //[DataRow(Resolution.Day, 7)]
    //[DataRow(Resolution.Day, 8)]
    //[DataRow(Resolution.Day, 9)]
    //[DataRow(Resolution.Day, 10)]
    //[DataRow(Resolution.Day, 11)]
    //[DataRow(Resolution.Day, 12)]
    //[DataRow(Resolution.Day, 13)]
    //[DataRow(Resolution.Day, 14)]
    //[DataRow(Resolution.Week, 1)]
    //[DataRow(Resolution.Week, 2)]
    //[DataRow(Resolution.Week, 3)]
    //[DataRow(Resolution.Week, 4)]
    //[DataRow(Resolution.Week, 5)]
    //[DataRow(Resolution.Week, 6)]
    //[DataRow(Resolution.Week, 7)]
    //[DataRow(Resolution.Week, 8)]
    //[DataRow(Resolution.Week, 9)]
    //[DataRow(Resolution.Week, 10)]
    //[DataRow(Resolution.Week, 11)]
    //[DataRow(Resolution.Week, 12)]
    //[DataRow(Resolution.Month, 1)]
    //[DataRow(Resolution.Month, 2)]
    //[DataRow(Resolution.Month, 3)]
    //[DataRow(Resolution.Month, 4)]
    //[DataRow(Resolution.Month, 5)]
    //[DataRow(Resolution.Month, 6)]
    //[DataRow(Resolution.Month, 7)]
    //[DataRow(Resolution.Month, 8)]
    //[DataRow(Resolution.Month, 9)]
    //[DataRow(Resolution.Month, 10)]
    //[DataRow(Resolution.Month, 11)]
    //[DataRow(Resolution.Month, 12)]
    //public void OnChange_SingleBarDataInRangeMerge_Success(Resolution resolution, int interval)
    //{
    //  // - add only HALF the data initially to the DataStore for the given test time interval
    //  // - create a data feed over the full time interval with a pinned time interval
    //  // - check that the DataFeed correctly grows to the full time interval as more data is added to the DataStore
    //  // - Systematically add the remaining data and check that the DataFeed reflects the correct data after each add.

    //  //generate test data without persisting
    //  int generatedBarCount = interval * 20;
    //  DateTime fromDateTime = new DateTime(2023, 1, 1, 1, 0, 0);
    //  createTestDataNoPersist(fromDateTime, generatedBarCount);

    //  int expectedBarCount;
    //  DateTime[] expectedDateTime;
    //  double[] expectedOpen;
    //  double[] expectedHigh;
    //  double[] expectedLow;
    //  double[] expectedClose;
    //  long[] expectedVolume;

    //  (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeBarTestData(resolution, generatedBarCount, interval);

    //  //persist only half of the data
    //  int div2Count = m_testBarData[resolution].Count / 2;
    //  DateTime toDateTime = m_testBarData[resolution].DateTime.Last();

    //  for (int i = 0; i < div2Count; i++)
    //    m_dataStore.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, m_testBarData[resolution].DateTime[i], m_testBarData[resolution].Open[i], m_testBarData[resolution].High[i], m_testBarData[resolution].Low[i], m_testBarData[resolution].Close[i], m_testBarData[resolution].Volume[i]);

    //  //get data feed - it should update automatically as new data are added to the DataManager since ToDateMode.Open
    //  Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_dataStore, m_dataProvider.Object, m_instrument, resolution, interval, m_fromDateTime, m_toDateTime, ToDateMode.Open, PriceDataType.Both);

    //  //check that the first half of the data is reflected correctly
    //  for (int i = 0; i < div2Count; i++)
    //  {
    //    int expectedBarIndex = (div2Count + i) / interval;
    //    if ((i != 0) && (i % interval == 0)) dataFeed.Next();
    //    Assert.AreEqual(expectedDateTime[expectedBarIndex], dataFeed.DateTime[0], $"DateTime at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedOpen[expectedBarIndex], dataFeed.Open[0], $"Open at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedHigh[expectedBarIndex], dataFeed.High[0], $"High at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedLow[expectedBarIndex], dataFeed.Low[0], $"Low at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedClose[expectedBarIndex], dataFeed.Close[0], $"Close at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedVolume[expectedBarIndex], dataFeed.Volume[0], $"Volume at index {expectedBarIndex} is not correct");
    //  }


    //  //TODO: These unit tests will fail as there not observer pattern built yet between the IDataStoreService and the associated DataFeeds.


    //  //inject additional test data into the DataManager and check that DataFeed reflects the new data since it is an observer of the DataManager
    //  dataFeed.Reset();   //new data is added to the beginning of the data feed to we remain on the first bar
    //  for (int i = div2Count; i < m_testBarData[resolution].Count; i += interval)
    //  {
    //    //add the number of bars required for the interval to update the data feed with the next expected bar
    //    for (int subIndex = 0; subIndex < interval; subIndex++)
    //      m_dataStore.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, m_testBarData[resolution].DateTime[i + subIndex], m_testBarData[resolution].Open[i + subIndex], m_testBarData[resolution].High[i + subIndex], m_testBarData[resolution].Low[i + subIndex], m_testBarData[resolution].Close[i + subIndex], m_testBarData[resolution].Volume[i + subIndex]);

    //    //since data feed has an open To-date new bars on the feed should become available and align with the expected bar data
    //    int expectedBarIndex = (m_testBarData[resolution].Count - i - 1) / interval;
    //    Assert.AreEqual(expectedDateTime[expectedBarIndex], dataFeed.DateTime[0], $"DateTime at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedOpen[expectedBarIndex], dataFeed.Open[0], $"Open at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedHigh[expectedBarIndex], dataFeed.High[0], $"High at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedLow[expectedBarIndex], dataFeed.Low[0], $"Low at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedClose[expectedBarIndex], dataFeed.Close[0], $"Close at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedVolume[expectedBarIndex], dataFeed.Volume[0], $"Volume at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //  }
    //}

    //[TestMethod]
    //[DataRow(Resolution.Minute, 1)]
    //[DataRow(Resolution.Minute, 2)]
    //[DataRow(Resolution.Minute, 3)]
    //[DataRow(Resolution.Minute, 4)]
    //[DataRow(Resolution.Minute, 5)]
    //[DataRow(Resolution.Minute, 6)]
    //[DataRow(Resolution.Minute, 7)]
    //[DataRow(Resolution.Minute, 8)]
    //[DataRow(Resolution.Minute, 9)]
    //[DataRow(Resolution.Minute, 10)]
    //[DataRow(Resolution.Minute, 11)]
    //[DataRow(Resolution.Minute, 12)]
    //[DataRow(Resolution.Minute, 13)]
    //[DataRow(Resolution.Minute, 14)]
    //[DataRow(Resolution.Minute, 15)]
    //[DataRow(Resolution.Minute, 16)]
    //[DataRow(Resolution.Minute, 17)]
    //[DataRow(Resolution.Minute, 18)]
    //[DataRow(Resolution.Minute, 19)]
    //[DataRow(Resolution.Minute, 20)]
    //[DataRow(Resolution.Minute, 21)]
    //[DataRow(Resolution.Minute, 22)]
    //[DataRow(Resolution.Minute, 23)]
    //[DataRow(Resolution.Minute, 24)]
    //[DataRow(Resolution.Minute, 25)]
    //[DataRow(Resolution.Minute, 26)]
    //[DataRow(Resolution.Minute, 27)]
    //[DataRow(Resolution.Minute, 28)]
    //[DataRow(Resolution.Minute, 29)]
    //[DataRow(Resolution.Minute, 30)]
    //[DataRow(Resolution.Minute, 31)]
    //[DataRow(Resolution.Minute, 32)]
    //[DataRow(Resolution.Minute, 33)]
    //[DataRow(Resolution.Minute, 34)]
    //[DataRow(Resolution.Minute, 35)]
    //[DataRow(Resolution.Minute, 36)]
    //[DataRow(Resolution.Minute, 37)]
    //[DataRow(Resolution.Minute, 38)]
    //[DataRow(Resolution.Minute, 39)]
    //[DataRow(Resolution.Minute, 40)]
    //[DataRow(Resolution.Minute, 41)]
    //[DataRow(Resolution.Minute, 42)]
    //[DataRow(Resolution.Minute, 43)]
    //[DataRow(Resolution.Minute, 44)]
    //[DataRow(Resolution.Minute, 45)]
    //[DataRow(Resolution.Minute, 46)]
    //[DataRow(Resolution.Minute, 47)]
    //[DataRow(Resolution.Minute, 48)]
    //[DataRow(Resolution.Minute, 49)]
    //[DataRow(Resolution.Minute, 50)]
    //[DataRow(Resolution.Minute, 51)]
    //[DataRow(Resolution.Minute, 52)]
    //[DataRow(Resolution.Minute, 53)]
    //[DataRow(Resolution.Minute, 54)]
    //[DataRow(Resolution.Minute, 55)]
    //[DataRow(Resolution.Minute, 56)]
    //[DataRow(Resolution.Minute, 57)]
    //[DataRow(Resolution.Minute, 58)]
    //[DataRow(Resolution.Minute, 59)]
    //[DataRow(Resolution.Hour, 1)]
    //[DataRow(Resolution.Hour, 2)]
    //[DataRow(Resolution.Hour, 3)]
    //[DataRow(Resolution.Hour, 4)]
    //[DataRow(Resolution.Hour, 5)]
    //[DataRow(Resolution.Hour, 6)]
    //[DataRow(Resolution.Hour, 7)]
    //[DataRow(Resolution.Hour, 8)]
    //[DataRow(Resolution.Hour, 9)]
    //[DataRow(Resolution.Hour, 10)]
    //[DataRow(Resolution.Hour, 11)]
    //[DataRow(Resolution.Hour, 12)]
    //[DataRow(Resolution.Day, 1)]
    //[DataRow(Resolution.Day, 2)]
    //[DataRow(Resolution.Day, 3)]
    //[DataRow(Resolution.Day, 4)]
    //[DataRow(Resolution.Day, 5)]
    //[DataRow(Resolution.Day, 6)]
    //[DataRow(Resolution.Day, 7)]
    //[DataRow(Resolution.Day, 8)]
    //[DataRow(Resolution.Day, 9)]
    //[DataRow(Resolution.Day, 10)]
    //[DataRow(Resolution.Day, 11)]
    //[DataRow(Resolution.Day, 12)]
    //[DataRow(Resolution.Day, 13)]
    //[DataRow(Resolution.Day, 14)]
    //[DataRow(Resolution.Week, 1)]
    //[DataRow(Resolution.Week, 2)]
    //[DataRow(Resolution.Week, 3)]
    //[DataRow(Resolution.Week, 4)]
    //[DataRow(Resolution.Week, 5)]
    //[DataRow(Resolution.Week, 6)]
    //[DataRow(Resolution.Week, 7)]
    //[DataRow(Resolution.Week, 8)]
    //[DataRow(Resolution.Week, 9)]
    //[DataRow(Resolution.Week, 10)]
    //[DataRow(Resolution.Week, 11)]
    //[DataRow(Resolution.Week, 12)]
    //[DataRow(Resolution.Month, 1)]
    //[DataRow(Resolution.Month, 2)]
    //[DataRow(Resolution.Month, 3)]
    //[DataRow(Resolution.Month, 4)]
    //[DataRow(Resolution.Month, 5)]
    //[DataRow(Resolution.Month, 6)]
    //[DataRow(Resolution.Month, 7)]
    //[DataRow(Resolution.Month, 8)]
    //[DataRow(Resolution.Month, 9)]
    //[DataRow(Resolution.Month, 10)]
    //[DataRow(Resolution.Month, 11)]
    //[DataRow(Resolution.Month, 12)]
    //public void OnChange_SingleBarDataInToOutRangeMerge_Success(Resolution resolution, int interval)
    //{
    //  // - add only HALF the data initially to the DataStore for the given test time interval
    //  // - create a data feed around three quarters over the full time interval with a pinned time interval
    //  // - check that the DataFeed reflects the correct data
    //  // - Systematically add the remaining data check the following:
    //  //      - up to the three quarters point, the DataFeed reflects the correct data
    //  //      - after the three quarters point, the DataFeed remains unchanged

    //  //generate test data without persisting
    //  int generatedBarCount = interval * 20;
    //  DateTime fromDateTime = new DateTime(2023, 1, 1, 1, 0, 0);
    //  createTestDataNoPersist(fromDateTime, generatedBarCount);

    //  int expectedBarCount;
    //  DateTime[] expectedDateTime;
    //  double[] expectedOpen;
    //  double[] expectedHigh;
    //  double[] expectedLow;
    //  double[] expectedClose;
    //  long[] expectedVolume;

    //  (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeBarTestData(resolution, generatedBarCount, interval);

    //  //persist only half of the data
    //  int div2Count = m_testBarData[resolution].Count / 2;

    //  for (int i = 0; i < div2Count; i++)
    //    m_dataStore.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, m_testBarData[resolution].DateTime[i], m_testBarData[resolution].Open[i], m_testBarData[resolution].High[i], m_testBarData[resolution].Low[i], m_testBarData[resolution].Close[i], m_testBarData[resolution].Volume[i]);

    //  //get data feed - it should update automatically as new data are added to the DataManager since ToDateMode.Open
    //  int div3over4Count = (m_testBarData[resolution].Count * 3) / 4;
    //  DateTime toDateTime = m_testBarData[resolution].DateTime[div3over4Count];

    //  Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_dataStore, m_dataProvider.Object, m_instrument, resolution, interval, m_fromDateTime, toDateTime, ToDateMode.Pinned, PriceDataType.Both);

    //  //check that the first half of the data is reflected correctly
    //  for (int i = 0; i < div2Count; i++)
    //  {
    //    int expectedBarIndex = (div2Count + i) / interval;
    //    if ((i != 0) && (i % interval == 0)) dataFeed.Next();
    //    Assert.AreEqual(expectedDateTime[expectedBarIndex], dataFeed.DateTime[0], $"DateTime at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedOpen[expectedBarIndex], dataFeed.Open[0], $"Open at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedHigh[expectedBarIndex], dataFeed.High[0], $"High at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedLow[expectedBarIndex], dataFeed.Low[0], $"Low at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedClose[expectedBarIndex], dataFeed.Close[0], $"Close at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedVolume[expectedBarIndex], dataFeed.Volume[0], $"Volume at index {expectedBarIndex} is not correct");
    //  }


    //  //TODO: These unit tests will fail as there not observer pattern built yet between the IDataStoreService and the associated DataFeeds.



    //  //inject additional test data into the DataManager and check that DataFeed reflects the new data since it is an observer of the DataManager
    //  dataFeed.Reset();   //new data is added to the beginning of the data feed to we remain on the first bar
    //  for (int i = div2Count; i < m_testBarData[resolution].Count; i += interval)
    //  {
    //    //add the number of bars required for the interval to update the data feed with the next expected bar
    //    for (int subIndex = 0; subIndex < interval; subIndex++)
    //      m_dataStore.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, m_testBarData[resolution].DateTime[i + subIndex], m_testBarData[resolution].Open[i + subIndex], m_testBarData[resolution].High[i + subIndex], m_testBarData[resolution].Low[i + subIndex], m_testBarData[resolution].Close[i + subIndex], m_testBarData[resolution].Volume[i + subIndex]);

    //    //check that data feed adds new data up to the pinned date
    //    if (i < div3over4Count)
    //    {
    //      //since data feed has an open To-date new bars on the feed should become available and align with the expected bar data
    //      int expectedBarIndex = (m_testBarData[resolution].Count - i - 1) / interval;
    //      Assert.AreEqual(expectedDateTime[expectedBarIndex], dataFeed.DateTime[0], $"DateTime at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //      Assert.AreEqual(expectedOpen[expectedBarIndex], dataFeed.Open[0], $"Open at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //      Assert.AreEqual(expectedHigh[expectedBarIndex], dataFeed.High[0], $"High at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //      Assert.AreEqual(expectedLow[expectedBarIndex], dataFeed.Low[0], $"Low at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //      Assert.AreEqual(expectedClose[expectedBarIndex], dataFeed.Close[0], $"Close at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //      Assert.AreEqual(expectedVolume[expectedBarIndex], dataFeed.Volume[0], $"Volume at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    }
    //    else
    //      Assert.AreEqual(toDateTime, dataFeed.DateTime[0], $"DateTime at index merging test data bar {i} is not correct");
    //  }
    //}

    //[TestMethod]
    //[DataRow(Resolution.Minute, 1)]
    //[DataRow(Resolution.Minute, 2)]
    //[DataRow(Resolution.Minute, 3)]
    //[DataRow(Resolution.Minute, 4)]
    //[DataRow(Resolution.Minute, 5)]
    //[DataRow(Resolution.Minute, 6)]
    //[DataRow(Resolution.Minute, 7)]
    //[DataRow(Resolution.Minute, 8)]
    //[DataRow(Resolution.Minute, 9)]
    //[DataRow(Resolution.Minute, 10)]
    //[DataRow(Resolution.Minute, 11)]
    //[DataRow(Resolution.Minute, 12)]
    //[DataRow(Resolution.Minute, 13)]
    //[DataRow(Resolution.Minute, 14)]
    //[DataRow(Resolution.Minute, 15)]
    //[DataRow(Resolution.Minute, 16)]
    //[DataRow(Resolution.Minute, 17)]
    //[DataRow(Resolution.Minute, 18)]
    //[DataRow(Resolution.Minute, 19)]
    //[DataRow(Resolution.Minute, 20)]
    //[DataRow(Resolution.Minute, 21)]
    //[DataRow(Resolution.Minute, 22)]
    //[DataRow(Resolution.Minute, 23)]
    //[DataRow(Resolution.Minute, 24)]
    //[DataRow(Resolution.Minute, 25)]
    //[DataRow(Resolution.Minute, 26)]
    //[DataRow(Resolution.Minute, 27)]
    //[DataRow(Resolution.Minute, 28)]
    //[DataRow(Resolution.Minute, 29)]
    //[DataRow(Resolution.Minute, 30)]
    //[DataRow(Resolution.Minute, 31)]
    //[DataRow(Resolution.Minute, 32)]
    //[DataRow(Resolution.Minute, 33)]
    //[DataRow(Resolution.Minute, 34)]
    //[DataRow(Resolution.Minute, 35)]
    //[DataRow(Resolution.Minute, 36)]
    //[DataRow(Resolution.Minute, 37)]
    //[DataRow(Resolution.Minute, 38)]
    //[DataRow(Resolution.Minute, 39)]
    //[DataRow(Resolution.Minute, 40)]
    //[DataRow(Resolution.Minute, 41)]
    //[DataRow(Resolution.Minute, 42)]
    //[DataRow(Resolution.Minute, 43)]
    //[DataRow(Resolution.Minute, 44)]
    //[DataRow(Resolution.Minute, 45)]
    //[DataRow(Resolution.Minute, 46)]
    //[DataRow(Resolution.Minute, 47)]
    //[DataRow(Resolution.Minute, 48)]
    //[DataRow(Resolution.Minute, 49)]
    //[DataRow(Resolution.Minute, 50)]
    //[DataRow(Resolution.Minute, 51)]
    //[DataRow(Resolution.Minute, 52)]
    //[DataRow(Resolution.Minute, 53)]
    //[DataRow(Resolution.Minute, 54)]
    //[DataRow(Resolution.Minute, 55)]
    //[DataRow(Resolution.Minute, 56)]
    //[DataRow(Resolution.Minute, 57)]
    //[DataRow(Resolution.Minute, 58)]
    //[DataRow(Resolution.Minute, 59)]
    //[DataRow(Resolution.Hour, 1)]
    //[DataRow(Resolution.Hour, 2)]
    //[DataRow(Resolution.Hour, 3)]
    //[DataRow(Resolution.Hour, 4)]
    //[DataRow(Resolution.Hour, 5)]
    //[DataRow(Resolution.Hour, 6)]
    //[DataRow(Resolution.Hour, 7)]
    //[DataRow(Resolution.Hour, 8)]
    //[DataRow(Resolution.Hour, 9)]
    //[DataRow(Resolution.Hour, 10)]
    //[DataRow(Resolution.Hour, 11)]
    //[DataRow(Resolution.Hour, 12)]
    //[DataRow(Resolution.Day, 1)]
    //[DataRow(Resolution.Day, 2)]
    //[DataRow(Resolution.Day, 3)]
    //[DataRow(Resolution.Day, 4)]
    //[DataRow(Resolution.Day, 5)]
    //[DataRow(Resolution.Day, 6)]
    //[DataRow(Resolution.Day, 7)]
    //[DataRow(Resolution.Day, 8)]
    //[DataRow(Resolution.Day, 9)]
    //[DataRow(Resolution.Day, 10)]
    //[DataRow(Resolution.Day, 11)]
    //[DataRow(Resolution.Day, 12)]
    //[DataRow(Resolution.Day, 13)]
    //[DataRow(Resolution.Day, 14)]
    //[DataRow(Resolution.Week, 1)]
    //[DataRow(Resolution.Week, 2)]
    //[DataRow(Resolution.Week, 3)]
    //[DataRow(Resolution.Week, 4)]
    //[DataRow(Resolution.Week, 5)]
    //[DataRow(Resolution.Week, 6)]
    //[DataRow(Resolution.Week, 7)]
    //[DataRow(Resolution.Week, 8)]
    //[DataRow(Resolution.Week, 9)]
    //[DataRow(Resolution.Week, 10)]
    //[DataRow(Resolution.Week, 11)]
    //[DataRow(Resolution.Week, 12)]
    //[DataRow(Resolution.Month, 1)]
    //[DataRow(Resolution.Month, 2)]
    //[DataRow(Resolution.Month, 3)]
    //[DataRow(Resolution.Month, 4)]
    //[DataRow(Resolution.Month, 5)]
    //[DataRow(Resolution.Month, 6)]
    //[DataRow(Resolution.Month, 7)]
    //[DataRow(Resolution.Month, 8)]
    //[DataRow(Resolution.Month, 9)]
    //[DataRow(Resolution.Month, 10)]
    //[DataRow(Resolution.Month, 11)]
    //[DataRow(Resolution.Month, 12)]
    //public void OnChange_SingleBarDataNewBarMerge_Success(Resolution resolution, int interval)
    //{
    //  // - add only half the data initially to the DataStore for the given test time interval
    //  // - create a data feed over the half time interval with an open time interval so that the data feed will automatically grow as new data is added
    //  // - check that the DataFeed reflects the correct data
    //  // - Systematically add the remaining data and check that the DataFeed correctly grows with the new data and reflects the new data correctly

    //  //generate test data without persisting
    //  int generatedBarCount = interval * 20;
    //  DateTime fromDateTime = new DateTime(2023, 1, 1, 1, 0, 0);
    //  createTestDataNoPersist(fromDateTime, generatedBarCount);

    //  int expectedBarCount;
    //  DateTime[] expectedDateTime;
    //  double[] expectedOpen;
    //  double[] expectedHigh;
    //  double[] expectedLow;
    //  double[] expectedClose;
    //  long[] expectedVolume;

    //  (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeBarTestData(resolution, generatedBarCount, interval);

    //  //persist only half of the data
    //  int div2Count = m_testBarData[resolution].Count / 2;
    //  DateTime toDateTime = m_testBarData[resolution].DateTime[div2Count - 1];

    //  for (int i = 0; i < div2Count; i++)
    //    m_dataStore.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, m_testBarData[resolution].DateTime[i], m_testBarData[resolution].Open[i], m_testBarData[resolution].High[i], m_testBarData[resolution].Low[i], m_testBarData[resolution].Close[i], m_testBarData[resolution].Volume[i]);

    //  //get data feed - it should update automatically as new data are added to the DataManager since ToDateMode.Open
    //  Data.DataFeed dataFeed = new Data.DataFeed(m_configuration.Object, m_dataStore, m_dataProvider.Object, m_instrument, resolution, interval, m_fromDateTime, toDateTime, ToDateMode.Open, PriceDataType.Both);

    //  //check that the first half of the data is reflected correctly
    //  for (int i = 0; i < div2Count; i++)
    //  {
    //    int expectedBarIndex = (div2Count + i) / interval;
    //    if ((i != 0) && (i % interval == 0)) dataFeed.Next();
    //    Assert.AreEqual(expectedDateTime[expectedBarIndex], dataFeed.DateTime[0], $"DateTime at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedOpen[expectedBarIndex], dataFeed.Open[0], $"Open at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedHigh[expectedBarIndex], dataFeed.High[0], $"High at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedLow[expectedBarIndex], dataFeed.Low[0], $"Low at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedClose[expectedBarIndex], dataFeed.Close[0], $"Close at index {expectedBarIndex} is not correct");
    //    Assert.AreEqual(expectedVolume[expectedBarIndex], dataFeed.Volume[0], $"Volume at index {expectedBarIndex} is not correct");
    //  }


    //  //TODO: These unit tests will fail as there not observer pattern built yet between the IDataStoreService and the associated DataFeeds.


    //  //inject additional test data into the DataManager and check that DataFeed reflects the new data since it is an observer of the DataManager
    //  dataFeed.Reset();   //new data is added to the beginning of the data feed to we remain on the first bar
    //  for (int i = div2Count; i < m_testBarData[resolution].Count; i += interval)
    //  {
    //    //add the number of bars required for the interval to update the data feed with the next expected bar
    //    for (int subIndex = 0; subIndex < interval; subIndex++)
    //      m_dataStore.UpdateData(m_dataProvider.Object.Name, m_instrument.Id, m_instrument.Ticker, resolution, m_testBarData[resolution].DateTime[i + subIndex], m_testBarData[resolution].Open[i + subIndex], m_testBarData[resolution].High[i + subIndex], m_testBarData[resolution].Low[i + subIndex], m_testBarData[resolution].Close[i + subIndex], m_testBarData[resolution].Volume[i + subIndex]);

    //    //since data feed has an open To-date new bars on the feed should become available and align with the expected bar data
    //    int expectedBarIndex = (m_testBarData[resolution].Count - i - 1) / interval;
    //    Assert.AreEqual(expectedDateTime[expectedBarIndex], dataFeed.DateTime[0], $"DateTime at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedOpen[expectedBarIndex], dataFeed.Open[0], $"Open at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedHigh[expectedBarIndex], dataFeed.High[0], $"High at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedLow[expectedBarIndex], dataFeed.Low[0], $"Low at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedClose[expectedBarIndex], dataFeed.Close[0], $"Close at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //    Assert.AreEqual(expectedVolume[expectedBarIndex], dataFeed.Volume[0], $"Volume at index {expectedBarIndex} (merging test data bar {i}) is not correct");
    //  }
    //}



  }
}
