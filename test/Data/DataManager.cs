using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.Data.Testing;
using Moq;
using TradeSharp.Common;
using TradeSharp.Data;
using static TradeSharp.Common.IConfigurationService;
using static TradeSharp.Data.IDataStoreService;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Data.Testing
{
  [TestClass]
  public class DataManager
  {

    //constants


    //enums


    //types


    //attributes
    private Mock<IConfigurationService> m_configuration;
    private Dictionary<string, object> m_generalConfiguration;
    private Mock<ILoggerFactory> m_loggerFactory;
    private CultureInfo m_cultureEnglish;
    private CultureInfo m_cultureFrench;
    private CultureInfo m_cultureGerman;
    private RegionInfo m_regionInfo;
    private Data.DataManagerService m_dataManager;
    private Data.SqliteDataStoreService m_dataStore;
    private Country m_country;
    private TimeZoneInfo m_timeZone;
    private Exchange m_exchange;
    private Data.Instrument m_instrument;
    private DateTime m_instrumentInceptionDate;
    private Mock<Common.IObserver<ModelChange>> m_modelChangeObserver;
    private long m_modelChangeObserverCount;
    private Mock<Common.IObserver<FundamentalChange>> m_fundamentalChangeObserver;
    private long m_fundamentalChangeObserverCount;
    private Mock<Common.IObserver<PriceChange>> m_priceChangeObserver;
    private long m_priceChangeObserverCount;
    private DateTime m_fromDateTime;
    private DateTime m_toDateTime;
    private Dictionary<Resolution, IDataStoreService.BarData> m_testBarData;
    private Dictionary<Resolution, IDataStoreService.BarData> m_testBarDataReversed;
    private IDataStoreService.Level1Data m_level1TestData;
    private IDataStoreService.Level1Data m_level1TestDataReversed;

    //constructors
    public DataManager()
    {
      m_cultureEnglish = CultureInfo.GetCultureInfo("en-US");
      m_cultureFrench = CultureInfo.GetCultureInfo("fr-FR");
      m_cultureGerman = CultureInfo.GetCultureInfo("de-DE");
      m_regionInfo = new RegionInfo(m_cultureEnglish.LCID);

      m_loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
      m_loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

      m_configuration = new Mock<IConfigurationService>(MockBehavior.Strict);
      m_configuration.Setup(x => x.CultureInfo).Returns(m_cultureEnglish);
      m_configuration.Setup(x => x.RegionInfo).Returns(m_regionInfo);
      m_configuration.Setup(x => x.CultureFallback).Returns(new List<CultureInfo>(1) { m_cultureEnglish, m_cultureFrench }); //we use m_cultureGerman as the ANY language fallback
      Type testDataProviderType = typeof(TradeSharp.Data.Testing.TestDataProvider);
      m_configuration.Setup(x => x.DataProviders).Returns(new Dictionary<string, string>() { { testDataProviderType.AssemblyQualifiedName!, "TestDataProvider1" } });

      m_generalConfiguration = new Dictionary<string, object>() {
          { IConfigurationService.GeneralConfiguration.TimeZone, (object)IConfigurationService.TimeZone.Local },
          { IConfigurationService.GeneralConfiguration.CultureFallback, new List<CultureInfo>(1) { m_cultureEnglish } },
          { IConfigurationService.GeneralConfiguration.DataStore, new IConfigurationService.DataStoreConfiguration(typeof(TradeSharp.Data.SqliteDataStoreService).ToString(), Path.GetTempPath() + "TradeSharpTest.db") }
      };

      m_configuration.Setup(x => x.General).Returns(m_generalConfiguration);

      m_modelChangeObserver = new Mock<Common.IObserver<ModelChange>>(MockBehavior.Strict);
      m_modelChangeObserver.Setup(x => x.OnChange(It.IsAny<IEnumerable<ModelChange>>())).Callback(() => { m_modelChangeObserverCount++; });
      m_modelChangeObserver.Setup(x => x.GetHashCode()).Returns(-1);
      m_fundamentalChangeObserver = new Mock<Common.IObserver<FundamentalChange>>(MockBehavior.Strict);
      m_fundamentalChangeObserver.Setup(x => x.OnChange(It.IsAny<IEnumerable<FundamentalChange>>())).Callback(() => { m_fundamentalChangeObserverCount++; });
      m_fundamentalChangeObserver.Setup(x => x.GetHashCode()).Returns(-2);
      m_priceChangeObserver = new Mock<Common.IObserver<PriceChange>>(MockBehavior.Strict);
      m_priceChangeObserver.Setup(x => x.OnChange(It.IsAny<IEnumerable<PriceChange>>())).Callback(() => { m_priceChangeObserverCount++; });
      m_priceChangeObserver.Setup(x => x.GetHashCode()).Returns(-3);

      m_dataStore = new TradeSharp.Data.SqliteDataStoreService(m_configuration.Object);

      m_dataManager = new Data.DataManagerService(m_configuration.Object, m_loggerFactory.Object, m_dataStore);
      m_dataManager.Subscribe(m_modelChangeObserver.Object);
      m_dataManager.Subscribe(m_fundamentalChangeObserver.Object);
      m_dataManager.Subscribe(m_priceChangeObserver.Object);

      //remove stale data from previous tests - this is to ensure proper test isolation
      m_dataStore.ClearDatabase();

      //hard refresh the data manager to ensure it is in a clean state since the data manager construtor above would also refresh from the data store
      m_dataManager.Refresh();    

      //create common attributes used for testing
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_country = (Country)m_dataManager.Create("en-US");
      m_exchange = (Exchange)m_dataManager.Create(m_country, "TestExchange", m_timeZone);
      m_instrumentInceptionDate = DateTime.Now.ToUniversalTime();
      m_instrument = (Instrument)m_dataManager.Create(m_exchange, InstrumentType.Stock, "TEST", "TestInstrument", "TestInstrumentDescription", m_instrumentInceptionDate);

      m_modelChangeObserverCount = 0;
      m_fundamentalChangeObserverCount = 0;
      m_priceChangeObserverCount = 0;

      //create some test data for the instrument
      m_testBarData = new Dictionary<Resolution, IDataStoreService.BarData>();
      m_testBarDataReversed = new Dictionary<Resolution, IDataStoreService.BarData>();
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
      bool synthetic = false;
      m_level1TestData = new IDataStoreService.Level1Data(0);
      m_level1TestData.Count = count;
      m_level1TestData.DateTime = new List<DateTime>(m_level1TestData.Count); for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.DateTime.Add(m_fromDateTime.AddSeconds(i)); }
      m_level1TestData.Bid = new List<double>(m_level1TestData.Count); price = 100.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Bid.Add(price); price += 1.0; }
      m_level1TestData.BidSize = new List<long>(m_level1TestData.Count); size = 200; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.BidSize.Add(size); size += 1; }
      m_level1TestData.Ask = new List<double>(m_level1TestData.Count); price = 300.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Ask.Add(price); price += 1.0; }
      m_level1TestData.AskSize = new List<long>(m_level1TestData.Count); size = 400; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.AskSize.Add(size); size += 1; }
      m_level1TestData.Last = new List<double>(m_level1TestData.Count); price = 500.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Last.Add(price); price += 1.0; }
      m_level1TestData.LastSize = new List<long>(m_level1TestData.Count); size = 600; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.LastSize.Add(size); size += 1; }
      m_level1TestData.Synthetic = new List<bool>(m_level1TestData.Count); synthetic = false; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Synthetic.Add(synthetic); synthetic = !synthetic; }

      m_dataManager.Update(m_instrument, m_level1TestData);

      //data feed would reverse the data according to date/time so we need to reverse it here to match
      m_level1TestDataReversed.Count = count;
      m_level1TestDataReversed.DateTime = m_level1TestData.DateTime.Reverse().ToArray();
      m_level1TestDataReversed.Bid = m_level1TestData.Bid.Reverse().ToArray();
      m_level1TestDataReversed.BidSize = m_level1TestData.BidSize.Reverse().ToArray();
      m_level1TestDataReversed.Ask = m_level1TestData.Ask.Reverse().ToArray();
      m_level1TestDataReversed.AskSize = m_level1TestData.AskSize.Reverse().ToArray();
      m_level1TestDataReversed.Last = m_level1TestData.Last.Reverse().ToArray();
      m_level1TestDataReversed.LastSize = m_level1TestData.LastSize.Reverse().ToArray();
      m_level1TestDataReversed.Synthetic = m_level1TestData.Synthetic.Reverse().ToArray();

      //create bar resolution test data
      foreach (Resolution resolution in m_dataStore.SupportedDataResolutions)
      {
        BarData barData = new IDataStoreService.BarData(0);
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
        barData.Synthetic = new List<bool>(barData.Count); synthetic = false; for (int i = 0; i < barData.Count; i++) { barData.Synthetic.Add(synthetic); synthetic = !synthetic; }

        m_toDateTime = m_fromDateTime.AddMonths(count); //just use the longest resolution for the to-date time
        m_dataManager.Update(m_instrument, resolution, barData);

        m_testBarData.Add(resolution, barData);

        //data feed would reverse the data according to date/time so we need to reverse it here to match
        BarData reversedBarData = new IDataStoreService.BarData(0);
        reversedBarData.Count = count;
        reversedBarData.DateTime = barData.DateTime.Reverse().ToArray();
        reversedBarData.Open = barData.Open.Reverse().ToArray();
        reversedBarData.High = barData.High.Reverse().ToArray();
        reversedBarData.Low = barData.Low.Reverse().ToArray();
        reversedBarData.Close = barData.Close.Reverse().ToArray();
        reversedBarData.Volume = barData.Volume.Reverse().ToArray();
        reversedBarData.Synthetic = barData.Synthetic.Reverse().ToArray();

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
      bool synthetic = false;
      m_level1TestData = new IDataStoreService.Level1Data(0);
      m_level1TestData.Count = count;
      m_level1TestData.DateTime = new List<DateTime>(m_level1TestData.Count); for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.DateTime.Add(m_fromDateTime.AddSeconds(i)); }
      m_level1TestData.Bid = new List<double>(m_level1TestData.Count); price = 100.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Bid.Add(price); price += 1.0; }
      m_level1TestData.BidSize = new List<long>(m_level1TestData.Count); size = 200; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.BidSize.Add(size); size += 1; }
      m_level1TestData.Ask = new List<double>(m_level1TestData.Count); price = 300.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Ask.Add(price); price += 1.0; }
      m_level1TestData.AskSize = new List<long>(m_level1TestData.Count); size = 400; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.AskSize.Add(size); size += 1; }
      m_level1TestData.Last = new List<double>(m_level1TestData.Count); price = 500.0; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Last.Add(price); price += 1.0; }
      m_level1TestData.LastSize = new List<long>(m_level1TestData.Count); size = 600; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.LastSize.Add(size); size += 1; }
      m_level1TestData.Synthetic = new List<bool>(m_level1TestData.Count); synthetic = false; for (int i = 0; i < m_level1TestData.Count; i++) { m_level1TestData.Synthetic.Add(synthetic); synthetic = !synthetic; }

      //data feed would reverse the data according to date/time so we need to reverse it here to match
      m_level1TestDataReversed.Count = count;
      m_level1TestDataReversed.DateTime = m_level1TestData.DateTime.Reverse().ToArray();
      m_level1TestDataReversed.Bid = m_level1TestData.Bid.Reverse().ToArray();
      m_level1TestDataReversed.BidSize = m_level1TestData.BidSize.Reverse().ToArray();
      m_level1TestDataReversed.Ask = m_level1TestData.Ask.Reverse().ToArray();
      m_level1TestDataReversed.AskSize = m_level1TestData.AskSize.Reverse().ToArray();
      m_level1TestDataReversed.Last = m_level1TestData.Last.Reverse().ToArray();
      m_level1TestDataReversed.LastSize = m_level1TestData.LastSize.Reverse().ToArray();
      m_level1TestDataReversed.Synthetic = m_level1TestData.Synthetic.Reverse().ToArray();

      //create bar resolution test data
      foreach (Resolution resolution in m_dataStore.SupportedDataResolutions)
      {
        BarData barData = new IDataStoreService.BarData(0);
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
        barData.Synthetic = new List<bool>(barData.Count); synthetic = false; for (int i = 0; i < barData.Count; i++) { barData.Synthetic.Add(synthetic); synthetic = !synthetic; }

        m_toDateTime = m_fromDateTime.AddMonths(count); //just use the longest resolution for the to-date time
        m_testBarData.Add(resolution, barData);

        //data feed would reverse the data according to date/time so we need to reverse it here to match
        BarData reversedBarData = new IDataStoreService.BarData(0);
        reversedBarData.Count = count;
        reversedBarData.DateTime = barData.DateTime.Reverse().ToArray();
        reversedBarData.Open = barData.Open.Reverse().ToArray();
        reversedBarData.High = barData.High.Reverse().ToArray();
        reversedBarData.Low = barData.Low.Reverse().ToArray();
        reversedBarData.Close = barData.Close.Reverse().ToArray();
        reversedBarData.Volume = barData.Volume.Reverse().ToArray();
        reversedBarData.Synthetic = barData.Synthetic.Reverse().ToArray();

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

      BarData originalBarData = m_testBarDataReversed[resolution];
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
            currentDateTime = m_fromDateTime.AddMinutes(index);
            break;
          case Resolution.Hour:
            currentDateTime = m_fromDateTime.AddHours(index);
            break;
          case Resolution.Day:
            currentDateTime = m_fromDateTime.AddDays(index);
            break;
          case Resolution.Week:
            currentDateTime = m_fromDateTime.AddDays(index * 7);
            break;
          case Resolution.Month:
            currentDateTime = m_fromDateTime.AddMonths(index);
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


    [TestMethod]
    public void Subscribe_ModelChange_Success()
    {
      Assert.AreEqual(1, m_dataManager.ModelChangeObservers.Count(), "Model change observers are not correct");
    }

    [TestMethod]
    public void Unsubscribe_ModelChange_Success()
    {
      m_dataManager.Unsubscribe(m_modelChangeObserver.Object);
      Assert.AreEqual(0, m_dataManager.ModelChangeObservers.Count(), "Model change observers are not correct");
      m_modelChangeObserverCount = 0;

      m_dataManager.Update(m_instrument, "New description");
      Assert.AreEqual(0, m_modelChangeObserverCount, "Model change observer not unsubscribed");
    }

    [TestMethod]
    public void Subscribe_FundamentalChange_Success()
    {
      Assert.AreEqual(1, m_dataManager.FundamentalChangeObservers.Count(), "Fundamental change observers are not correct");
    }

    [TestMethod]
    public void Unsubscribe_FundamentalChange_Success()
    {
      m_dataManager.Unsubscribe(m_fundamentalChangeObserver.Object);
      Assert.AreEqual(0, m_dataManager.FundamentalChangeObservers.Count(), "Fundamental change observers are not correct");
      m_fundamentalChangeObserverCount = 0;

      Fundamental fundamental = (Fundamental)m_dataManager.Create("Revenue", "Revenue", FundamentalCategory.Instrument, FundamentalReleaseInterval.Quarterly);
      InstrumentFundamental instrumentFundamental = (InstrumentFundamental)m_dataManager.Create(fundamental, m_instrument);
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 2000.0;
      m_dataManager.Update(instrumentFundamental, dateTime, value);

      Assert.AreEqual(0, m_fundamentalChangeObserverCount, "Fundamental change observer not unsubscribed");
    }

    [TestMethod]
    public void Subscribe_PriceChange_Success()
    {
      Assert.AreEqual(1, m_dataManager.PriceChangeObservers.Count(), "Price change observers are not correct");
    }

    [TestMethod]
    public void Unsubscribe_PriceChange_Success()
    {
      m_priceChangeObserver = new Mock<Common.IObserver<PriceChange>>(MockBehavior.Strict);
      m_priceChangeObserver.Setup(x => x.OnChange(It.IsAny<IEnumerable<PriceChange>>())).Callback(() => { m_priceChangeObserverCount++; });
      m_priceChangeObserver.Setup(x => x.GetHashCode()).Returns(-3);

      m_dataManager.Unsubscribe(m_priceChangeObserver.Object);
      Assert.AreEqual(0, m_dataManager.PriceChangeObservers.Count(), "Price change observers are not correct");
      m_priceChangeObserverCount = 0;

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      m_dataManager.Update(m_instrument, Resolution.Minute, dateTime, 100.0, 200.0, 300.0, 400.0, 500, false);

      Assert.AreEqual(0, m_priceChangeObserverCount, "Price change observer not unsubscribed");
    }

    [TestMethod]
    public void GetText_NoTextFoundDefault_Success()
    {
      Guid id = Guid.NewGuid();
      string textValue = m_dataStore.GetText(id);
      Assert.AreEqual(textValue, TradeSharp.Common.Resources.NoTextAvailable, "GetText did not return the default \"No text available\" text.");
    }

    [TestMethod]
    public void GetTexts_ReturnAllPersistedData_Success()
    {
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));
        
      m_dataStore.UpdateText(m_exchange.NameTextId, m_cultureFrench.ThreeLetterISOLanguageName, "Échange d'essai");
      m_dataStore.UpdateText(m_exchange.NameTextId, m_cultureGerman.ThreeLetterISOLanguageName, "Testaustausch");
        
      m_dataStore.UpdateText(m_instrument.NameTextId, m_cultureFrench.ThreeLetterISOLanguageName, "Appareil d'essai");
      m_dataStore.UpdateText(m_instrument.NameTextId, m_cultureGerman.ThreeLetterISOLanguageName, "Prüfgerät");
      m_dataStore.UpdateText(m_instrument.DescriptionTextId, m_cultureFrench.ThreeLetterISOLanguageName, "Description de l'instrument d'essai");
      m_dataStore.UpdateText(m_instrument.DescriptionTextId, m_cultureGerman.ThreeLetterISOLanguageName, "Beschreibung des Prüfgeräts");


      var texts = m_dataStore.GetTexts();

      Assert.AreEqual(9, texts.Count, "Returned text count is not correct.");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "eng" && x.Value == "TestExchange").Single(), "TestExchange English text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "eng" && x.Value == "TestInstrument").Single(), "TestInstrument English text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "eng" && x.Value == "TestInstrumentDescription").Single(), "TestInstrumentDescription English text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "fra" && x.Value == "Échange d'essai").Single(), "TestExchange French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "fra" && x.Value == "Appareil d'essai").Single(), "TestInstrument French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "fra" && x.Value == "Description de l'instrument d'essai").Single(), "TestInstrumentDescription French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "deu" && x.Value == "Testaustausch").Single(), "TestExchange German text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "deu" && x.Value == "Prüfgerät").Single(), "TestInstrument German text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "deu" && x.Value == "Beschreibung des Prüfgeräts").Single(), "TestInstrumentDescription German text not found");
    }

    [TestMethod]
    public void GetTexts_ReturnSpecificPersistedData_Success()
    {
      m_dataStore.CreateExchange(new IDataStoreService.Exchange(m_exchange.Id, m_exchange.Country.Id, m_exchange.NameTextId, m_exchange.Name, m_exchange.TimeZone));
      m_dataStore.CreateInstrument(new IDataStoreService.Instrument(m_instrument.Id, m_instrument.Type, m_instrument.Ticker, m_instrument.NameTextId, m_instrument.Name, m_instrument.DescriptionTextId, m_instrument.Description, m_instrument.InceptionDate, new List<Guid>(), m_instrument.PrimaryExchange.Id, new List<Guid>()));
        
      m_dataStore.UpdateText(m_exchange.NameTextId, m_cultureFrench.ThreeLetterISOLanguageName, "Échange d'essai");
      m_dataStore.UpdateText(m_exchange.NameTextId, m_cultureGerman.ThreeLetterISOLanguageName, "Testaustausch");
        
      m_dataStore.UpdateText(m_instrument.NameTextId, m_cultureFrench.ThreeLetterISOLanguageName, "Appareil d'essai");
      m_dataStore.UpdateText(m_instrument.NameTextId, m_cultureGerman.ThreeLetterISOLanguageName, "Prüfgerät");
      m_dataStore.UpdateText(m_instrument.DescriptionTextId, m_cultureFrench.ThreeLetterISOLanguageName, "Description de l'instrument d'essai");
      m_dataStore.UpdateText(m_instrument.DescriptionTextId, m_cultureGerman.ThreeLetterISOLanguageName, "Beschreibung des Prüfgeräts");

      var texts = m_dataStore.GetTexts(m_cultureEnglish.ThreeLetterISOLanguageName);

      Assert.AreEqual(3, texts.Count, "Returned English text count is not correct.");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "eng" && x.Value == "TestExchange").Single(), "TestExchange French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "eng" && x.Value == "TestInstrument").Single(), "TestInstrument French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "eng" && x.Value == "TestInstrumentDescription").Single(), "TestInstrumentDescription French text not found");

      texts = m_dataStore.GetTexts(m_cultureFrench.ThreeLetterISOLanguageName);

      Assert.AreEqual(3, texts.Count, "Returned French text count is not correct.");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "fra" && x.Value == "Échange d'essai").Single(), "TestExchange French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "fra" && x.Value == "Appareil d'essai").Single(), "TestInstrument French text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "fra" && x.Value == "Description de l'instrument d'essai").Single(), "TestInstrumentDescription French text not found");

      texts = m_dataStore.GetTexts(m_cultureGerman.ThreeLetterISOLanguageName);

      Assert.AreEqual(3, texts.Count, "Returned German text count is not correct.");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "deu" && x.Value == "Testaustausch").Single(), "TestExchange German text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "deu" && x.Value == "Prüfgerät").Single(), "TestInstrument German text not found");
      Assert.IsNotNull(texts.Where(x => x.IsoLang == "deu" && x.Value == "Beschreibung des Prüfgeräts").Single(), "TestInstrumentDescription German text not found");
    }

    [TestMethod]
    public void Create_CountryPersistData_Success()
    {
      Country franceCreated = (Country)m_dataManager.Create("FRA");

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(2, m_dataManager.Countries.Count, "Country count is not correct");

      Country usa = (Country)m_dataManager.Countries.First(x => x.RegionCode == "USA");
      Assert.AreEqual(m_country, usa, "DEEP-COPY-ERROR: USA country instance and returned value by data manager is not correct");
      Country france = (Country)m_dataManager.Countries.First(x => x.RegionCode == "FRA");
      Assert.AreEqual(franceCreated, france, "DEEP-COPY-ERROR: France country instance and returned value by data manager is not correct");

      Assert.AreNotEqual(Guid.Empty, usa.Id, "Country id is not set for USA");
      Assert.AreEqual(m_dataManager, usa.DataManager, "Country data manager object not set for USA");
      Assert.AreEqual("United States", usa.Name, "Country name not set for USA");
      Assert.AreEqual("$", usa.CurrencySymbol, "Country currency symbol not set for USA");
      Assert.AreEqual("USD", usa.Currency, "Country currency not set for USA");
      Assert.AreEqual(Guid.Empty, usa.NameTextId, "Country NameTextId allocated for USA, name is retrieved from the region information");

      Assert.AreNotEqual(Guid.Empty, france.Id, "Country id is not set for France");
      Assert.AreEqual(m_dataManager, france.DataManager, "Country data manager object not set for France");
      Assert.AreEqual("France", france.Name, "Country name not set for France");
      Assert.AreEqual("€", france.CurrencySymbol, "Country currency symbol not set for France");
      Assert.AreEqual("EUR", france.Currency, "Country currency not set for France");
      Assert.AreEqual(Guid.Empty, france.NameTextId, "Country NameTextId allocated for France, name is retrieved from the region information");

      Guid usaId = usa.Id;
      Guid franceId = france.Id;

      m_dataManager.Refresh();

      Assert.AreEqual(2, m_dataManager.Countries.Count, "POST-REFRESH: Country count is not correct");

      usa = (Country)m_dataManager.Countries.First(x => x.Id == usaId);
      france = (Country)m_dataManager.Countries.First(x => x.Id == franceId);

      Assert.AreEqual(usaId, usa.Id, "POST-REFRESH: Country id is not correct for USA");
      Assert.AreEqual(m_dataManager, usa.DataManager, "POST-REFRESH: Country data manager object not set for USA");
      Assert.AreEqual("United States", usa.Name, "POST-REFRESH: Country name not set for USA");
      Assert.AreEqual("$", usa.CurrencySymbol, "POST-REFRESH: Country currency symbol not set for USA");
      Assert.AreEqual("USD", usa.Currency, "POST-REFRESH: Country currency not set for USA");
      Assert.AreEqual(Guid.Empty, usa.NameTextId, "POST-REFRESH: Country NameTextId allocated for USA, name is retrieved from the region information");

      Assert.AreEqual(franceId, france.Id, "POST-REFRESH: Country id is not correct for France");
      Assert.AreEqual(m_dataManager, france.DataManager, "POST-REFRESH: Country data manager object not set for France");
      Assert.AreEqual("France", france.Name, "POST-REFRESH: Country name not set for France");
      Assert.AreEqual("€", france.CurrencySymbol, "POST-REFRESH: Country currency symbol not set for France");
      Assert.AreEqual("EUR", france.Currency, "POST-REFRESH: Country currency not set for France");
      Assert.AreEqual(Guid.Empty, france.NameTextId, "POST-REFRESH: Country NameTextId allocated for France, name is retrieved from the region information");
    }

    [TestMethod]
    public void Create_CountryHolidayDayOfMonthPersistData_Success()
    {
      Holiday holiday = (Holiday)m_dataManager.Create(m_country, "New Years Day", Months.January, 1, MoveWeekendHoliday.PreviousBusinessDay);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_country.Holidays.FirstOrDefault(x => x == holiday), "Country holiday not added to country");

      Assert.AreNotEqual(Guid.Empty, holiday.Id, "Holiday id is not set");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "Holiday data manager not set");
      Assert.AreEqual(m_country, holiday.Country, "Holiday country not set correctly");
      Assert.AreEqual("New Years Day", holiday.Name, "Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfMonth, holiday.Type, "Holiday type is not set correctly");
      Assert.AreEqual(Months.January, holiday.Month, "Holiday month is not set correctly");
      Assert.AreEqual(1, holiday.DayOfMonth, "Holiday day of month is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.PreviousBusinessDay, holiday.MoveWeekendHoliday, "Holiday move weekend holiday is not set correctly");

      Guid countryId = m_country.Id;
      Guid id = holiday.Id;

      m_dataManager.Refresh();

      Assert.IsNotNull(m_country.Holidays.FirstOrDefault(x => x.Id == holiday.Id), "POST-REFRESH: Country holiday not added to country");

      Country country = (Country)m_dataManager.Countries.First(x => x.Id == countryId);
      holiday = (Holiday)country.Holidays.ElementAt(0);

      Assert.AreEqual(id, holiday.Id, "POST-REFRESH: Holiday id is not correct");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "POST-REFRESH: Holiday data manager not set");
      Assert.AreEqual(country, holiday.Country, "POST-REFRESH: Holiday country not set correctly");
      Assert.AreEqual("New Years Day", holiday.Name, "POST-REFRESH: Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "POST-REFRESH: Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfMonth, holiday.Type, "POST-REFRESH: Holiday type is not set correctly");
      Assert.AreEqual(Months.January, holiday.Month, "POST-REFRESH: Holiday month is not set correctly");
      Assert.AreEqual(1, holiday.DayOfMonth, "POST-REFRESH: Holiday day of month is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.PreviousBusinessDay, holiday.MoveWeekendHoliday, "POST-REFRESH: Holiday move weekend holiday is not set correctly");
    }

    [TestMethod]
    public void Create_CountryHolidayDayOfWeekPersistData_Success()
    {
      Holiday holiday = (Holiday)m_dataManager.Create(m_country, "Martin Luther King Jr Day", Months.January, DayOfWeek.Monday, WeekOfMonth.Third, MoveWeekendHoliday.NextBusinessDay);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(1, m_country.Holidays.Count, "Country holiday not added to country");

      Assert.AreNotEqual(Guid.Empty, holiday.Id, "Holiday id is not set");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "Holiday data manager not set");
      Assert.AreEqual(m_country, holiday.Country, "Holiday country not set correctly");
      Assert.AreEqual("Martin Luther King Jr Day", holiday.Name, "Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfWeek, holiday.Type, "Holiday type is not set correctly");
      Assert.AreEqual(Months.January, holiday.Month, "Holiday month is not set correctly");
      Assert.AreEqual(DayOfWeek.Monday, holiday.DayOfWeek, "Holiday day of week is not set correctly");
      Assert.AreEqual(WeekOfMonth.Third, holiday.WeekOfMonth, "Holiday day of week is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.NextBusinessDay, holiday.MoveWeekendHoliday, "Holiday move weekend holiday is not set correctly");

      Guid countryId = m_country.Id;
      Guid id = holiday.Id;

      m_dataManager.Refresh();

      Assert.IsNotNull(m_country.Holidays.FirstOrDefault(x => x.Id == holiday.Id), "POST-REFRESH: Country holiday not added to country");

      Country country = (Country)m_dataManager.Countries.First(x => x.Id == countryId);
      holiday = (Holiday)country.Holidays.ElementAt(0);

      Assert.AreEqual(id, holiday.Id, "POST-REFRESH: Holiday id is not correct");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "POST-REFRESH: Holiday data manager not set");
      Assert.AreEqual(country, holiday.Country, "POST-REFRESH: Holiday country not set correctly");
      Assert.AreEqual("Martin Luther King Jr Day", holiday.Name, "POST-REFRESH: Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "POST-REFRESH: Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfWeek, holiday.Type, "POST-REFRESH: Holiday type is not set correctly");
      Assert.AreEqual(Months.January, holiday.Month, "POST-REFRESH: Holiday month is not set correctly");
      Assert.AreEqual(DayOfWeek.Monday, holiday.DayOfWeek, "POST-REFRESH: Holiday day of week is not set correctly");
      Assert.AreEqual(WeekOfMonth.Third, holiday.WeekOfMonth, "POST-REFRESH: Holiday day of week is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.NextBusinessDay, holiday.MoveWeekendHoliday, "POST-REFRESH: Holiday move weekend holiday is not set correctly");
    }

    [TestMethod]
    public void Create_ExchangePersistData_Success()
    {
      Assert.IsNotNull(m_country.Exchanges.FirstOrDefault(x => x == m_exchange), "Country exchanges not added to country");

      Assert.AreNotEqual(Guid.Empty, m_exchange.Id, "Exchange id is not set");
      Assert.AreEqual(m_dataManager, m_exchange.DataManager, "Exchange data manager not set");
      Assert.AreEqual(m_country, m_exchange.Country, "Holiday country not set correctly");
      Assert.AreEqual("TestExchange", m_exchange.Name, "Exchange name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, m_exchange.NameTextId, "Exchange name text id is not set");
      Assert.AreEqual(7, m_exchange.Sessions.Count, "Exchange sessions not setup");
      Assert.AreEqual(0, m_exchange.Holidays.Count, "Exchange holidays are not correct");
      Assert.AreEqual(1, m_exchange.Instruments.Count, "Exchange instrument count not correct");
    }

    [TestMethod]
    public void Create_ExchangeHolidayDayOfMonthPersistData_Success()
    {
      ExchangeHoliday holiday = (ExchangeHoliday)m_dataManager.Create(m_exchange, "Exchange Day of Month", Months.December, 26, MoveWeekendHoliday.PreviousBusinessDay);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_exchange.Holidays.FirstOrDefault(x => x == holiday), "Exchange holiday not added to exchange");

      Assert.AreNotEqual(Guid.Empty, holiday.Id, "Holiday id is not set");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "Holiday data manager not set");
      Assert.AreEqual(m_country, holiday.Country, "Holiday country not set correctly");
      Assert.AreEqual(m_exchange, holiday.Exchange, "Holiday exchange not set correctly");
      Assert.AreEqual("Exchange Day of Month", holiday.Name, "Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfMonth, holiday.Type, "Holiday type is not set correctly");
      Assert.AreEqual(Months.December, holiday.Month, "Holiday month is not set correctly");
      Assert.AreEqual(26, holiday.DayOfMonth, "Holiday day of month is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.PreviousBusinessDay, holiday.MoveWeekendHoliday, "Holiday move weekend holiday is not set correctly");

      Guid exchangeId = m_exchange.Id;
      Guid id = holiday.Id;

      m_dataManager.Refresh();

      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == exchangeId);
      holiday = (ExchangeHoliday)exchange.Holidays.ElementAt(0);

      Assert.AreEqual(id, holiday.Id, "POST-REFRESH: Holiday id is not correct");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "POST-REFRESH: Holiday data manager not set");
      Assert.AreEqual(exchange.Country, holiday.Country, "POST-REFRESH: Holiday country not set correctly");
      Assert.AreEqual(exchange, holiday.Exchange, "POST-REFRESH: Holiday exchange not set correctly");
      Assert.AreEqual("Exchange Day of Month", holiday.Name, "POST-REFRESH: Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "POST-REFRESH: Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfMonth, holiday.Type, "POST-REFRESH: Holiday type is not set correctly");
      Assert.AreEqual(Months.December, holiday.Month, "POST-REFRESH: Holiday month is not set correctly");
      Assert.AreEqual(26, holiday.DayOfMonth, "POST-REFRESH: Holiday day of month is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.PreviousBusinessDay, holiday.MoveWeekendHoliday, "POST-REFRESH: Holiday move weekend holiday is not set correctly");
    }

    [TestMethod]
    public void Create_ExchangeHolidayDayOfWeekPersistData_Success()
    {
      ExchangeHoliday holiday = (ExchangeHoliday)m_dataManager.Create(m_exchange, "Exchange Day of Week", Months.December, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.PreviousBusinessDay);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_exchange.Holidays.FirstOrDefault(x => x.Id == holiday.Id), "Exchange holiday not added to exchange");

      Assert.AreNotEqual(Guid.Empty, holiday.Id, "Holiday id is not set");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "Holiday data manager not set");
      Assert.AreEqual(m_country, holiday.Country, "Holiday country not set correctly");
      Assert.AreEqual(m_exchange, holiday.Exchange, "Holiday exchange not set correctly");
      Assert.AreEqual("Exchange Day of Week", holiday.Name, "Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfWeek, holiday.Type, "Holiday type is not set correctly");
      Assert.AreEqual(Months.December, holiday.Month, "Holiday month is not set correctly");
      Assert.AreEqual(DayOfWeek.Monday, holiday.DayOfWeek, "Holiday day of week is not set correctly");
      Assert.AreEqual(WeekOfMonth.Second, holiday.WeekOfMonth, "Holiday week of month is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.PreviousBusinessDay, holiday.MoveWeekendHoliday, "Holiday move weekend holiday is not set correctly");

      Guid countryId = m_country.Id;
      Guid exchangeId = m_exchange.Id;
      Guid id = holiday.Id;

      m_dataManager.Refresh();

      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == exchangeId);
      holiday = (ExchangeHoliday)exchange.Holidays.ElementAt(0);

      Assert.AreEqual(id, holiday.Id, "POST-REFRESH: Holiday id is not correct");
      Assert.AreEqual(m_dataManager, holiday.DataManager, "POST-REFRESH: Holiday data manager not set");
      Assert.AreEqual(exchange.Country, holiday.Country, "POST-REFRESH: Holiday country not set correctly");
      Assert.AreEqual(exchange, holiday.Exchange, "POST-REFRESH: Holiday exchange not set correctly");
      Assert.AreEqual("Exchange Day of Week", holiday.Name, "POST-REFRESH: Holiday name is not set correctly");
      Assert.AreNotEqual(Guid.Empty, holiday.NameTextId, "POST-REFRESH: Holiday name text-id is not set correctly");
      Assert.AreEqual(HolidayType.DayOfWeek, holiday.Type, "POST-REFRESH: Holiday type is not set correctly");
      Assert.AreEqual(DayOfWeek.Monday, holiday.DayOfWeek, "Holiday day of week is not set correctly");
      Assert.AreEqual(WeekOfMonth.Second, holiday.WeekOfMonth, "Holiday week of month is not set correctly");
      Assert.AreEqual(MoveWeekendHoliday.PreviousBusinessDay, holiday.MoveWeekendHoliday, "POST-REFRESH: Holiday move weekend holiday is not set correctly");
    }

    [TestMethod]
    public void Create_HolidaysDifferentiateCountryAndExchangeHolidays_Success()
    {
      Holiday countryHolidayDayOfMonth = (Holiday)m_dataManager.Create(m_country, "New Years Day", Months.January, 1, MoveWeekendHoliday.PreviousBusinessDay);
      Holiday countryHolidayDayOfWeek = (Holiday)m_dataManager.Create(m_country, "Martin Luther King Jr Day", Months.January, DayOfWeek.Monday, WeekOfMonth.Third, MoveWeekendHoliday.NextBusinessDay);
      ExchangeHoliday exchangeHolidayDayOfMonth = (ExchangeHoliday)m_dataManager.Create(m_exchange, "Exchange Day of Month", Months.December, 26, MoveWeekendHoliday.PreviousBusinessDay);
      ExchangeHoliday exchangeHolidayDayOfWeek = (ExchangeHoliday)m_dataManager.Create(m_exchange, "Exchange Day of Week", Months.December, DayOfWeek.Monday, WeekOfMonth.Second, MoveWeekendHoliday.PreviousBusinessDay);

      Assert.AreEqual(4, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(2, m_country.Holidays.Count, "Country holidays not added to country");
      Assert.AreEqual(2, m_exchange.Holidays.Count, "Exchange holidays not added to exchange");

      Guid countryId = m_country.Id;
      Guid countryHolidayDayOfMonthId = countryHolidayDayOfMonth.Id;
      Guid countryHolidayDayOfWeekId = countryHolidayDayOfWeek.Id;
      Guid exchangeId = m_exchange.Id;
      Guid exchangeHolidayDayOfMonthId = exchangeHolidayDayOfMonth.Id;
      Guid exchangeHolidayDayOfWeekId = exchangeHolidayDayOfWeek.Id;

      m_dataManager.Refresh();

      Country country = (Country)m_dataManager.Countries.First(x => x.Id == countryId);
      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == exchangeId);

      Assert.AreEqual(2, country.Holidays.Count, "POST-REFRESH: Country holidays not added to country");
      Assert.AreEqual(2, exchange.Holidays.Count, "POST-REFRESH: Exchange holidays not added to exchange");

      Assert.IsNotNull(country.Holidays.FirstOrDefault(x => x.Id == countryHolidayDayOfMonthId), "POST-REFRESH: Country holiday day of month not found");
      Assert.IsNotNull(country.Holidays.FirstOrDefault(x => x.Id == countryHolidayDayOfWeekId), "POST-REFRESH: Country holiday day of week not found");
      Assert.IsNotNull(exchange.Holidays.FirstOrDefault(x => x.Id == exchangeHolidayDayOfMonthId), "POST-REFRESH: Exchange holiday day of month not found");
      Assert.IsNotNull(exchange.Holidays.FirstOrDefault(x => x.Id == exchangeHolidayDayOfWeekId), "POST-REFRESH: Exchange holiday day of week not found");
    }

    [TestMethod]
    public void Create_SessionPersistData_Success()
    {
      Session preMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Pre-market", new TimeOnly(6,0), new TimeOnly(9,30));
      Session market = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Market", new TimeOnly(9,30), new TimeOnly(16,0));
      Session postMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Post-market", new TimeOnly(16,0), new TimeOnly(21,0));

      Assert.AreEqual(3, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(3, m_exchange.Sessions[DayOfWeek.Monday].Count, "Session count is incorrect");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == preMarket), "Pre-market session not added to exchange");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == market), "Market session not added to exchange");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == postMarket), "Post-market session not added to exchange");

      Assert.AreEqual(m_dataManager, preMarket.DataManager, "Pre-market exchange is not correct");
      Assert.AreEqual(m_exchange, preMarket.Exchange, "Pre-market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, preMarket.Day, "Pre-market day is not correct");
      Assert.AreEqual("Pre-market", preMarket.Name, "Pre-market name is not correct");
      Assert.AreNotEqual(Guid.Empty, preMarket.NameTextId, "Pre-market name text id is not set");
      Assert.AreEqual("6:00 AM", preMarket.Start.ToString(), "Pre-market start is not set");
      Assert.AreEqual("9:30 AM", preMarket.End.ToString(), "Pre-market end is not set");

      Assert.AreEqual(m_dataManager, market.DataManager, "Market exchange is not correct");
      Assert.AreEqual(m_exchange, market.Exchange, "Market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, market.Day, "Market day is not correct");
      Assert.AreEqual("Market", market.Name, "Market name is not correct");
      Assert.AreNotEqual(Guid.Empty, market.NameTextId, "Market name text id is not set");
      Assert.AreEqual("9:30 AM", market.Start.ToString(), "Market start is not set");
      Assert.AreEqual("4:00 PM", market.End.ToString(), "Market end is not set");

      Assert.AreEqual(m_dataManager, postMarket.DataManager, "Post-market exchange is not correct");
      Assert.AreEqual(m_exchange, postMarket.Exchange, "Post-market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, postMarket.Day, "Post-market day is not correct");
      Assert.AreEqual("Post-market", postMarket.Name, "Post-market name is not correct");
      Assert.AreNotEqual(Guid.Empty, postMarket.NameTextId, "Post-market name text id is not set");
      Assert.AreEqual("4:00 PM", postMarket.Start.ToString(), "Post-market start is not set");
      Assert.AreEqual("9:00 PM", postMarket.End.ToString(), "Post-market end is not set");

      Guid exchangeId = m_exchange.Id;
      Guid preMarketId = preMarket.Id;
      Guid marketId = market.Id;
      Guid postMarketId = postMarket.Id;

      m_dataManager.Refresh();

      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == exchangeId);
      Assert.AreEqual(3, exchange.Sessions[DayOfWeek.Monday].Count, "POST-REFRESH: Session count is incorrect");
      preMarket = (Session)exchange.Sessions[DayOfWeek.Monday].First(x => x.Id == preMarketId);
      market = (Session)exchange.Sessions[DayOfWeek.Monday].First(x => x.Id == marketId);
      postMarket = (Session)exchange.Sessions[DayOfWeek.Monday].First(x => x.Id == postMarketId);
      Assert.IsNotNull(preMarket, "POST-REFRESH: Pre-market session not added to exchange");
      Assert.IsNotNull(market, "POST-REFRESH: Market session not added to exchange");
      Assert.IsNotNull(postMarket, "POST-REFRESH: Post-market session not added to exchange");

      Assert.AreEqual(m_dataManager, preMarket.DataManager, "POST-REFRESH: Pre-market exchange is not correct");
      Assert.AreEqual(exchange, preMarket.Exchange, "POST-REFRESH: Pre-market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, preMarket.Day, "POST-REFRESH: Pre-market day is not correct");
      Assert.AreEqual("Pre-market", preMarket.Name, "POST-REFRESH: Pre-market name is not correct");
      Assert.AreNotEqual(Guid.Empty, preMarket.NameTextId, "POST-REFRESH: Pre-market name text id is not set");
      Assert.AreEqual("6:00 AM", preMarket.Start.ToString(), "POST-REFRESH: Pre-market start is not set");
      Assert.AreEqual("9:30 AM", preMarket.End.ToString(), "POST-REFRESH: Pre-market end is not set");

      Assert.AreEqual(m_dataManager, market.DataManager, "POST-REFRESH: Market exchange is not correct");
      Assert.AreEqual(exchange, market.Exchange, "POST-REFRESH: Market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, market.Day, "POST-REFRESH: Market day is not correct");
      Assert.AreEqual("Market", market.Name, "POST-REFRESH: Market name is not correct");
      Assert.AreNotEqual(Guid.Empty, market.NameTextId, "POST-REFRESH: Market name text id is not set");
      Assert.AreEqual("9:30 AM", market.Start.ToString(), "POST-REFRESH: Market start is not set");
      Assert.AreEqual("4:00 PM", market.End.ToString(), "POST-REFRESH: Market end is not set");

      Assert.AreEqual(m_dataManager, postMarket.DataManager, "POST-REFRESH: Post-market exchange is not correct");
      Assert.AreEqual(exchange, postMarket.Exchange, "POST-REFRESH: Post-market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, postMarket.Day, "POST-REFRESH: Post-market day is not correct");
      Assert.AreEqual("Post-market", postMarket.Name, "POST-REFRESH: Post-market name is not correct");
      Assert.AreNotEqual(Guid.Empty, postMarket.NameTextId, "POST-REFRESH: Post-market name text id is not set");
      Assert.AreEqual("4:00 PM", postMarket.Start.ToString(), "POST-REFRESH: Post-market start is not set");
      Assert.AreEqual("9:00 PM", postMarket.End.ToString(), "POST-REFRESH: Post-market end is not set");
    }

    [TestMethod]
    public void Create_InstrumentGroupPersist_Success()
    {
      InstrumentGroup group = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description");

      Assert.AreEqual(m_dataManager, group.DataManager, "Data manager not set correctly");
      Assert.AreEqual(InstrumentGroupRoot.Instance, group.Parent, "Data manager not set correctly");
      Assert.IsNotNull(m_dataManager.InstrumentGroups.FirstOrDefault(x => x.Id == group.Id), "New group is not added to list on data manager");
      Assert.AreEqual(InstrumentGroupRoot.Instance, group.Parent, "Parent not set correctly");
      Assert.AreEqual("Test Instrument Group", group.Name, "Name not set correctly");
      Assert.AreEqual("Test Instrument Group Description", group.Description, "Description not set correctly");

      m_dataManager.Refresh();

      InstrumentGroup postRefreshGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == group.Id);
      Assert.IsNotNull(postRefreshGroup, "POST-REFRESH: New group is not added to list on data manager");
      Assert.AreEqual(m_dataManager, postRefreshGroup.DataManager, "POST-REFRESH: Data manager not set correctly");
      Assert.AreEqual(InstrumentGroupRoot.Instance, postRefreshGroup.Parent, "POST-REFRESH: Data manager not set correctly");
      Assert.AreEqual(InstrumentGroupRoot.Instance, postRefreshGroup.Parent, "POST-REFRESH: Parent not set correctly");
      Assert.AreEqual("Test Instrument Group", postRefreshGroup.Name, "POST-REFRESH: Name not set correctly");
      Assert.AreEqual("Test Instrument Group Description", postRefreshGroup.Description, "POST-REFRESH: Description not set correctly");
    }

    [TestMethod]
    public void Create_InstrumentGroupSubGroupPersist_Success()
    {
      InstrumentGroup group = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description");
      InstrumentGroup subGroup = (InstrumentGroup)m_dataManager.Create("Test Instrument sub-group", "Test Instrument Sub-group Description", group);

      Assert.AreEqual(m_dataManager, subGroup.DataManager, "Data manager not set correctly for sub-group");
      Assert.AreEqual(group, subGroup.Parent, "Data manager not set correctly for sub-group");
      Assert.IsNotNull(m_dataManager.InstrumentGroups.FirstOrDefault(x => x.Id == subGroup.Id), "New sub-group is not added to list on data manager");

      m_dataManager.Refresh();

      InstrumentGroup postRefreshGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == group.Id);
      InstrumentGroup postRefreshSubGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == subGroup.Id);
      Assert.IsNotNull(postRefreshGroup, "POST-REFRESH: New group is not added to list on data manager");
      Assert.IsNotNull(postRefreshSubGroup, "POST-REFRESH: New sub-group is not added to list on data manager");
      Assert.AreEqual(m_dataManager, postRefreshGroup.DataManager, "POST-REFRESH: Data manager not set correctly for group");
      Assert.AreEqual(m_dataManager, postRefreshSubGroup.DataManager, "POST-REFRESH: Data manager not set correctly for sub-group");
      Assert.AreEqual(InstrumentGroupRoot.Instance, postRefreshGroup.Parent, "POST-REFRESH: Parent not set correctly for group");
      Assert.AreEqual(postRefreshGroup, postRefreshSubGroup.Parent, "POST-REFRESH: Parent not set correctly for sub-group");
    }

    [TestMethod]
    public void Create_InstrumentInstrumentGroupProperlySet_Success()
    {
      InstrumentGroup group = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description");
      Instrument instrument = (Instrument)m_dataManager.Create(m_exchange, InstrumentType.Stock, "TEST2", "TestInstrument2", "TestInstrumentDescription2", m_instrumentInceptionDate);
      m_dataManager.Update(group, instrument);

      Assert.IsNotNull(instrument.InstrumentGroups.FirstOrDefault(x => x.Id == group.Id), "Instrument group not set correctly");
      Assert.IsNotNull(group.Instruments.FirstOrDefault(x => x.Id == instrument.Id), "Instrument not added to the instrument group");

      m_dataManager.Refresh();

      InstrumentGroup postRefreshGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == group.Id);
      Assert.IsNotNull(postRefreshGroup, "POST-REFRESH: Group is not added to list on data manager");
      Instrument postRefreshInstrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == instrument.Id);
      Assert.IsNotNull(postRefreshInstrument, "POST-REFRESH: Instrument is not added to list on data manager");
      Assert.IsNotNull(postRefreshGroup.Instruments.FirstOrDefault(x => x.Id == instrument.Id), "POST-REFRESH: Instrument not added to the instrument group");
      Assert.IsNotNull(postRefreshInstrument.InstrumentGroups.FirstOrDefault(x => x.Id == postRefreshGroup.Id), "POST-REFRESH: Instrument group not set correctly");
    }

    [TestMethod]
    public void Create_InstrumentPersistData_Success()
    {
      Exchange secondaryExchange = (Exchange)m_dataManager.Create(m_country, "SecondaryExchange", m_timeZone);
      m_dataManager.CreateSecondaryExchange(m_instrument, secondaryExchange);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_dataManager.Instruments.FirstOrDefault(x => x == m_instrument), "Instrument not added to data manager");
      Assert.IsNotNull(secondaryExchange.Instruments.FirstOrDefault(x => x.Value == m_instrument), "Instrument not added to secondary exchange");

      Assert.AreEqual(m_dataManager, m_instrument.DataManager, "Instrument data manager is not correct");
      Assert.AreEqual("TEST", m_instrument.Ticker, "Instrument ticker is not correct");
      Assert.AreEqual("TestInstrument", m_instrument.Name, "Instrument name is not correct");
      Assert.AreEqual("TestInstrumentDescription", m_instrument.Description, "Instrument description is not correct");
      Assert.AreNotEqual(Guid.Empty, m_instrument.NameTextId, "Instrument name text id is not correct");
      Assert.AreNotEqual(Guid.Empty, m_instrument.DescriptionTextId, "Instrument description text id is not correct");
      Assert.AreEqual(InstrumentType.Stock, m_instrument.Type, "Instrument type is not correct");
      Assert.AreEqual(m_exchange, m_instrument.PrimaryExchange, "Instrument primary exchange is not correct");
      Assert.AreEqual(m_instrumentInceptionDate, m_instrument.InceptionDate, "Instrument inception date is not correct");
      Assert.AreEqual(1, m_instrument.SecondaryExchanges.Count, "Instrument secondary exchange not added");
      Assert.IsNotNull(m_instrument.SecondaryExchanges.FirstOrDefault(x => x.Id == secondaryExchange.Id), "Secondary exchange not found in instrument secondary exchanges");
      Assert.AreEqual(0, m_instrument.Fundamentals.Count, "Instrument fundamentals not set correctly");

      m_dataManager.Refresh();

      Assert.AreEqual(2, m_dataManager.Exchanges.Count, "POST-REFRESH: Incorrect number of exchanges returned");
      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == m_exchange.Id);
      Exchange secondaryExchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == secondaryExchange.Id);
      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);

      Assert.AreEqual(m_dataManager, instrument.DataManager, "POST-REFRESH: Instrument data manager is not correct");
      Assert.AreEqual("TEST", instrument.Ticker, "POST-REFRESH: Instrument ticker is not correct");
      Assert.AreEqual("TestInstrument", instrument.Name, "POST-REFRESH: Instrument name is not correct");
      Assert.AreEqual("TestInstrumentDescription", instrument.Description, "POST-REFRESH: Instrument description is not correct");
      Assert.AreNotEqual(Guid.Empty, instrument.NameTextId, "POST-REFRESH: Instrument name text id is not correct");
      Assert.AreNotEqual(Guid.Empty, instrument.DescriptionTextId, "POST-REFRESH: Instrument description text id is not correct");
      Assert.AreEqual(InstrumentType.Stock, instrument.Type, "POST-REFRESH: Instrument type is not correct");
      Assert.AreEqual(exchange, instrument.PrimaryExchange, "POST-REFRESH: Instrument primary exchange is not correct");
      Assert.AreEqual(m_instrumentInceptionDate, instrument.InceptionDate, "POST-REFRESH: Instrument inception date is not correct");
      Assert.AreEqual(1, instrument.SecondaryExchanges.Count, "POST-REFRESH: Instrument secondary exchange not added");
      Assert.IsNotNull(instrument.SecondaryExchanges.FirstOrDefault(x => x.Id == secondaryExchange.Id), "POST-REFRESH: Secondary exchange not found in instrument secondary exchanges");
      Assert.AreEqual(0, instrument.Fundamentals.Count, "POST-REFRESH: Instrument fundamentals not set correctly");
    }

    [TestMethod]
    public void Create_FundamentalDefinitionPersistData_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("GDP", "Gross Domestic Product", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_dataManager.Fundamentals.FirstOrDefault(x => x == fundamental), "Fundamental not added to data manager");

      Assert.AreNotEqual(Guid.Empty, fundamental.Id, "Fundamental id not set");
      Assert.AreEqual(m_dataManager, fundamental.DataManager, "Fundamental data manager not set");
      Assert.AreEqual("GDP", fundamental.Name, "Fundamental name not set");
      Assert.AreEqual("Gross Domestic Product", fundamental.Description, "Fundamental description not set");
      Assert.AreNotEqual(Guid.Empty, fundamental.NameTextId, "Fundamental name text id not set");
      Assert.AreNotEqual(Guid.Empty, fundamental.DescriptionTextId, "Fundamental description text id not set");
      Assert.AreEqual(FundamentalCategory.Country, fundamental.Category, "Fundamental category is not set");
      Assert.AreEqual(FundamentalReleaseInterval.Monthly, fundamental.ReleaseInterval, "Fundamental release interval is not set");

      m_dataManager.Refresh();

      Fundamental fundamentalAfterRefresh = (Fundamental)m_dataManager.Fundamentals.First(x => x.Id == fundamental.Id);

      Assert.AreNotEqual(Guid.Empty, fundamentalAfterRefresh.Id, "POST-REFRESH: Fundamental id not set");
      Assert.AreEqual(m_dataManager, fundamentalAfterRefresh.DataManager, "POST-REFRESH: Fundamental data manager not set");
      Assert.AreEqual("GDP", fundamentalAfterRefresh.Name, "POST-REFRESH: Fundamental name not set");
      Assert.AreEqual("Gross Domestic Product", fundamentalAfterRefresh.Description, "POST-REFRESH: Fundamental description not set");
      Assert.AreNotEqual(Guid.Empty, fundamentalAfterRefresh.NameTextId, "POST-REFRESH: Fundamental name text id not set");
      Assert.AreNotEqual(Guid.Empty, fundamentalAfterRefresh.DescriptionTextId, "POST-REFRESH: Fundamental description text id not set");
      Assert.AreEqual(FundamentalCategory.Country, fundamentalAfterRefresh.Category, "POST-REFRESH: Fundamental category is not set");
      Assert.AreEqual(FundamentalReleaseInterval.Monthly, fundamental.ReleaseInterval, "POST-REFRESH: Fundamental release interval is not set");
    }

    [TestMethod]
    public void Create_CountryFundamentalDefinitionPersistData_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("GDP", "Gross Domestic Product", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);
      CountryFundamental countryFundamental = (CountryFundamental)m_dataManager.Create(fundamental, m_country);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_country.Fundamentals.FirstOrDefault(x => x == countryFundamental), "Country fundamental not added to country");

      Assert.AreEqual(m_country.Id, countryFundamental.CountryId, "Country id is not set correctly");
      Assert.AreEqual(m_country, countryFundamental.Country, "Country reference is not set correctly");
      Assert.AreEqual(fundamental.Id, countryFundamental.Fundamental.Id, "Fundamental id is not set correctly");
      Assert.AreEqual(fundamental, countryFundamental.Fundamental, "Fundamental reference is not set correctly");

      m_dataManager.Refresh();

      Country countryAfterRefresh = (Country)m_dataManager.Countries.First(x => x.Id == m_country.Id);
      Assert.IsNotNull(countryAfterRefresh.Fundamentals.FirstOrDefault(x => x.Fundamental.Id == countryFundamental.Fundamental.Id), "POST-REFRESH: Country fundamental not loaded after reload");
    }

    [TestMethod]
    public void Create_InstrumentFundamentalDefinitionPersistData_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("Revenue", "Revenue", FundamentalCategory.Instrument, FundamentalReleaseInterval.Quarterly);
      InstrumentFundamental instrumentFundamental = (InstrumentFundamental)m_dataManager.Create(fundamental, m_instrument);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_instrument.Fundamentals.FirstOrDefault(x => x == instrumentFundamental), "Instrument fundamental not added to instrument");

      Assert.AreEqual(m_instrument.Id, instrumentFundamental.InstrumentId, "Instrument id is not set correctly");
      Assert.AreEqual(m_instrument, instrumentFundamental.Instrument, "Instrument reference is not set correctly");
      Assert.AreEqual(fundamental.Id, instrumentFundamental.Fundamental.Id, "Fundamental id is not set correctly");
      Assert.AreEqual(fundamental, instrumentFundamental.Fundamental, "Fundamental reference is not set correctly");
    }

    [TestMethod]
    public void Update_ObjectNameCurrentCultureImplicit_Success()
    {
      m_dataManager.Update((IName)m_instrument, "NewTestInstrument");
      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual("NewTestInstrument", m_instrument.Name, "Instance name not updated");

      m_dataManager.Refresh();

      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual("NewTestInstrument", instrument.Name, "POST-REFRESH: Instance name not updated");
    }

    [TestMethod]
    public void Update_ObjectNameCurrentCultureExplicit_Success()
    {
      m_dataManager.Update((IName)m_instrument, "NewTestInstrument", m_configuration.Object.CultureInfo);
      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual("NewTestInstrument", m_instrument.Name, "Instance name not updated");

      m_dataManager.Refresh();

      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual("NewTestInstrument", instrument.Name, "POST-REFRESH: Instance name not updated");
    }

    [TestMethod]
    public void Update_ObjectNameOtherCultureLanguage_Success()
    {
      m_dataManager.Update((IName)m_instrument, "NeuesPrüfgerät", m_cultureGerman);
      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual("TestInstrument", m_instrument.Name, "Instance name in current language was changed");
      Assert.AreEqual("NeuesPrüfgerät", m_instrument.NameInLanguage(m_cultureGerman), "Instance name in other language was not changed");

      m_dataManager.Refresh();

      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual("TestInstrument", m_instrument.Name, "POST-REFRESH: Instance name in current language was changed");
      Assert.AreEqual("NeuesPrüfgerät", m_instrument.NameInLanguage(m_cultureGerman), "POST-REFRESH: Instance name in other language was not changed");
    }

    [TestMethod]
    public void Update_ObjectDescriptionCurrentCultureImplicit_Success()
    {
      m_dataManager.Update((IDescription)m_instrument, "NewTestInstrumentDescription");
      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual("NewTestInstrumentDescription", m_instrument.Description, "Instance description not updated");

      m_dataManager.Refresh();

      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual("NewTestInstrumentDescription", instrument.Description, "POST-REFRESH: Instance description not updated");
    }

    [TestMethod]
    public void Update_ObjectDescriptionCurrentCultureExplicit_Success()
    {
      m_dataManager.Update((IDescription)m_instrument, "NewTestInstrumentDescription", m_configuration.Object.CultureInfo);
      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual("NewTestInstrumentDescription", m_instrument.Description, "Instance description not updated");

      m_dataManager.Refresh();

      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual("NewTestInstrumentDescription", instrument.Description, "POST-REFRESH: Instance description not updated");
    }

    [TestMethod]
    public void Update_ObjectDescriptionOtherCultureLanguage_Success()
    {
      m_dataManager.Update((IDescription)m_instrument, "BeschreibungDesNeuenPrüfgeräts", m_cultureGerman);
      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual("TestInstrumentDescription", m_instrument.Description, "Instance description in current language was changed");
      Assert.AreEqual("BeschreibungDesNeuenPrüfgeräts", m_instrument.DescriptionInLanguage(m_cultureGerman), "Instance description in other language was not changed");

      m_dataManager.Refresh();

      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual("TestInstrumentDescription", m_instrument.Description, "POST-REFRESH: Instance description in current language was changed");
      Assert.AreEqual("BeschreibungDesNeuenPrüfgeräts", m_instrument.DescriptionInLanguage(m_cultureGerman), "POST-REFRESH: Instance description in other language was not changed");
    }

    [TestMethod]
    public void Update_CountryFundamentalVerifyDataPersist_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("GDP", "Gross Domestic Product", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);
      CountryFundamental countryFundamental = (CountryFundamental)m_dataManager.Create(fundamental, m_country);

      DateTime dateTime  = DateTime.Now.ToUniversalTime();
      double value = 1000.0;
      m_dataManager.Update(countryFundamental, dateTime, value);
      m_dataManager.Update(countryFundamental, dateTime.AddMonths(1), value + 1);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsTrue(countryFundamental.Values.TryGetValue(dateTime, out decimal value1) && value1 == (decimal)value, "First value is not correct in country fundamental object");
      Assert.IsTrue(countryFundamental.Values.TryGetValue(dateTime.AddMonths(1), out decimal value2) && value2 == (decimal)value + 1, "Second value is not correct in country fundamental object");
      Assert.IsTrue(countryFundamental.Latest.HasValue && countryFundamental.Latest!.Value.Key == dateTime.AddMonths(1), "Latest key is not correct on country fundamental object");
      Assert.IsTrue(countryFundamental.Latest.HasValue && (double)countryFundamental.Latest!.Value.Value == value + 1, "Latest value is not correct on country fundamental object");

      CountryFundamental countryFundamentalAfterUpdate = (CountryFundamental)m_country.Fundamentals.First(x => x.Fundamental.Id == fundamental.Id);

      Assert.AreEqual(2, countryFundamentalAfterUpdate.Values.Count, "Country fundamental value count is incorrect");
      
      Assert.IsTrue(countryFundamentalAfterUpdate.Values.TryGetValue(dateTime, out decimal value3) && value3 == (decimal)value, "First value is not correct in country fundamental object");
      Assert.IsTrue(countryFundamentalAfterUpdate.Values.TryGetValue(dateTime.AddMonths(1), out decimal value4) && value4 == (decimal)value + 1, "Second value is not correct in country fundamental object");
      Assert.IsTrue(countryFundamentalAfterUpdate.Latest.HasValue && countryFundamentalAfterUpdate.Latest!.Value.Key == dateTime.AddMonths(1), "Latest key is not correct in country object");
      Assert.IsTrue(countryFundamentalAfterUpdate.Latest.HasValue && (double)countryFundamentalAfterUpdate.Latest!.Value.Value == value + 1, "Latest value is not correct in country object");

      m_dataManager.Refresh();

      CountryFundamental countryFundamentalAfterRefresh = (CountryFundamental)m_country.Fundamentals.First(x => x.Fundamental.Id == fundamental.Id);

      Assert.AreEqual(2, countryFundamentalAfterRefresh.Values.Count, "POST-REFRESH: Country fundamental value count is incorrect");

      Assert.IsTrue(countryFundamentalAfterRefresh.Values.TryGetValue(dateTime, out decimal value5) && value5 == (decimal)value, "POST-REFRESH: First value is not correct in country fundamental object");
      Assert.IsTrue(countryFundamentalAfterRefresh.Values.TryGetValue(dateTime.AddMonths(1), out decimal value6) && value6 == (decimal)value + 1, "POST-REFRESH: Second value is not correct in country fundamental object");
      Assert.IsTrue(countryFundamentalAfterRefresh.Latest.HasValue && countryFundamentalAfterUpdate.Latest!.Value.Key == dateTime.AddMonths(1), "POST-REFRESH: Latest key is not correct in country object");
      Assert.IsTrue(countryFundamentalAfterRefresh.Latest.HasValue && (double)countryFundamentalAfterUpdate.Latest!.Value.Value == value + 1, "POST-REFRESH: Latest value is not correct in country object");
    }

    [TestMethod]
    public void Update_InstrumentFundamentalVerifyDataPersist_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("Cashflow", "Cash Flow", FundamentalCategory.Instrument, FundamentalReleaseInterval.Monthly);
      InstrumentFundamental instrumentFundamental = (InstrumentFundamental)m_dataManager.Create(fundamental, m_instrument);

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value = 2000.0;
      m_dataManager.Update(instrumentFundamental, dateTime, value);
      m_dataManager.Update(instrumentFundamental, dateTime.AddMonths(1), value + 1);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsTrue(instrumentFundamental.Values.TryGetValue(dateTime, out decimal value1) && value1 == (decimal)value, "First value is not correct in instrument object");
      Assert.IsTrue(instrumentFundamental.Values.TryGetValue(dateTime.AddMonths(1), out decimal value2) && value2 == (decimal)value + 1, "Second value is not correct in instrument object");
      Assert.IsTrue(instrumentFundamental.Latest.HasValue && instrumentFundamental.Latest!.Value.Key == dateTime.AddMonths(1), "Latest key is not correct on instrument fundamental object");
      Assert.IsTrue(instrumentFundamental.Latest.HasValue && (double)instrumentFundamental.Latest!.Value.Value == value + 1, "Latest value is not correct on instrument fundamental object");

      InstrumentFundamental instrumentFundamentalAfterUpdate = (InstrumentFundamental)m_instrument.Fundamentals.First(x => x.Fundamental.Id == fundamental.Id);

      Assert.AreEqual(2, instrumentFundamentalAfterUpdate.Values.Count, "Instrument fundamental value count is incorrect");

      Assert.IsTrue(instrumentFundamentalAfterUpdate.Values.TryGetValue(dateTime, out decimal value3) && value3 == (decimal)value, "First value is not correct in instrument fundamental object");
      Assert.IsTrue(instrumentFundamentalAfterUpdate.Values.TryGetValue(dateTime.AddMonths(1), out decimal value4) && value4 == (decimal)value + 1, "Second value is not correct in instrument fundamental object");
      Assert.IsTrue(instrumentFundamentalAfterUpdate.Latest.HasValue && instrumentFundamentalAfterUpdate.Latest!.Value.Key == dateTime.AddMonths(1), "Latest key is not correct in instrument object");
      Assert.IsTrue(instrumentFundamentalAfterUpdate.Latest.HasValue && (double)instrumentFundamentalAfterUpdate.Latest!.Value.Value == value + 1, "Latest value is not correct in instrument object");

      m_dataManager.Refresh();

      InstrumentFundamental instrumentFundamentalAfterRefresh = (InstrumentFundamental)m_instrument.Fundamentals.First(x => x.Fundamental.Id == fundamental.Id);

      Assert.AreEqual(2, instrumentFundamentalAfterRefresh.Values.Count, "POST-REFRESH: Instrument fundamental value count is incorrect");

      Assert.IsTrue(instrumentFundamentalAfterRefresh.Values.TryGetValue(dateTime, out decimal value5) && value5 == (decimal)value, "POST-REFRESH: First value is not correct in instrument fundamental object");
      Assert.IsTrue(instrumentFundamentalAfterRefresh.Values.TryGetValue(dateTime.AddMonths(1), out decimal value6) && value6 == (decimal)value + 1, "POST-REFRESH: Second value is not correct in instrument fundamental object");
      Assert.IsTrue(instrumentFundamentalAfterRefresh.Latest.HasValue && instrumentFundamentalAfterUpdate.Latest!.Value.Key == dateTime.AddMonths(1), "POST-REFRESH: Latest key is not correct in instrument object");
      Assert.IsTrue(instrumentFundamentalAfterRefresh.Latest.HasValue && (double)instrumentFundamentalAfterUpdate.Latest!.Value.Value == value + 1, "POST-REFRESH: Latest value is not correct in instrument object");
    }

    [TestMethod]
    public void Update_SessionDayStartEndTime_Success()
    {
      Session preMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Pre-market", new TimeOnly(6, 0), new TimeOnly(9, 30));
      Session market = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Market", new TimeOnly(9, 30), new TimeOnly(16, 0));
      Session postMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Post-market", new TimeOnly(16, 0), new TimeOnly(21, 0));

      Assert.AreEqual(3, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(3, m_exchange.Sessions[DayOfWeek.Monday].Count, "Session count is incorrect");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == preMarket), "Pre-market session not added to exchange");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == market), "Market session not added to exchange");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == postMarket), "Post-market session not added to exchange");

      Assert.AreEqual(m_dataManager, preMarket.DataManager, "Pre-market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, preMarket.Day, "Pre-market day is not correct");
      Assert.AreEqual("Pre-market", preMarket.Name, "Pre-market name is not correct");
      Assert.AreEqual("6:00 AM", preMarket.Start.ToString(), "Pre-market start is not set");
      Assert.AreEqual("9:30 AM", preMarket.End.ToString(), "Pre-market end is not set");

      Assert.AreEqual(m_dataManager, market.DataManager, "Market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, market.Day, "Market day is not correct");
      Assert.AreEqual("Market", market.Name, "Market name is not correct");
      Assert.AreEqual("9:30 AM", market.Start.ToString(), "Market start is not set");
      Assert.AreEqual("4:00 PM", market.End.ToString(), "Market end is not set");

      Assert.AreEqual(m_dataManager, postMarket.DataManager, "Post-market exchange is not correct");
      Assert.AreEqual(DayOfWeek.Monday, postMarket.Day, "Post-market day is not correct");
      Assert.AreEqual("Post-market", postMarket.Name, "Post-market name is not correct");
      Assert.AreEqual("4:00 PM", postMarket.Start.ToString(), "Post-market start is not set");
      Assert.AreEqual("9:00 PM", postMarket.End.ToString(), "Post-market end is not set");

      m_dataManager.Update(preMarket, DayOfWeek.Tuesday, new TimeOnly(6, 30), new TimeOnly(9, 45));

      //check that session was update
      Assert.AreEqual(2, m_exchange.Sessions[DayOfWeek.Monday].Count, "POST-UPDATE: Session count for Monday is incorrect");
      Assert.AreEqual(1, m_exchange.Sessions[DayOfWeek.Tuesday].Count, "POST-UPDATE: Session count for Tuesday is incorrect");
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Tuesday].FirstOrDefault(x => x == preMarket), "POST-UPDATE: Pre-market session not moved to correct day on exchange");
      Assert.AreEqual(DayOfWeek.Tuesday, preMarket.Day, "POST-UPDATE: Pre-market day is not correct");
      Assert.AreEqual("Pre-market", preMarket.Name, "POST-UPDATE: Pre-market name is not correct");
      Assert.AreEqual("6:30 AM", preMarket.Start.ToString(), "POST-UPDATE: Pre-market start is not set");
      Assert.AreEqual("9:45 AM", preMarket.End.ToString(), "POST-UPDATE: Pre-market end is not set");

      //check that other sessions was unaffected
      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == market), "POST-UPDATE: Market session not moved to correct day on exchange");
      Assert.AreEqual(DayOfWeek.Monday, market.Day, "POST-UPDATE: Market day is not correct");
      Assert.AreEqual("Market", market.Name, "POST-UPDATE: Market name is not correct");
      Assert.AreEqual("9:30 AM", market.Start.ToString(), "POST-UPDATE: Market start is not set");
      Assert.AreEqual("4:00 PM", market.End.ToString(), "POST-UPDATE: Market end is not set");

      Assert.IsNotNull(m_exchange.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x == postMarket), "POST-UPDATE: Post-market session not moved to correct day on exchange");
      Assert.AreEqual(DayOfWeek.Monday, postMarket.Day, "POST-UPDATE: Pre-market day is not correct");
      Assert.AreEqual("Post-market", postMarket.Name, "POST-UPDATE: Post-market name is not correct");
      Assert.AreEqual("4:00 PM", postMarket.Start.ToString(), "POST-UPDATE: Post-market start is not set");
      Assert.AreEqual("9:00 PM", postMarket.End.ToString(), "POST-UPDATE: Post-market end is not set");

      //check that session is properly loaded from database
      Guid exchangeId = m_exchange.Id;
      Guid preMarketId = preMarket.Id;
      Guid marketId = market.Id;
      Guid postMarketId = postMarket.Id;

      m_dataManager.Refresh();

      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == exchangeId);
      Assert.AreEqual(2, exchange.Sessions[DayOfWeek.Monday].Count, "POST-REFRESH: Session count for Monday is incorrect");
      Assert.AreEqual(1, exchange.Sessions[DayOfWeek.Tuesday].Count, "POST-REFRESH: Session count for Tuesday is incorrect");
      preMarket = (Session)exchange.Sessions[DayOfWeek.Tuesday].First(x => x.Id == preMarketId);
      market = (Session)exchange.Sessions[DayOfWeek.Monday].First(x => x.Id == marketId);
      postMarket = (Session)exchange.Sessions[DayOfWeek.Monday].First(x => x.Id == postMarketId);
      Assert.IsNotNull(preMarket, "POST-REFRESH: Pre-market session not added to exchange");
      Assert.IsNotNull(market, "POST-REFRESH: Market session not added to exchange");
      Assert.IsNotNull(postMarket, "POST-REFRESH: Post-market session not added to exchange");

      Assert.AreEqual("Pre-market", preMarket.Name, "POST-REFRESH: Pre-market name is not correct");
      Assert.AreEqual("6:30 AM", preMarket.Start.ToString(), "POST-REFRESH: Pre-market start is not set");
      Assert.AreEqual("9:45 AM", preMarket.End.ToString(), "POST-REFRESH: Pre-market end is not set");

      Assert.AreEqual("Market", market.Name, "POST-REFRESH: Market name is not correct");
      Assert.AreEqual("9:30 AM", market.Start.ToString(), "POST-REFRESH: Market start is not set");
      Assert.AreEqual("4:00 PM", market.End.ToString(), "POST-REFRESH: Market end is not set");

      Assert.AreEqual("Post-market", postMarket.Name, "POST-REFRESH: Post-market name is not correct");
      Assert.AreEqual("4:00 PM", postMarket.Start.ToString(), "POST-REFRESH: Post-market start is not set");
      Assert.AreEqual("9:00 PM", postMarket.End.ToString(), "POST-REFRESH: Post-market end is not set");
    }

    [TestMethod]
    public void Update_InstrumentExchangeTickerInceptionDate_Success()
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime newInceptionDate = DateTime.Now.ToUniversalTime().AddDays(-10);
      Exchange newExchange = (Exchange)m_dataManager.Create(m_country, "NewExchange", m_timeZone);

      m_dataManager.Update(m_instrument, newExchange.Id, "NWTCK", newInceptionDate);

      Assert.AreEqual(2, m_modelChangeObserverCount, "POST-UPDATE: Model change observer count is not correct");
      Assert.AreEqual(newExchange, m_instrument.PrimaryExchange, "POST-UPDATE: Exchange not updated");
      Assert.AreEqual("NWTCK", m_instrument.Ticker, "POST-UPDATE: Ticker not updated");
      Assert.AreEqual(newInceptionDate, m_instrument.InceptionDate, "POST-UPDATE: Inception date not updated");

      m_dataManager.Refresh();

      Exchange exchange = (Exchange)m_dataManager.Exchanges.First(x => x.Id == newExchange.Id);
      Instrument instrument = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);

      Assert.AreEqual(exchange, instrument.PrimaryExchange, "POST-REFRESH: Exchange not updated");
      Assert.AreEqual("NWTCK", instrument.Ticker, "POST-REFRESH: Ticker not updated");
      Assert.AreEqual(newInceptionDate, instrument.InceptionDate, "POST-REFRESH: Inception date not updated");
    }

    [TestMethod]
    public void Update_InstrumentGroupAddSubGroup_Success()
    {
      InstrumentGroup group = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description");
      InstrumentGroup subGroup = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description", group);

      m_dataManager.Update(group, m_instrument);

      Assert.AreEqual(3, m_modelChangeObserverCount, "POST-UPDATE: Model change observer count is not correct");
      Assert.IsNotNull(group.Children.FirstOrDefault(x => x.Id == subGroup.Id), "POST-UPDATE: Sub-group not added to the group");
      Assert.AreEqual(group, subGroup.Parent, "POST-UPDATE: Sub-group parent not set correctly");

      m_dataManager.Refresh();

      InstrumentGroup postRefreshGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == group.Id);
      InstrumentGroup postRefreshSubGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == subGroup.Id);
      Assert.IsNotNull(postRefreshGroup, "POST-REFRESH: Group not found on data manager");
      Assert.IsNotNull(postRefreshSubGroup, "POST-REFRESH: Sub-group not found on data manager");
      Assert.AreEqual(postRefreshGroup, postRefreshSubGroup.Parent, "POST-REFRESH: Sub-group parent not set correctly");
      Assert.IsNotNull(postRefreshGroup.Instruments.FirstOrDefault(x => x.Id == m_instrument.Id), "POST-REFRESH: Instrument not added to the group");
    }

    [TestMethod]
    public void Update_InstrumentGroupParentCorrectlyReassigned_Success()
    {
      InstrumentGroup group1 = (InstrumentGroup)m_dataManager.Create("Test Instrument Group1", "Test Instrument Group Description2");
      InstrumentGroup group2 = (InstrumentGroup)m_dataManager.Create("Test Instrument Group1", "Test Instrument Group Description2");
      InstrumentGroup subGroup = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description", group1);

      Assert.IsNotNull(group1.Children.FirstOrDefault(x => x.Id == subGroup.Id), "Sub-group not added to the group1");
      Assert.IsNull(group2.Children.FirstOrDefault(x => x.Id == subGroup.Id), "Sub-group added to the group2");
      Assert.AreEqual(group1, subGroup.Parent, "Sub-group parent not set correctly");

      m_dataManager.Update(subGroup, group2);

      Assert.AreEqual(4, m_modelChangeObserverCount, "POST-UPDATE: Model change observer count is not correct");
      Assert.IsNull(group1.Children.FirstOrDefault(x => x.Id == subGroup.Id), "POST-UPDATE: Sub-group not removed from group1");
      Assert.IsNotNull(group2.Children.FirstOrDefault(x => x.Id == subGroup.Id), "POST-UPDATE: Sub-group not added to the group2");
      Assert.AreEqual(group2, subGroup.Parent, "POST-UPDATE: Sub-group parent updated correctly");

      m_dataManager.Refresh();

      InstrumentGroup postRefreshGroup1 = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == group1.Id);
      InstrumentGroup postRefreshGroup2 = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == group2.Id);
      InstrumentGroup postRefreshSubGroup = (InstrumentGroup)m_dataManager.InstrumentGroups.First(x => x.Id == subGroup.Id);
      Assert.IsNotNull(postRefreshGroup1, "POST-REFRESH: Group1 not found on data manager");
      Assert.IsNotNull(postRefreshGroup2, "POST-REFRESH: Group2 not found on data manager");
      Assert.IsNotNull(postRefreshSubGroup, "POST-REFRESH: Sub-group not found on data manager");
      Assert.IsNull(postRefreshGroup1.Children.FirstOrDefault(x => x.Id == subGroup.Id), "POST-REFRESH: Sub-group not removed from group1");
      Assert.IsNotNull(postRefreshGroup2.Children.FirstOrDefault(x => x.Id == subGroup.Id), "POST-REFRESH: Sub-group not added to the group2");
      Assert.AreEqual(postRefreshGroup2, postRefreshSubGroup.Parent, "POST-REFRESH: Sub-group parent updated correctly");
    }

    [TestMethod]
    public void Update_InstrumentGroupAddInstrument_Success()
    {
      InstrumentGroup group = (InstrumentGroup)m_dataManager.Create("Test Instrument Group", "Test Instrument Group Description");
      m_dataManager.Update(group, m_instrument);

      Assert.AreEqual(2, m_modelChangeObserverCount, "POST-UPDATE: Model change observer count is not correct");
      Assert.IsNotNull(group.Instruments.FirstOrDefault(x => x.Id == m_instrument.Id), "POST-UPDATE: Instrument not added to the group" );
      Assert.IsNotNull(m_instrument.InstrumentGroups.FirstOrDefault(x => x.Id == group.Id), "POST-UPDATE: Group not added to instrument groups" );

      m_dataManager.Refresh();

      InstrumentGroup? postRefreshGroup = (InstrumentGroup?)m_dataManager.InstrumentGroups.FirstOrDefault(x => x.Id == group.Id);
      Assert.IsNotNull(postRefreshGroup, "POST-REFRESH: Group not found on data manager");
      Assert.IsNotNull(postRefreshGroup.Instruments.FirstOrDefault(x => x.Id == m_instrument.Id), "POST-REFRESH: Instrument not added to the group");
      Instrument? postRefreshInstrument = (Instrument?)m_dataManager.Instruments.FirstOrDefault(x => x.Id == m_instrument.Id);
      Assert.IsNotNull(postRefreshInstrument, "POST-REFRESH: Instrument not found on data manager");
      Assert.IsNotNull(postRefreshInstrument.InstrumentGroups.FirstOrDefault(x => x.Id == group.Id), "POST-REFRESH: Group not added to the instrument");
    }

    [TestMethod]
    public void GetDataFeed_DisposeUnsubscribesFromDataManager_Success()
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      m_dataManager.Update(m_instrument, Resolution.Day, dateTime, 1, 2, 3, 4, 5, false);

      //we need to use a using statement here to ensure the data feed is disposed of, in real scenario's the
      //garbage collector may or may not dispose of the data feed at the close of a block
      using (Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, Resolution.Day, 1, dateTime, dateTime, ToDateMode.Pinned, PriceDataType.Actual))
      {
        Assert.AreEqual(2, m_dataManager.PriceChangeObservers.Count(), "Price change observers are not correct");
        Assert.IsNotNull(m_dataManager.PriceChangeObservers[dataFeed.GetHashCode()], "Data feed not added to list of price change observers");
      }

      Assert.AreEqual(1, m_dataManager.PriceChangeObservers.Count(), "Data feed not removed from price change observer");
    }

    [TestMethod]
    public void GetDataFeed_SameCriteriaReturnsSameDataFeed_Success()
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      m_dataManager.Update(m_instrument, Resolution.Day, dateTime, 1, 2, 3, 4, 5, false);

      Data.DataFeed dataFeed1 = m_dataManager.GetDataFeed(m_instrument, Resolution.Day, 1, dateTime, dateTime, ToDateMode.Pinned, PriceDataType.Actual);
      Data.DataFeed dataFeed2 = m_dataManager.GetDataFeed(m_instrument, Resolution.Day, 1, dateTime, dateTime, ToDateMode.Pinned, PriceDataType.Actual);

      Assert.AreEqual(dataFeed1.GetHashCode(), dataFeed2.GetHashCode(), "Data feeds are not the same for exact same data feed parameters");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
    public void Update_InstrumentPriceDataSingleBar_Success(Resolution resolution, PriceDataType priceDataType)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      m_dataManager.Update(m_instrument, resolution, dateTime, 1, 2, 3, 4, 5, priceDataType == PriceDataType.Synthetic);

      Assert.AreEqual(1, m_priceChangeObserverCount, "Price change observer count is not correct");

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, resolution, 1, dateTime, dateTime, ToDateMode.Pinned, priceDataType);

      Assert.AreEqual(1, dataFeed.Count, "Data feed count is not correct");
      Assert.AreEqual(dateTime, dataFeed.DateTime[0], "Data feed date time is not correct");
      Assert.AreEqual(1, dataFeed.Open[0], "Data feed open is not correct");
      Assert.AreEqual(2, dataFeed.High[0], "Data feed high is not correct");
      Assert.AreEqual(3, dataFeed.Low[0], "Data feed low is not correct");
      Assert.AreEqual(4, dataFeed.Close[0], "Data feed close is not correct");
      Assert.AreEqual(5, dataFeed.Volume[0], "Data feed volume is not correct");
      Assert.AreEqual(4, dataFeed.LastPrice[0], "Data feed last price is not correct");
      Assert.AreEqual(5, dataFeed.LastVolume[0], "Data feed last volume is not correct");
      Assert.AreEqual(priceDataType == PriceDataType.Synthetic, dataFeed.Synthetic[0], "Data feed synthetic flag is not correct");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
     public void Update_InstrumentPriceDataReplaceBar_Success(Resolution resolution, PriceDataType priceDataType)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      m_dataManager.Update(m_instrument, resolution, dateTime, 1, 2, 3, 4, 5, priceDataType == PriceDataType.Synthetic);

      Assert.AreEqual(1, m_priceChangeObserverCount, "Price change observer count is not correct");

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, resolution, 1, dateTime, dateTime, ToDateMode.Pinned, priceDataType);

      Assert.AreEqual(1, dataFeed.Count, "Data feed count is not correct");
      Assert.AreEqual(dateTime, dataFeed.DateTime[0], "Data feed date time is not correct");
      Assert.AreEqual(1, dataFeed.Open[0], "Data feed open is not correct");
      Assert.AreEqual(2, dataFeed.High[0], "Data feed high is not correct");
      Assert.AreEqual(3, dataFeed.Low[0], "Data feed low is not correct");
      Assert.AreEqual(4, dataFeed.Close[0], "Data feed close is not correct");
      Assert.AreEqual(5, dataFeed.Volume[0], "Data feed volume is not correct");
      Assert.AreEqual(priceDataType == PriceDataType.Synthetic, dataFeed.Synthetic[0], "Data feed synthetic flag is not correct");

      m_dataManager.Update(m_instrument, resolution, dateTime, 10, 9, 8, 7, 6, priceDataType == PriceDataType.Synthetic);

      Assert.AreEqual(2, m_priceChangeObserverCount, "POST-UPDATE: Price change observer count is not correct");

      Assert.AreEqual(1, dataFeed.Count, "POST-UPDATE: Data feed count is not correct");
      Assert.AreEqual(dateTime, dataFeed.DateTime[0], "POST-UPDATE: Data feed date time is not correct");
      Assert.AreEqual(10, dataFeed.Open[0], "POST-UPDATE: Data feed open is not correct");
      Assert.AreEqual(9, dataFeed.High[0], "POST-UPDATE: Data feed high is not correct");
      Assert.AreEqual(8, dataFeed.Low[0], "POST-UPDATE: Data feed low is not correct");
      Assert.AreEqual(7, dataFeed.Close[0], "POST-UPDATE: Data feed close is not correct");
      Assert.AreEqual(6, dataFeed.Volume[0], "POST-UPDATE: Data feed volume is not correct");
      Assert.AreEqual(priceDataType == PriceDataType.Synthetic, dataFeed.Synthetic[0], "POST-UPDATE: Data feed synthetic flag is not correct");
    }

    [TestMethod]
    [DataRow(Resolution.Minute, PriceDataType.Actual)]
    [DataRow(Resolution.Minute, PriceDataType.Synthetic)]
    [DataRow(Resolution.Day, PriceDataType.Actual)]
    [DataRow(Resolution.Day, PriceDataType.Synthetic)]
    [DataRow(Resolution.Week, PriceDataType.Actual)]
    [DataRow(Resolution.Week, PriceDataType.Synthetic)]
    [DataRow(Resolution.Month, PriceDataType.Actual)]
    [DataRow(Resolution.Month, PriceDataType.Synthetic)]
    public void Update_InstrumentPriceDataRange_Success(Resolution resolution, PriceDataType priceDataType)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime fromDateTime = DateTime.Now.ToUniversalTime();
      DateTime toDateTime = fromDateTime.AddMonths(4);
      BarData barData = new IDataStoreService.BarData(5);

      switch (resolution)
      {
        case Resolution.Minute:
          barData.DateTime = new List<DateTime> { fromDateTime.AddMinutes(0), fromDateTime.AddMinutes(1), fromDateTime.AddMinutes(2), fromDateTime.AddMinutes(3), fromDateTime.AddMinutes(4) };
          toDateTime = fromDateTime.AddMinutes(4);
          break;
        case Resolution.Day:
          barData.DateTime = new List<DateTime> { fromDateTime.AddDays(0), fromDateTime.AddDays(1), fromDateTime.AddDays(2), fromDateTime.AddDays(3), fromDateTime.AddDays(4) };
          toDateTime = fromDateTime.AddDays(4);
          break;

        case Resolution.Week:
          barData.DateTime = new List<DateTime> { fromDateTime.AddDays(0), fromDateTime.AddDays(7), fromDateTime.AddDays(14), fromDateTime.AddDays(21), fromDateTime.AddDays(28) };
          toDateTime = fromDateTime.AddDays(28);
          break;

        case Resolution.Month:
          barData.DateTime = new List<DateTime> { fromDateTime.AddMonths(0), fromDateTime.AddMonths(1), fromDateTime.AddMonths(2), fromDateTime.AddMonths(3), fromDateTime.AddMonths(4) };
          toDateTime = fromDateTime.AddMonths(4);
          break;
      }

      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155 };
      barData.Synthetic = new List<bool> { priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic };

      m_dataManager.Update(m_instrument, resolution, barData);

      Assert.AreEqual(1, m_priceChangeObserverCount, "Price change observer count is not correct");

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, resolution, 1, fromDateTime, toDateTime, ToDateMode.Pinned, priceDataType);

      //dataFeed will return data in reverse order
      barData.DateTime = barData.DateTime.Reverse().ToArray();
      barData.Open = barData.Open.Reverse().ToArray();
      barData.High = barData.High.Reverse().ToArray();
      barData.Low = barData.Low.Reverse().ToArray();
      barData.Close = barData.Close.Reverse().ToArray();
      barData.Volume = barData.Volume.Reverse().ToArray();
      barData.Synthetic = barData.Synthetic.Reverse().ToArray();

      Assert.AreEqual(barData.DateTime.Count, dataFeed.Count, "Data feed count is not correct");

      for (int i = 0; i < dataFeed.Count; i++)
      {
        Assert.AreEqual(barData.DateTime[i], dataFeed.DateTime[0], $"Date time bar at index {i} not correct");
        Assert.AreEqual(barData.Open[i], dataFeed.Open[0], $"Open bar at index {i} not correct");
        Assert.AreEqual(barData.High[i], dataFeed.High[0], $"High bar at index {i} not correct");
        Assert.AreEqual(barData.Low[i], dataFeed.Low[0], $"Low bar at index {i} not correct");
        Assert.AreEqual(barData.Close[i], dataFeed.Close[0], $"Close bar at index {i} not correct");
        Assert.AreEqual(barData.Volume[i], dataFeed.Volume[0], $"Volume bar at index {i} not correct");
        Assert.AreEqual(barData.Synthetic[i], dataFeed.Synthetic[0], $"Synthetic bar at index {i} not correct");
        dataFeed.Next();
      }
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Day)]
    [DataRow(Resolution.Week)]
    [DataRow(Resolution.Month)]
    public void Update_InstrumentPriceDataBarActualAndSyntheticRange_Success(Resolution resolution)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime fromDateTime = DateTime.Now.ToUniversalTime();
      DateTime toDateTime = fromDateTime.AddMonths(4);
      BarData barData = new IDataStoreService.BarData(5);

      switch (resolution)
      {
        case Resolution.Minute:
          barData.DateTime = new List<DateTime> { fromDateTime.AddMinutes(0), fromDateTime.AddMinutes(1), fromDateTime.AddMinutes(2), fromDateTime.AddMinutes(3), fromDateTime.AddMinutes(4) };
          break;
        case Resolution.Day:
          barData.DateTime = new List<DateTime> { fromDateTime.AddDays(0), fromDateTime.AddDays(1), fromDateTime.AddDays(2), fromDateTime.AddDays(3), fromDateTime.AddDays(4) };
          break;

        case Resolution.Week:
          barData.DateTime = new List<DateTime> { fromDateTime.AddDays(0), fromDateTime.AddDays(7), fromDateTime.AddDays(14), fromDateTime.AddDays(21), fromDateTime.AddDays(28) };
          break;

        case Resolution.Month:
          barData.DateTime = new List<DateTime> { fromDateTime.AddMonths(0), fromDateTime.AddMonths(1), fromDateTime.AddMonths(2), fromDateTime.AddMonths(3), fromDateTime.AddMonths(4) };
          break;
      }

      barData.Open = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      barData.High = new List<double> { 112.0, 122.0, 132.0, 142.0, 152.0 };
      barData.Low = new List<double> { 113.0, 123.0, 133.0, 143.0, 153.0 };
      barData.Close = new List<double> { 114.0, 124.0, 134.0, 144.0, 154.0 };
      barData.Volume = new List<long> { 115, 125, 135, 145, 155 };
      barData.Synthetic = new List<bool> { true, false, true, false, true};

      m_dataManager.Update(m_instrument, resolution, barData);

      Assert.AreEqual(1, m_priceChangeObserverCount, "Price change observer count is not correct");

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, resolution, 1, fromDateTime, toDateTime, ToDateMode.Pinned, PriceDataType.Both);

      //dataFeed data will be reversed
      barData.DateTime = barData.DateTime.Reverse().ToArray();
      barData.Open = barData.Open.Reverse().ToArray();
      barData.High = barData.High.Reverse().ToArray();
      barData.Low = barData.Low.Reverse().ToArray();
      barData.Close = barData.Close.Reverse().ToArray();
      barData.Volume = barData.Volume.Reverse().ToArray();
      barData.Synthetic = barData.Synthetic.Reverse().ToArray();

      Assert.AreEqual(barData.DateTime.Count, dataFeed.Count, "Data feed count is not correct");

      for (int i = 0; i < dataFeed.Count; i++)
      {
        Assert.AreEqual(barData.DateTime[i], dataFeed.DateTime[0], $"DateTime bar at index {i} not correct");
        Assert.AreEqual(barData.Open[i], dataFeed.Open[0], $"Open bar at index {i} not correct");
        Assert.AreEqual(barData.High[i], dataFeed.High[0], $"High bar at index {i} not correct");
        Assert.AreEqual(barData.Low[i], dataFeed.Low[0], $"Low bar at index {i} not correct");
        Assert.AreEqual(barData.Close[i], dataFeed.Close[0], $"Close bar at index {i} not correct");
        Assert.AreEqual(barData.Volume[i], dataFeed.Volume[0], $"Volume bar at index {i} not correct");
        Assert.AreEqual(barData.Synthetic[i], dataFeed.Synthetic[0], $"Synthetic bar at index {i} not correct");
        dataFeed.Next();
      }      
    }

    [TestMethod]
    [DataRow(PriceDataType.Actual)]
    [DataRow(PriceDataType.Synthetic)]
    public void Update_InstrumentPriceDataL1Data_Success(PriceDataType priceDataType)
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      IDataStoreService.Level1Data level1Data = new IDataStoreService.Level1Data(5);

      level1Data.DateTime = new List<DateTime> { dateTime.AddSeconds(0), dateTime.AddSeconds(1), dateTime.AddSeconds(2), dateTime.AddSeconds(3), dateTime.AddSeconds(4) };
      level1Data.Bid = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      level1Data.Ask = new List<double> { 211.0, 221.0, 231.0, 241.0, 251.0 };
      level1Data.Last = new List<double> { 311.0, 321.0, 331.0, 341.0, 351.0 };
      level1Data.BidSize = new List<long> { 1, 2, 3, 4, 5 };
      level1Data.AskSize = new List<long> { 5, 4, 3, 2, 1 };
      level1Data.LastSize = new List<long> { 6, 7, 8, 9, 10};
      level1Data.Synthetic = new List<bool> { priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic, priceDataType == PriceDataType.Synthetic };

      m_dataManager.Update(m_instrument, level1Data);

      Assert.AreEqual(1, m_priceChangeObserverCount, "Price change observer count is not correct");

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, Resolution.Level1, 1, dateTime, dateTime.AddSeconds(5), ToDateMode.Pinned, priceDataType);

      //dataFeed data will be reversed
      level1Data.DateTime = level1Data.DateTime.Reverse().ToArray();
      level1Data.Bid = level1Data.Bid.Reverse().ToArray();
      level1Data.Ask = level1Data.Ask.Reverse().ToArray();
      level1Data.Last = level1Data.Last.Reverse().ToArray();
      level1Data.BidSize = level1Data.BidSize.Reverse().ToArray();
      level1Data.AskSize = level1Data.AskSize.Reverse().ToArray();
      level1Data.LastSize = level1Data.LastSize.Reverse().ToArray();
      level1Data.Synthetic = level1Data.Synthetic.Reverse().ToArray();

      Assert.AreEqual(level1Data.DateTime.Count, dataFeed.Count, "Data feed count is not correct");

      for (int i = 0; i < dataFeed.Count; i++)
      {
        Assert.AreEqual(level1Data.DateTime[i], dataFeed.DateTime[0], $"DateTime price at index {i} not correct");
        Assert.AreEqual(level1Data.Bid[i], dataFeed.BidPrice[0], $"Bid price at index {i} not correct");
        Assert.AreEqual(level1Data.BidSize[i], dataFeed.BidVolume[0], $"Bid volume at index {i} not correct");
        Assert.AreEqual(level1Data.Ask[i], dataFeed.AskPrice[0], $"Ask price at index {i} not correct");
        Assert.AreEqual(level1Data.AskSize[i], dataFeed.AskVolume[0], $"Ask volume at index {i} not correct");
        Assert.AreEqual(level1Data.Last[i], dataFeed.Close[0], $"Close at index {i} not correct");
        Assert.AreEqual(level1Data.LastSize[i], dataFeed.Volume[0], $"Volume at index {i} not correct");
        Assert.AreEqual(level1Data.Synthetic[i], dataFeed.Synthetic[0], $"Synthetic at index {i} not correct");

        dataFeed.Next();
      }
    }

    [TestMethod]
    public void Update_InstrumentPriceDataL1ActualAndSyntheticData_Success()
    {
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      DateTime dateTime = DateTime.Now.ToUniversalTime();
      IDataStoreService.Level1Data level1Data = new IDataStoreService.Level1Data(5);

      level1Data.DateTime = new List<DateTime> { dateTime.AddSeconds(0), dateTime.AddSeconds(1), dateTime.AddSeconds(2), dateTime.AddSeconds(3), dateTime.AddSeconds(4) };
      level1Data.Bid = new List<double> { 111.0, 121.0, 131.0, 141.0, 151.0 };
      level1Data.Ask = new List<double> { 211.0, 221.0, 231.0, 241.0, 251.0 };
      level1Data.Last = new List<double> { 311.0, 321.0, 331.0, 341.0, 351.0 };
      level1Data.BidSize = new List<long> { 1, 2, 3, 4, 5 };
      level1Data.AskSize = new List<long> { 5, 4, 3, 2, 1 };
      level1Data.LastSize = new List<long> { 6, 7, 8, 9, 10 };
      level1Data.Synthetic = new List<bool> { false, true, false, true, false};

      m_dataManager.Update(m_instrument, level1Data);

      Assert.AreEqual(1, m_priceChangeObserverCount, "Price change observer count is not correct");

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, Resolution.Level1, 1, dateTime, dateTime.AddSeconds(5), ToDateMode.Pinned, PriceDataType.Both);

      //dataFeed data will be reversed
      level1Data.DateTime = level1Data.DateTime.Reverse().ToArray();
      level1Data.Bid = level1Data.Bid.Reverse().ToArray();
      level1Data.Ask = level1Data.Ask.Reverse().ToArray();
      level1Data.Last = level1Data.Last.Reverse().ToArray();
      level1Data.BidSize = level1Data.BidSize.Reverse().ToArray();
      level1Data.AskSize = level1Data.AskSize.Reverse().ToArray();
      level1Data.LastSize = level1Data.LastSize.Reverse().ToArray();
      level1Data.Synthetic = level1Data.Synthetic.Reverse().ToArray();

      for (int i = 0; i < dataFeed.Count; i++)
      {
        Assert.AreEqual(level1Data.DateTime[i], dataFeed.DateTime[0], $"DateTime price at index {i} not correct");
        Assert.AreEqual(level1Data.Bid[i], dataFeed.BidPrice[0], $"Bid price at index {i} not correct");
        Assert.AreEqual(level1Data.BidSize[i], dataFeed.BidVolume[0], $"Bid volume at index {i} not correct");
        Assert.AreEqual(level1Data.Ask[i], dataFeed.AskPrice[0], $"Ask price at index {i} not correct");
        Assert.AreEqual(level1Data.AskSize[i], dataFeed.AskVolume[0], $"Ask volume at index {i} not correct");
        Assert.AreEqual(level1Data.Last[i], dataFeed.Close[0], $"Close at index {i} not correct");
        Assert.AreEqual(level1Data.LastSize[i], dataFeed.Volume[0], $"Volume at index {i} not correct");
        Assert.AreEqual(level1Data.Synthetic[i], dataFeed.Synthetic[0], $"Synthetic at index {i} not correct");

        dataFeed.Next();
      }
    }

    public static class ThreadSafeRandom
    {
      [ThreadStatic] private static Random? Local;

      public static Random ThisThreadsRandom
      {
        get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
      }
    }

    public static void Shuffle<T>(IList<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }

    public static void persistPriceData(object? parameters)
    {
      (IInstrument instrument, IDataManagerService dataManager, IDataStoreService.BarData barData, Resolution resolution, int phase1Count, int phase2Count, int phase3Count) = ((IInstrument, IDataManagerService, IDataStoreService.BarData, Resolution, int, int, int))parameters!;

      Assert.AreEqual(phase1Count + phase2Count + phase3Count, barData.Count, "Phase count total not the same as number of generated bars");

      int offset = 0;
      for (int i = 0; i < phase1Count; i++) dataManager.Update(instrument, resolution, barData.DateTime[offset + i], barData.Open[offset + i], barData.High[offset + i], barData.Low[offset + i], barData.Close[offset + i], barData.Volume[offset + i], barData.Synthetic[offset + i]);
      offset = phase1Count;

      List<int> randomIndices = new List<int>();
      for (int i = 0; i < phase2Count; i++) randomIndices.Add(i);
      Shuffle<int>(randomIndices);
      for (int i = 0; i < phase2Count; i++) dataManager.Update(instrument, resolution, barData.DateTime[offset + randomIndices[i]], barData.Open[offset + randomIndices[i]], barData.High[offset + randomIndices[i]], barData.Low[offset + randomIndices[i]], barData.Close[offset + randomIndices[i]], barData.Volume[offset + randomIndices[i]], barData.Synthetic[offset + randomIndices[i]]);
      
      offset = phase1Count + phase2Count;
      for (int i = 0; i < phase3Count; i++) dataManager.Update(instrument, resolution, barData.DateTime[offset + i], barData.Open[offset + i], barData.High[offset + i], barData.Low[offset + i], barData.Close[offset + i], barData.Volume[offset + i], barData.Synthetic[offset + i]);
    }

    [TestMethod]
    [DataRow(Resolution.Minute)]
    [DataRow(Resolution.Hour)]
    [DataRow(Resolution.Day)]
    [DataRow(Resolution.Week)]
    [DataRow(Resolution.Month)]
    public void Update_ConcurrentPriceChangeResultsInConsistentDataFeed_Success(Resolution resolution)
    {
      // - generate large set of test data without persisting it 
      // - kick off two threads that will update the data manager with the SAME data each using three phases to update the data
      //   - first phase is just output the data sequentially
      //   - second phase is to output the data using a random order
      //   - third pahse is switch again to sequential output
      // - retrieve a DataFeed over the given set of test data
      m_generalConfiguration[IConfigurationService.GeneralConfiguration.TimeZone] = IConfigurationService.TimeZone.UTC;
      createTestDataNoPersist(DateTime.Now.ToUniversalTime(), 10000);    //increase this bar count and the totals used below to increase the test time and potential update conflicts

      (IInstrument, IDataManagerService, IDataStoreService.BarData, Resolution, int, int, int) task1Parameters = new(m_instrument, m_dataManager, m_testBarData[resolution], resolution, 5000, 2500, 2500);
      (IInstrument, IDataManagerService, IDataStoreService.BarData, Resolution, int, int, int) task2Parameters = new(m_instrument, m_dataManager, m_testBarData[resolution], resolution, 3000, 4000, 3000);
      Task task1 = new(persistPriceData, task1Parameters);
      Task task2 = new(persistPriceData, task2Parameters);
      task1.Start();
      task2.Start();

      Task.WaitAll(task1, task2);

      int expectedBarCount;
      DateTime[] expectedDateTime;
      double[] expectedOpen;
      double[] expectedHigh;
      double[] expectedLow;
      double[] expectedClose;
      long[] expectedVolume;

      (expectedBarCount, expectedDateTime, expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) = mergeBarTestData(resolution, 10000, 1);

      Data.DataFeed dataFeed = m_dataManager.GetDataFeed(m_instrument, resolution, 1, m_fromDateTime, m_toDateTime, ToDateMode.Pinned, PriceDataType.Both);

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
    public void Delete_CountryVerifyDataRemoved_Success()
    {
      Assert.AreEqual(1, m_dataManager.Countries.Count, "Country not added to data manager");

      m_dataManager.Delete(m_country);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(0, m_dataManager.Countries.Count, "Country not removed from data manager");

      m_dataManager.Refresh();

      Assert.AreEqual(0, m_dataManager.Countries.Count, "POST-REFRESH: Country not removed from data manager");
      Assert.AreEqual(0, m_dataManager.Exchanges.Count, "POST-REFRESH: Exchange associated with country not removed from data manager");
    }

    [TestMethod]
    public void Delete_CountryHolidayVerifyDataRemoved_Success()
    {
      Holiday holiday = (Holiday)m_dataManager.Create(m_country, "Martin Luther King Jr Day", Months.January, DayOfWeek.Monday, WeekOfMonth.Third, MoveWeekendHoliday.NextBusinessDay);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(1, m_country.Holidays.Count, "Country holiday not added to country");
  
      m_dataManager.Delete(holiday);

      Assert.AreEqual(2, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(0, m_country.Holidays.Count, "Country holiday not removed from country");

      m_dataManager.Refresh();

      Country countryAfterRefresh = (Country)m_dataManager.Countries.First(x => x.Id == m_country.Id);
      Assert.AreEqual(0, countryAfterRefresh.Holidays.Count, "POST-REFRESH: Country holiday not removed from country");
    }

    [TestMethod]
    public void Delete_ExchangeHolidayVerifyDataRemoved_Success()
    {
      ExchangeHoliday holiday = (ExchangeHoliday)m_dataManager.Create(m_exchange, "Exchange Day of Month", Months.December, 26, MoveWeekendHoliday.PreviousBusinessDay);

      Assert.AreEqual(1, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(1, m_exchange.Holidays.Count, "Exchange holiday count is incorrect");

      m_dataManager.Delete(holiday);

      Assert.AreEqual(2, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(0, m_exchange.Holidays.Count, "Exchange holiday count is incorrect after delete");

      m_dataManager.Refresh();

      Exchange exchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == m_exchange.Id);
      Assert.AreEqual(0, exchangeAfterRefresh.Holidays.Count, "POST-REFRESH: Exchange holiday count is incorrect after delete");
    }

    [TestMethod]
    public void Delete_ExchangeVerifyDataRemoved_Success()
    {
      ExchangeHoliday holiday = (ExchangeHoliday)m_dataManager.Create(m_exchange, "Exchange Day of Month", Months.December, 26, MoveWeekendHoliday.PreviousBusinessDay);
      Session preMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Pre-market", new TimeOnly(6, 0), new TimeOnly(9, 30));
      Session market = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Market", new TimeOnly(9, 30), new TimeOnly(16, 0));
      Session postMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Post-market", new TimeOnly(16, 0), new TimeOnly(21, 0));

      Assert.AreEqual(4, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Exchanges.Count, "Exchange count is incorrect");
      Assert.AreEqual(1, m_country.Exchanges.Count, "Exchange count on country is incorrect");
      Assert.AreEqual(1, m_exchange.Holidays.Count, "Exchange holiday count is incorrect");
      Assert.AreEqual(3, m_exchange.Sessions[DayOfWeek.Monday].Count, "Session count is incorrect");

      m_dataManager.Delete(m_exchange);

      Assert.AreEqual(5, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(0, m_dataManager.Exchanges.Count, "Exchange count on data manager is incorrect after delete");
      Assert.AreEqual(0, m_country.Exchanges.Count, "Exchange count on country is incorrect after delete");

      m_dataManager.Refresh();

      Country countryAfterRefresh = (Country)m_dataManager.Countries.First(x => x.Id == m_country.Id);
      Assert.AreEqual(0, m_dataManager.Exchanges.Count, "POST-REFRESH: Exchange count is incorrect after refresh");
      Assert.AreEqual(0, countryAfterRefresh.Exchanges.Count, "POST-REFRESH: Exchange count on country is incorrect after refresh");
    }

    [TestMethod]
    public void Delete_SessionVerifyDataRemoved_Success()
    {
      Session preMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Pre-market", new TimeOnly(6, 0), new TimeOnly(9, 30));
      Session market = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Market", new TimeOnly(9, 30), new TimeOnly(16, 0));
      Session postMarket = (Session)m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Post-market", new TimeOnly(16, 0), new TimeOnly(21, 0));

      Assert.AreEqual(3, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(3, m_exchange.Sessions[DayOfWeek.Monday].Count, "Session count is incorrect");

      m_dataManager.Delete(market);

      Assert.AreEqual(4, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(2, m_exchange.Sessions[DayOfWeek.Monday].Count, "Session count is incorrect");

      m_dataManager.Refresh();

      Exchange exchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == m_exchange.Id);
      
      Assert.AreEqual(exchangeAfterRefresh.Sessions[DayOfWeek.Monday].Count, 2, "POST-REFRESH: Session count is incorrect");
      Assert.IsNotNull(exchangeAfterRefresh.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x.Id == preMarket.Id), "POST-REFRESH: Pre-market session not found");
      Assert.IsNotNull(exchangeAfterRefresh.Sessions[DayOfWeek.Monday].FirstOrDefault(x => x.Id == postMarket.Id), "POST-REFRESH: Post-market session not found");
    }

    [TestMethod]
    public void Delete_InstrumentVerifyDataRemoved_Success()
    {
      Instrument additionalInstrument = (Instrument)m_dataManager.Create(m_exchange, InstrumentType.Stock, "TEST2", "Test2Instrument", "Test2InstrumentDescription", m_instrumentInceptionDate);

      Exchange secondaryExchange = (Exchange)m_dataManager.Create(m_country, "SecondaryExchange", m_timeZone);
      m_dataManager.CreateSecondaryExchange(m_instrument, secondaryExchange);

      Assert.AreEqual(3, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(2, m_dataManager.Instruments.Count, "Additional instrument not created on data manager");
      Assert.AreEqual(2, m_exchange.Instruments.Count, "Additional instrument not created on primary exchange");
      Assert.AreEqual(1, secondaryExchange.Instruments.Count, "Additional instrument not created on secondary exchange");

      Exchange primaryExchange = (Exchange)m_instrument.PrimaryExchange;

      m_dataManager.Delete(m_instrument);

      Assert.AreEqual(4, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Instruments.Count, "POST-DELETE: Instrument not removed from data manager");
      Assert.AreEqual(1, primaryExchange.Instruments.Count, "POST-DELETE: Instrument not removed from primary exchange");
      Assert.AreEqual(0, secondaryExchange.Instruments.Count, "POST-DELETE: Instrument not removed from secondary exchange");

      m_dataManager.Refresh();

      Exchange primaryExchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == primaryExchange.Id);
      Exchange secondaryExchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == secondaryExchange.Id);
      Assert.IsNotNull(m_dataManager.Instruments.FirstOrDefault(x => x.Id == additionalInstrument.Id), "POST-REFRESH: Additional instrument not loaded");

      Assert.AreEqual(1, m_dataManager.Instruments.Count, "POST-REFRESH: Instrument not removed from data manager");
      Assert.AreEqual(1, primaryExchangeAfterRefresh.Instruments.Count, "POST-REFRESH: Instrument not removed from primary exchange");
      Assert.AreEqual(0, secondaryExchangeAfterRefresh.Instruments.Count, "POST-REFRESH: Instrument not removed from secondary exchange");
    }

    [TestMethod]
    public void Delete_InstrumentRemoveFromSecondaryExchange_Success()
    {
      Exchange secondaryExchange = (Exchange)m_dataManager.Create(m_country, "SecondaryExchange", m_timeZone);
      m_dataManager.CreateSecondaryExchange(m_instrument, secondaryExchange);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Instruments.Count, "Instrument not created on data manager");
      Assert.AreEqual(1, m_exchange.Instruments.Count, "Instrument not created on primary exchange");
      Assert.IsNotNull(m_exchange.Instruments.Values.FirstOrDefault(x => x.Id == m_instrument.Id), "Instrument not added to primary exchange");
      Assert.AreEqual(1, secondaryExchange.Instruments.Count, "Instrument not created on secondary exchange");
      Assert.IsNotNull(secondaryExchange.Instruments.Values.FirstOrDefault(x => x.Id == m_instrument.Id), "Instrument not added to secondary exchange");

      Exchange primaryExchangeBeforeDelete = (Exchange)m_dataManager.Exchanges.First(x => x.Id == m_exchange.Id);
      Exchange secondaryExchangeBeforeDelete = (Exchange)m_dataManager.Exchanges.First(x => x.Id == secondaryExchange.Id);
      Assert.AreEqual(m_exchange, primaryExchangeBeforeDelete, "DEEP-COPY-ERROR: Primary exchange is not the same on data manager, debug for Refresh errors that would cause new instance of objects to be created");
      Assert.AreEqual(1, primaryExchangeBeforeDelete.Instruments.Count, "DEEP-COPY-ERROR: Primary exchange returned by data manager is not the same as the object instance returned by Create method, debug for Refresh errors");
      Assert.AreEqual(secondaryExchange, secondaryExchangeBeforeDelete, "DEEP-COPY-ERROR: Secondary exchange is not the same on data manager, debug for Refresh errors");
      Assert.AreEqual(1, secondaryExchangeBeforeDelete.Instruments.Count, "DEEP-COPY-ERROR: Secondary exchange returned by data manager is not the same as the object instance returned by Create method, debug for Refresh errors");

      m_dataManager.DeleteSecondaryExchange(m_instrument, secondaryExchange);

      Assert.AreEqual(3, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Instruments.Count, "POST-DELETE: Instrument count not correct on data manager");
      Assert.AreEqual(1, m_exchange.Instruments.Count, "POST-DELETE: Instrument count not correct on primary exchange");
      Assert.IsNotNull(m_exchange.Instruments.Values.FirstOrDefault(x => x.Id == m_instrument.Id), "POST-DELETE: Instrument not added to primary exchange");
      Assert.AreEqual(0, secondaryExchange.Instruments.Count, "POST-DELETE: Instrument not removed from secondary exchange");

      m_dataManager.Refresh();

      Exchange primaryExchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == m_exchange.Id);
      Exchange secondaryExchangeAfterRefresh = (Exchange)m_dataManager.Exchanges.First(x => x.Id == secondaryExchange.Id);
      Assert.AreEqual(1, m_dataManager.Instruments.Count, "POST-REFRESH: Instrument not correct on data manager");
      Assert.AreEqual(1, primaryExchangeAfterRefresh.Instruments.Count, "POST-REFRESH: Instrument not created on primary exchange");
      Assert.AreEqual(0, secondaryExchangeAfterRefresh.Instruments.Count, "POST-REFRESH: Instrument not removed from secondary exchange");
    }

    [TestMethod]
    public void Delete_CountryFundamentalVerifyAssociationRemoved_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("GDP", "Gross Domestic Product", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);
      CountryFundamental countryFundamental = (CountryFundamental)m_dataManager.Create(fundamental, m_country);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_country.Fundamentals.FirstOrDefault(x => x == countryFundamental), "Country fundamental not added to country object");

      m_dataManager.Delete(countryFundamental);

      Assert.AreEqual(3, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.IsNull(m_country.Fundamentals.FirstOrDefault(x => x == countryFundamental), "Country fundamental not removed from country object");

      m_dataManager.Refresh();

      Country countryAfterRefresh = (Country)m_dataManager.Countries.First(x => x.Id == m_country.Id);

      Assert.AreEqual(0, countryAfterRefresh.Fundamentals.Count, "POST-REFRESH: Fundamental country not zero for country");
      Assert.IsNull(countryAfterRefresh.Fundamentals.FirstOrDefault(x => x.Fundamental.Id == countryFundamental.Fundamental.Id), "POST-REFRESH: Fundamental not removed from country");
    }

    [TestMethod]
    public void Delete_CountryFundamentalAndRelatedDataRemoved_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("GDP", "Gross Domestic Product", FundamentalCategory.Country, FundamentalReleaseInterval.Monthly);
      CountryFundamental countryFundamental = (CountryFundamental)m_dataManager.Create(fundamental, m_country);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_country.Fundamentals.FirstOrDefault(x => x == countryFundamental), "Country fundamental not added to country object");

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value1 = 123.45;
      m_dataManager.Update(countryFundamental, dateTime, value1);
      double value2 = 67.89;
      m_dataManager.Update(countryFundamental, dateTime.AddMonths(1), value2);

      Assert.AreEqual(2, m_modelChangeObserverCount, "POST-UPDATE: Model change observer count is not correct");
      Assert.AreEqual(2, m_fundamentalChangeObserverCount, "POST-UPDATE: Fundamental change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "Fundamental not created on data manager");
      Assert.AreEqual(1, m_country.Fundamentals.Count, "Fundamental not created on country");
      Assert.AreEqual(2, countryFundamental.Values.Count, "Country fundamental do not reflect values added");
      Assert.AreEqual(value1, (double)countryFundamental.Values[dateTime], "Country fundamental value 1 is not correct");
      Assert.AreEqual(value2, (double)countryFundamental.Values[dateTime.AddMonths(1)], "Country fundamental value 2 is not correct");

      m_dataManager.Delete(countryFundamental);

      Assert.AreEqual(3, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(0, m_country.Fundamentals.Count, "Fundamental not removed from country");

      m_dataManager.Refresh();

      Country countryAfterRefresh = (Country)m_dataManager.Countries.First(x => x.Id == m_country.Id);
      Assert.AreEqual(0, countryAfterRefresh.Fundamentals.Count, "POST-REFRESH: Fundamental country not zero for country");
      Assert.IsNull(countryAfterRefresh.Fundamentals.FirstOrDefault(x => x.Fundamental.Id == countryFundamental.Fundamental.Id), "POST-REFRESH: Fundamental not removed from country");
    }

    [TestMethod]
    public void Delete_InstrumentFundamentalVerifyAssociationRemoved_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("Cashflow", "Cashflow", FundamentalCategory.Instrument, FundamentalReleaseInterval.Monthly);
      InstrumentFundamental instrumentFundamental = (InstrumentFundamental)m_dataManager.Create(fundamental, m_instrument);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_instrument.Fundamentals.FirstOrDefault(x => x == instrumentFundamental), "Instrument fundamental not added to instrument object");

      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "Fundamental not created on data manager");
      Assert.AreEqual(1, m_instrument.Fundamentals.Count, "Fundamental not created on instrument");

      m_dataManager.Delete(instrumentFundamental);

      Assert.AreEqual(3, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "POST-DELETE: Fundamental definition removed from data manager");
      Assert.AreEqual(0, m_instrument.Fundamentals.Count, "POST-DELETE: Fundamental not removed from instrument");

      m_dataManager.Refresh();

      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "POST-REFRESH: Fundamental definition removed from data manager");

      Instrument intrumentAfterRefresh = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);
      Assert.AreEqual(0, intrumentAfterRefresh.Fundamentals.Count, "POST-REFRESH: Fundamental instrument not zero for instrument");
      Assert.IsNull(intrumentAfterRefresh.Fundamentals.FirstOrDefault(x => x.Fundamental.Id == instrumentFundamental.Fundamental.Id), "POST-REFRESH: Fundamental not removed from instrument");
    }

    [TestMethod]
    public void Delete_InstrumentFundamentalAndRelatedDataRemoved_Success()
    {
      Fundamental fundamental = (Fundamental)m_dataManager.Create("Cashflow", "Cashflow", FundamentalCategory.Instrument, FundamentalReleaseInterval.Monthly);
      InstrumentFundamental instrumentFundamental = (InstrumentFundamental)m_dataManager.Create(fundamental, m_instrument);

      Assert.AreEqual(2, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.IsNotNull(m_instrument.Fundamentals.FirstOrDefault(x => x == instrumentFundamental), "Instrument fundamental not added to instrument object");

      DateTime dateTime = DateTime.Now.ToUniversalTime();
      double value1 = 678.99;
      m_dataManager.Update(instrumentFundamental, dateTime, value1);
      double value2 = 123.45;
      m_dataManager.Update(instrumentFundamental, dateTime.AddMonths(1), value2);

      Assert.AreEqual(2, m_modelChangeObserverCount, "POST-UPDATE: Model change observer count is not correct");
      Assert.AreEqual(2, m_fundamentalChangeObserverCount, "POST-UPDATE: Fundamental change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "Fundamental not created on data manager");
      Assert.AreEqual(2, instrumentFundamental.Values.Count, "Instrument fundamental do not reflect values added");
      Assert.AreEqual(value1, (double)instrumentFundamental.Values[dateTime], "Instrument fundamental value 1 is not correct");
      Assert.AreEqual(value2, (double)instrumentFundamental.Values[dateTime.AddMonths(1)], "Instrument fundamental value 2 is not correct");

      m_dataManager.Delete(instrumentFundamental);

      Assert.AreEqual(3, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");
      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "POST-DELETE: Fundamental definition removed from data manager");
      Assert.AreEqual(0, m_instrument.Fundamentals.Count, "POST-DELETE: Instrument fundamental not removed from instrument");

      m_dataManager.Refresh();

      Assert.AreEqual(1, m_dataManager.Fundamentals.Count, "POST-REFRESH: Fundamental definition not loaded into data manager");

      Instrument intrumentAfterRefresh = (Instrument)m_dataManager.Instruments.First(x => x.Id == m_instrument.Id);

      Assert.AreEqual(0, intrumentAfterRefresh.Fundamentals.Count, "POST-REFRESH: Fundamental instrument not zero for instrument");
      Assert.IsNull(intrumentAfterRefresh.Fundamentals.FirstOrDefault(x => x.Fundamental.Id == instrumentFundamental.Fundamental.Id), "POST-REFRESH: Fundamental not removed from instrument");
    }

    [TestMethod]
    public void Delete_CountryAndRelatedObjectsRemoved_Success()
    {
      m_dataManager.Create(m_country, "Martin Luther King Jr Day", Months.January, DayOfWeek.Monday, WeekOfMonth.Third, MoveWeekendHoliday.NextBusinessDay);
      m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Pre-market", new TimeOnly(6, 0), new TimeOnly(9, 30));
      m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Market", new TimeOnly(9, 30), new TimeOnly(16, 0));
      m_dataManager.Create(m_exchange, DayOfWeek.Monday, "Post-market", new TimeOnly(16, 0), new TimeOnly(21, 0));
      m_dataManager.Create(m_exchange, InstrumentType.Stock, "TEST2", "Test2Instrument", "Test2InstrumentDescription", m_instrumentInceptionDate);

      Assert.AreEqual(5, m_modelChangeObserverCount, "Model change observer count is not correct");
      Assert.AreEqual(1, m_country.Holidays.Count, "Country holiday not added to country");
      Assert.AreEqual(1, m_country.Exchanges.Count, "Exchange not added to country");
      Assert.AreEqual(3, m_exchange.Sessions[DayOfWeek.Monday].Count, "Sessions not added to exchange");

      m_dataManager.Delete(m_country);

      Assert.AreEqual(6, m_modelChangeObserverCount, "POST-DELETE: Model change observer count is not correct");

      m_dataManager.Refresh();

      Assert.AreEqual(0, m_dataManager.Countries.Count, "POST-REFRESH: Country not removed from data manager");
      Assert.AreEqual(0, m_dataManager.Exchanges.Count, "POST-REFRESH: Exchange associated with country not removed from data manager");
    }
  }
}
