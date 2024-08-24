using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.Testing.Services
{
  [TestClass]
  public class InstrumentBarDataService
  {
    //constants
    public static readonly DateTime StartDate = new DateTime(2024, 8, 13, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime EndDate = new DateTime(2024, 11, 15, 23, 59, 59, DateTimeKind.Utc);

    public static readonly List<IBarData> DialyBars = new List<IBarData>()
    {
      new BarData(Resolution.Days, new DateTime(2024, 8, 13, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.03, 28.18, 27.48, 27.55, 54529513),
      new BarData(Resolution.Days, new DateTime(2024, 8, 14, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.66, 28.11, 27.59, 27.84, 37921893),
      new BarData(Resolution.Days, new DateTime(2024, 8, 15, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.48, 28.4 , 26.7 , 28.34, 55655033),
      new BarData(Resolution.Days, new DateTime(2024, 8, 16, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.37, 28.61, 28.33, 28.36, 41445449),
      new BarData(Resolution.Days, new DateTime(2024, 8, 19, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.41, 28.95, 28.32, 28.9 , 51687185),
      new BarData(Resolution.Days, new DateTime(2024, 8, 20, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.76, 28.95, 28.47, 28.5 , 47232034),
      new BarData(Resolution.Days, new DateTime(2024, 8, 21, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.49, 29.51, 28.42, 29.5 , 67142172),
      new BarData(Resolution.Days, new DateTime(2024, 8, 22, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.76, 29.97, 29.52, 29.96, 91308218),
      new BarData(Resolution.Days, new DateTime(2024, 8, 23, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.39, 30 , 28.81, 29.07, 65680921),
      new BarData(Resolution.Days, new DateTime(2024, 8, 26, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.12, 29.71, 28.88, 29.6 , 58357896),
      new BarData(Resolution.Days, new DateTime(2024, 8, 27, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.61, 29.7 , 28.419 , 28.46, 66468431),
      new BarData(Resolution.Days, new DateTime(2024, 8, 28, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.47, 28.9597, 28.22, 28.24, 56163406),
      new BarData(Resolution.Days, new DateTime(2024, 8, 29, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.27, 28.78, 28.14, 28.19, 49763550),
      new BarData(Resolution.Days, new DateTime(2024, 8, 30, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.41, 28.91, 28.1 , 28.83, 47334102),
      new BarData(Resolution.Days, new DateTime(2024, 9,  2, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.59, 28.61, 27.75, 27.8 , 62853492),
      new BarData(Resolution.Days, new DateTime(2024, 9,  3, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.03, 28.63, 27.81, 28.52, 59154078),
      new BarData(Resolution.Days, new DateTime(2024, 9,  4, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.45, 28.75, 28.19, 28.5 , 38387640),
      new BarData(Resolution.Days, new DateTime(2024, 9,  5, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.16, 29.46, 28.93, 29.08, 57309025),
      new BarData(Resolution.Days, new DateTime(2024, 9,  6, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.15, 29.3 , 28.91, 29.19, 34032131),
      new BarData(Resolution.Days, new DateTime(2024, 9,  9, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.01, 29.372 , 28.92, 29.14, 52274444),
      new BarData(Resolution.Days, new DateTime(2024, 9, 10, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.36, 29.36, 28.68, 28.82, 46922429),
      new BarData(Resolution.Days, new DateTime(2024, 9, 11, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.22, 29.3496, 28.8 , 28.94, 62534308),
      new BarData(Resolution.Days, new DateTime(2024, 9, 12, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.91, 29.2 , 28.67, 28.91, 37576509),
      new BarData(Resolution.Days, new DateTime(2024, 9, 13, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.98, 29.1 , 28.5 , 28.78, 39328197),
      new BarData(Resolution.Days, new DateTime(2024, 9, 16, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.66, 28.77, 28.48, 28.68, 34758711),
      new BarData(Resolution.Days, new DateTime(2024, 9, 17, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.2 , 29.26, 28.7 , 29.07, 58210502),
      new BarData(Resolution.Days, new DateTime(2024, 9, 18, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.91, 29.31, 28.8 , 29.23, 41936253),
      new BarData(Resolution.Days, new DateTime(2024, 9, 19, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.28, 29.29, 28.8 , 28.93, 49537591),
      new BarData(Resolution.Days, new DateTime(2024, 9, 20, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.95, 29.37, 28.8 , 29.35, 38779583),
      new BarData(Resolution.Days, new DateTime(2024, 9, 23, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.35, 29.43, 29.17, 29.353 , 43469965),
      new BarData(Resolution.Days, new DateTime(2024, 9, 24, 21, 0, 0, DateTimeKind.Utc),"0.0000",29.03, 29.21, 28.8 , 28.89, 48788864),
      new BarData(Resolution.Days, new DateTime(2024, 9, 25, 21, 0, 0, DateTimeKind.Utc),"0.0000",28.72, 29.08, 28.1 , 28.91, 66821907),
      new BarData(Resolution.Days, new DateTime(2024, 9, 26, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.27, 27.4 , 26.42, 26.61, 209881393),
      new BarData(Resolution.Days, new DateTime(2024, 9, 27, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.91, 27.1 , 26.82, 26.91, 65620518),
      new BarData(Resolution.Days, new DateTime(2024, 9, 30, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.09, 27.22, 26.88, 27.2 , 71726961),
      new BarData(Resolution.Days, new DateTime(2024,10,  1, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.16, 27.25, 26.66, 26.74, 73484062),
      new BarData(Resolution.Days, new DateTime(2024,10,  2, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.01, 27.04, 25.91, 26.12, 99032973),
      new BarData(Resolution.Days, new DateTime(2024,10,  3, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.37, 26.44, 26.11, 26.14, 69656588),
      new BarData(Resolution.Days, new DateTime(2024,10,  4, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.35, 26.75, 26.29, 26.68, 57609027),
      new BarData(Resolution.Days, new DateTime(2024,10,  7, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.59, 26.62, 26.01, 26.07, 83974196),
      new BarData(Resolution.Days, new DateTime(2024,10,  8, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.15, 26.32, 26 , 26.1 , 61509721),
      new BarData(Resolution.Days, new DateTime(2024,10,  9, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.26, 26.3 , 26 , 26.23, 68200694),
      new BarData(Resolution.Days, new DateTime(2024,10, 10, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.38, 26.49, 26.03, 26.1 , 57861437),
      new BarData(Resolution.Days, new DateTime(2024,10, 11, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.12, 26.23, 26 , 26.001 , 54271351),
      new BarData(Resolution.Days, new DateTime(2024,10, 14, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.01, 26.076 , 25.67, 25.8 , 64939150),
      new BarData(Resolution.Days, new DateTime(2024,10, 15, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.85, 26.14, 25.6 , 25.98, 76102480),
      new BarData(Resolution.Days, new DateTime(2024,10, 16, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.86, 25.93, 25.45, 25.69, 79125813),
      new BarData(Resolution.Days, new DateTime(2024,10, 17, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.7 , 26.02, 25.44, 25.5 , 83129921),
      new BarData(Resolution.Days, new DateTime(2024,10, 18, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.39, 25.44, 24.84, 25.15, 103641080),
      new BarData(Resolution.Days, new DateTime(2024,10, 21, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.33, 25.84, 25.12, 25.15, 109657527),
      new BarData(Resolution.Days, new DateTime(2024,10, 22, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.29, 25.54, 25.17, 25.35, 78619921),
      new BarData(Resolution.Days, new DateTime(2024,10, 23, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.17, 25.631 , 25.08, 25.1 , 106575933),
      new BarData(Resolution.Days, new DateTime(2024,10, 24, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.33, 25.38, 25.08, 25.11, 70405065),
      new BarData(Resolution.Days, new DateTime(2024,10, 25, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.33, 25.81, 25.28, 25.73, 99940165),
      new BarData(Resolution.Days, new DateTime(2024,10, 28, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.87, 25.95, 25.38, 25.4 , 85768602),
      new BarData(Resolution.Days, new DateTime(2024,10, 29, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.61, 25.63, 25.32, 25.45, 68949266),
      new BarData(Resolution.Days, new DateTime(2024,10, 30, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.5 , 25.75, 25.4 , 25.71, 33404159),
      new BarData(Resolution.Days, new DateTime(2024,10, 31, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.9 , 26.21, 25.62, 25.84, 102388856),
      new BarData(Resolution.Days, new DateTime(2024,11,  1, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.95, 26.09, 25.61, 25.66, 84775982),
      new BarData(Resolution.Days, new DateTime(2024,11,  4, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.82, 26.07, 25.62, 25.67, 93094525),
      new BarData(Resolution.Days, new DateTime(2024,11,  5, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.72, 26.23, 25.66, 26.2 , 86977995),
      new BarData(Resolution.Days, new DateTime(2024,11,  6, 21, 0, 0, DateTimeKind.Utc),"0.0000",25.96, 26.48, 25.919 , 25.98, 95832260),
      new BarData(Resolution.Days, new DateTime(2024,11,  7, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.12, 26.34, 25.81, 26.24, 90366833),
      new BarData(Resolution.Days, new DateTime(2024,11,  8, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.44, 26.61, 26.25, 26.38, 103433807),
      new BarData(Resolution.Days, new DateTime(2024,11, 11, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.45, 26.63, 26.38, 26.59, 82713519),
      new BarData(Resolution.Days, new DateTime(2024,11, 12, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.59, 26.77, 26.28, 26.61, 79032987),
      new BarData(Resolution.Days, new DateTime(2024,11, 13, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.69, 26.81, 26.5 , 26.65, 69192858),
      new BarData(Resolution.Days, new DateTime(2024,11, 14, 21, 0, 0, DateTimeKind.Utc),"0.0000",27.05, 27.1 , 26.68, 26.74, 88554543),
      new BarData(Resolution.Days, new DateTime(2024,11, 15, 21, 0, 0, DateTimeKind.Utc),"0.0000",26.83, 27.16, 26.77, 27.06, 73899242)
    };

    public static readonly List<IBarData> WeeklyBars = new List<IBarData>()
    {
      new BarData(Resolution.Weeks, new DateTime(2024, 8, 16, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.03, 28.61, 26.70, 28.36, 189551888),
      new BarData(Resolution.Weeks, new DateTime(2024, 8, 23, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.41, 30.00, 28.32, 29.07, 323050530),
      new BarData(Resolution.Weeks, new DateTime(2024, 8, 30, 21, 0, 0, DateTimeKind.Utc), "0.0000", 29.12, 29.71, 28.1, 28.83, 278087385),
      new BarData(Resolution.Weeks, new DateTime(2024, 9,  6, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.59, 29.46, 27.75, 29.19, 251736366),
      new BarData(Resolution.Weeks, new DateTime(2024, 9, 13, 21, 0, 0, DateTimeKind.Utc), "0.0000", 29.01, 29.372, 28.5, 28.78, 238635887),
      new BarData(Resolution.Weeks, new DateTime(2024, 9, 20, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.66, 29.37, 28.48, 29.35, 223222640),
      new BarData(Resolution.Weeks, new DateTime(2024, 9, 27, 21, 0, 0, DateTimeKind.Utc), "0.0000", 29.35, 29.43, 26.42, 26.91, 434582647),      
      new BarData(Resolution.Weeks, new DateTime(2024, 10, 4, 21, 0, 0, DateTimeKind.Utc), "0.0000", 27.09, 27.25, 25.91, 26.68, 371509611),
      new BarData(Resolution.Weeks, new DateTime(2024, 10,11, 21, 0, 0, DateTimeKind.Utc), "0.0000", 26.59, 26.62, 26.00, 26.001, 325817399),
      new BarData(Resolution.Weeks, new DateTime(2024, 10,18, 21, 0, 0, DateTimeKind.Utc), "0.0000", 26.01, 26.14, 24.84, 25.15, 406938444),
      new BarData(Resolution.Weeks, new DateTime(2024, 10,25, 21, 0, 0, DateTimeKind.Utc), "0.0000", 25.33, 25.84, 25.08, 25.73, 465198611),
      new BarData(Resolution.Weeks, new DateTime(2024, 11, 1, 21, 0, 0, DateTimeKind.Utc), "0.0000", 25.87, 26.21, 25.32, 25.66, 375286865),
      new BarData(Resolution.Weeks, new DateTime(2024, 11, 8, 21, 0, 0, DateTimeKind.Utc), "0.0000", 25.82, 26.61, 25.62, 26.38, 469705420),
      new BarData(Resolution.Weeks, new DateTime(2024, 11, 15, 21, 0, 0, DateTimeKind.Utc), "0.0000", 26.45, 27.16, 26.28, 27.06, 393393149)
    };

    //NOTE: Copying data to the month timeframe should be copied from the days data since the end of the last week of a month will not correctly align
    //      with the end of the month (e.g. last Friday is not always the end of the month).
    public static readonly List<IBarData> ExistingMonthlyBars = new List<IBarData>()    //used to test against partial month data existing that needs to be removed
    {
      new BarData(Resolution.Months, new DateTime(2024, 8, 30, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.03, 30.00, 26.70, 28.83, 790689803),
      new BarData(Resolution.Months, new DateTime(2024, 9, 12, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.59, 29.46, 27.75, 28.91, 451044056),
    };
    public static readonly List<IBarData> MonthlyBars = new List<IBarData>()
    {
      new BarData(Resolution.Months, new DateTime(2024, 8, 30, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.03, 30.00, 26.70, 28.83, 790689803),
      new BarData(Resolution.Months, new DateTime(2024, 9, 30, 21, 0, 0, DateTimeKind.Utc), "0.0000", 28.59, 29.46, 26.42, 27.2, 1219904501),
      new BarData(Resolution.Months, new DateTime(2024, 10,31, 21, 0, 0, DateTimeKind.Utc), "0.0000", 27.16, 27.25, 24.84, 25.84, 1788247987),
      new BarData(Resolution.Months, new DateTime(2024, 11,15, 21, 0, 0, DateTimeKind.Utc), "0.0000", 25.95, 27.16, 25.61, 27.06, 947874551)
    };

    //enums


    //types


    //attributes
    private CoreUI.Services.InstrumentBarDataService m_instrumentBarDataService;
    private Mock<IApplication> m_application;
    private Mock<IServiceProvider> m_serviceProvider;
    private Mock<IInstrumentBarDataRepository> m_barDataRepository;
    private Mock<IInstrumentBarDataRepository> m_fromBarDataRepository;
    private Mock<IInstrumentBarDataRepository> m_toBarDataRepository;
    private Mock<ILogger<CoreUI.Services.InstrumentBarDataService>> m_logger;
    private Mock<IDatabase> m_database;
    private Mock<IConfigurationService> m_configurationService;
    private Mock<IDialogService> m_dialogService;
    private Mock<IExchangeService> m_exchangeService;
    private Country m_country;
    private Exchange m_nasdaq;
    private TimeZoneInfo m_timeZone;
    private Stock m_aapl;

    //properties


    //constructors
    public InstrumentBarDataService()
    {
      m_logger = new Mock<ILogger<CoreUI.Services.InstrumentBarDataService>>();

      m_country = new Country(Guid.Parse("11111111-0000-0000-0000-000000000001"), Country.DefaultAttributes, TagValue.EmptyJson, "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_nasdaq = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000002"), Exchange.DefaultAttributes, TagValue.EmptyJson, m_country.Id, "Nasdaq", Array.Empty<string>(), m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);
      m_aapl = new Stock("AAPL", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "AAPL^C", "AAPL~C" }, "Apple", "Apple Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nasdaq.Id, Array.Empty<Guid>(), string.Empty);
      m_aapl.MarketCap = 200000;

      m_barDataRepository = new Mock<IInstrumentBarDataRepository>();
      m_barDataRepository.Setup(x => x.Resolution).Returns(Resolution.Days);
      m_barDataRepository.Setup(x => x.Instrument).Returns(m_aapl);
      m_barDataRepository.Setup(x => x.GetItems(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns((IList<IBarData>)DialyBars);
      m_fromBarDataRepository = new Mock<IInstrumentBarDataRepository>();
      m_fromBarDataRepository.Setup(x => x.Instrument).Returns(m_aapl);
      m_toBarDataRepository = new Mock<IInstrumentBarDataRepository>();
      m_toBarDataRepository.Setup(x => x.Instrument).Returns(m_aapl);

      m_database = new Mock<IDatabase>();
      m_database.Setup(x => x.GetExchange(It.IsAny<Guid>())).Returns(m_nasdaq);
      m_configurationService = new Mock<IConfigurationService>();
      m_configurationService.Setup(x => x.General[IConfigurationService.GeneralConfiguration.TimeZone]).Returns(IConfigurationService.TimeZone.UTC);
      m_dialogService = new Mock<IDialogService>();
      m_exchangeService = new Mock<IExchangeService>();
      m_application = new Mock<IApplication>();
      m_serviceProvider = new Mock<IServiceProvider>();
      m_application.SetupGet(x => x.Services).Returns(m_serviceProvider.Object);
      IApplication.Current = m_application.Object;

      m_instrumentBarDataService = new CoreUI.Services.InstrumentBarDataService(m_barDataRepository.Object, m_logger.Object, m_database.Object, m_configurationService.Object, m_exchangeService.Object, m_dialogService.Object);      
    }

    ~InstrumentBarDataService()
    {

    }

    //finalizers


    //methods
    [TestMethod]
    public void Copy_DaysToWeeks_Success()
    {
      List<IBarData>? result = null;
      m_fromBarDataRepository.Setup(x => x.GetItems(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns((IList<IBarData>)DialyBars);
      m_toBarDataRepository.Setup(x => x.Update(It.IsAny<IList<IBarData>>())).Callback((IList<IBarData> bars) => result = (List<IBarData>)bars);
      m_toBarDataRepository.SetupGet(x => x.Resolution).Returns(Resolution.Weeks);
      m_serviceProvider.SetupSequence(m_serviceProvider => m_serviceProvider.GetService(typeof(IInstrumentBarDataRepository)))
        .Returns(m_fromBarDataRepository.Object)
        .Returns(m_toBarDataRepository.Object);

      m_instrumentBarDataService.Copy(Resolution.Days, Resolution.Weeks, StartDate, EndDate);

      Assert.IsNotNull(result);
      Assert.AreEqual(WeeklyBars.Count, result.Count);
      for (int i = 0; i < WeeklyBars.Count; i++)
      {
        Assert.AreEqual(WeeklyBars[i].Resolution, result[i].Resolution, $"Resolution for bar {i} not correct");
        Assert.IsTrue(WeeklyBars[i].DateTime.Equals(result[i].DateTime), $"DateTime for bar {i} does not match");
        Assert.AreEqual(WeeklyBars[i].Open, result[i].Open, $"Open of bar {result[i].DateTime} not correct");
        Assert.AreEqual(WeeklyBars[i].High, result[i].High, $"High of bar {result[i].DateTime} not correct");
        Assert.AreEqual(WeeklyBars[i].Low, result[i].Low, $"Low of bar {result[i].DateTime} not correct");
        Assert.AreEqual(WeeklyBars[i].Close, result[i].Close, $"Close of bar {result[i].DateTime} not correct");
        Assert.AreEqual(WeeklyBars[i].Volume, result[i].Volume, $"Volume of bar {result[i].DateTime} not correct");
      }
    }

    [TestMethod]
    public void Copy_DaysToMonths_Success()
    {
      List<IBarData>? result = null;
      bool deleteCalled = false;
      m_fromBarDataRepository.Setup(x => x.GetItems(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns((IList<IBarData>)DialyBars);
      m_toBarDataRepository.Setup(x => x.GetItems(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns((IList<IBarData>)ExistingMonthlyBars);
      m_toBarDataRepository.Setup(x => x.Update(It.IsAny<IList<IBarData>>())).Callback((IList<IBarData> bars) => result = (List<IBarData>)bars);
      m_toBarDataRepository.SetupGet(x => x.Resolution).Returns(Resolution.Months);
      m_toBarDataRepository.Setup(x => x.Delete(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Callback(() => deleteCalled = true);

      m_serviceProvider.SetupSequence(m_serviceProvider => m_serviceProvider.GetService(typeof(IInstrumentBarDataRepository)))
        .Returns(m_fromBarDataRepository.Object)
        .Returns(m_toBarDataRepository.Object);

      m_instrumentBarDataService.Copy(Resolution.Days, Resolution.Months, StartDate, EndDate);

      Assert.IsTrue(deleteCalled, "Stale data was not removed");
      Assert.IsNotNull(result);
      Assert.AreEqual(MonthlyBars.Count, result.Count);
      for (int i = 0; i < MonthlyBars.Count; i++)
      {
        Assert.AreEqual(MonthlyBars[i].Resolution, result[i].Resolution, $"Resolution for bar {i} not correct");
        Assert.IsTrue(MonthlyBars[i].DateTime.Equals(result[i].DateTime), $"DateTime for bar {i} does not match");
        Assert.AreEqual(MonthlyBars[i].Open, result[i].Open, $"Open of bar {result[i].DateTime} not correct");
        Assert.AreEqual(MonthlyBars[i].High, result[i].High, $"High of bar {result[i].DateTime} not correct");
        Assert.AreEqual(MonthlyBars[i].Low, result[i].Low, $"Low of bar {result[i].DateTime} not correct");
        Assert.AreEqual(MonthlyBars[i].Close, result[i].Close, $"Close of bar {result[i].DateTime} not correct");
        Assert.AreEqual(MonthlyBars[i].Volume, result[i].Volume, $"Volume of bar {result[i].DateTime} not correct");
      }
    }

    [TestMethod]
    public void ExportImport_FromCsv_Success()
    {
      Mock<IFileSystemService> fileSystemService = new Mock<IFileSystemService>();
      MemoryStream writeStream = new MemoryStream();
      StreamWriter streamWriter = new StreamWriter(writeStream);
      fileSystemService.Setup(x => x.CreateText(It.IsAny<string>())).Returns(streamWriter);
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(fileSystemService.Object);

      ExportSettings exportSettings = new ExportSettings()
      {
        FromDateTime = StartDate,
        ToDateTime = EndDate,
        ReplaceBehavior = ExportReplaceBehavior.Replace,
        DateTimeTimeZone = ImportExportDataDateTimeTimeZone.UTC,
        Filename = "test.csv"
      };

      m_instrumentBarDataService.Refresh(StartDate, EndDate);
      m_instrumentBarDataService.Export(exportSettings);

      byte[] buffer = writeStream.ToArray();
      MemoryStream readStream = new MemoryStream(buffer);
      StreamReader streamReader = new StreamReader(readStream);
      fileSystemService.Setup(x => x.OpenFile(It.IsAny<string>(), It.IsAny<FileStreamOptions>())).Returns(streamReader);

      ImportSettings importSettings = new ImportSettings()
      {
        FromDateTime = StartDate,
        ToDateTime = EndDate,
        DateTimeTimeZone = ImportExportDataDateTimeTimeZone.UTC,
        Filename = "test.csv"
      };

      List<IBarData>? result = null;
      m_barDataRepository.Setup(x => x.Update(It.IsAny<IList<IBarData>>())).Callback((IList<IBarData> bars) => result = (List<IBarData>)bars);
      m_instrumentBarDataService.Import(importSettings);

      Assert.IsNotNull(result);
      Assert.AreEqual(DialyBars.Count, result.Count);

      for (int i = 0; i < DialyBars.Count; i++)
      {
        Assert.AreEqual(DialyBars[i].Resolution, result[i].Resolution);
        Assert.IsTrue(DialyBars[i].DateTime.Equals(result[i].DateTime));
        Assert.AreEqual(DialyBars[i].Open, result[i].Open, $"Open of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].High, result[i].High, $"High of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].Low, result[i].Low, $"Low of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].Close, result[i].Close, $"Close of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].Volume, result[i].Volume, $"Volume of bar {result[i].DateTime} is incorrect");
      }
    }

    [TestMethod]
    public void ExportImport_FromJson_Success()
    {
      Mock<IFileSystemService> fileSystemService = new Mock<IFileSystemService>();
      MemoryStream writeStream = new MemoryStream();
      StreamWriter streamWriter = new StreamWriter(writeStream);
      fileSystemService.Setup(x => x.CreateText(It.IsAny<string>())).Returns(streamWriter);
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(fileSystemService.Object);

      ExportSettings exportSettings = new ExportSettings()
      {
        FromDateTime = StartDate,
        ToDateTime = EndDate,
        ReplaceBehavior = ExportReplaceBehavior.Replace,
        DateTimeTimeZone = ImportExportDataDateTimeTimeZone.UTC,
        Filename = "test.json"
      };

      m_instrumentBarDataService.Refresh(StartDate, EndDate);
      m_instrumentBarDataService.Export(exportSettings);

      byte[] buffer = writeStream.ToArray();
      MemoryStream readStream = new MemoryStream(buffer);
      StreamReader streamReader = new StreamReader(readStream);
      fileSystemService.Setup(x => x.OpenFile(It.IsAny<string>(), It.IsAny<FileStreamOptions>())).Returns(streamReader);

      ImportSettings importSettings = new ImportSettings()
      {
        FromDateTime = StartDate,
        ToDateTime = EndDate,
        DateTimeTimeZone = ImportExportDataDateTimeTimeZone.UTC,
        Filename = "test.json"
      };

      List<IBarData>? result = null;
      m_barDataRepository.Setup(x => x.Update(It.IsAny<IList<IBarData>>())).Callback((IList<IBarData> bars) => result = (List<IBarData>)bars);
      m_instrumentBarDataService.Import(importSettings);

      Assert.IsNotNull(result);
      Assert.AreEqual(DialyBars.Count, result.Count);

      for (int i = 0; i < DialyBars.Count; i++)
      {
        Assert.AreEqual(DialyBars[i].Resolution, result[i].Resolution);
        Assert.IsTrue(DialyBars[i].DateTime.Equals(result[i].DateTime));
        Assert.AreEqual(DialyBars[i].Open, result[i].Open, $"Open of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].High, result[i].High, $"High of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].Low, result[i].Low, $"Low of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].Close, result[i].Close, $"Close of bar {result[i].DateTime} is incorrect");
        Assert.AreEqual(DialyBars[i].Volume, result[i].Volume, $"Volume of bar {result[i].DateTime} is incorrect");
      }
    }
  }
}
