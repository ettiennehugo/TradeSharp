using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;
using System.Diagnostics;
using TradeSharp.CoreUI.Common;
using System;
using System.Text;

namespace TradeSharp.CoreUI.Testing.Services
{
  [TestClass]
  public class InstrumentService
  {
    //enums


    //types


    //attributes
    private Mock<IApplication> m_application;
    private Mock<IServiceProvider> m_serviceProvider;
    private Mock<IFileSystemService> m_fileSystemService;
    private Mock<ILogger<CoreUI.Services.InstrumentService>> m_logger;
    private Mock<IExchangeService> m_exchangeService;
    private Mock<IInstrumentRepository> m_instrumentRepository;
    private IInstrumentCacheService m_instrumentCacheService;
    private Mock<IDialogService> m_dialogService;
    private List<Instrument> m_addedInstruments;
    private List<Instrument> m_updatedInstruments;
    private List<Instrument> m_allFileInstruments;
    private List<Instrument> m_instruments;
    private List<Instrument> m_emptyInstruments;
    private CoreUI.Services.InstrumentService m_instrumentService;
    private TimeZoneInfo m_timeZone;
    private Country m_country;
    private IList<Exchange> m_exchanges;
    private Exchange m_global;
    private Exchange m_nyse;
    private Exchange m_nasdaq;
    private Exchange m_lse;

    //constructors
    public InstrumentService()
    {
      //ensure full coverage of the service with debugging output
      Debugging.ImportExport = true;

      //create the test country, exchange, and instruments
      m_country = new Country(Guid.Parse("11111111-0000-0000-0000-000000000001"), Country.DefaultAttributes, TagValue.EmptyJson, "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_global = new Exchange(Exchange.InternationalId, Exchange.DefaultAttributes, TagValue.EmptyJson, m_country.Id, "Global", Array.Empty<string>(), m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);
      m_nyse = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000001"), Exchange.DefaultAttributes, TagValue.EmptyJson, m_country.Id, "New York Stock Exchange", new List<string> { "NYSE" }, m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);
      m_nasdaq = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000002"), Exchange.DefaultAttributes, TagValue.EmptyJson, m_country.Id, "Nasdaq", Array.Empty<string>(), m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);
      m_lse = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000003"), Exchange.DefaultAttributes, TagValue.EmptyJson, m_country.Id, "London Stock Exchange", new List<string> { "LSE" }, m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);

      //create the test stock instruments and instrument groups
      var msft = new Stock("MSFT", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "MSFT^C", "MSFT-C" }, "Microsoft", "Microsoft Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id, m_lse.Id }, string.Empty);
      msft.MarketCap = 3073560000000;
      var aapl = new Stock("AAPL", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "AAPL^C", "AAPL~C" }, "Apple", "Apple Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id }, string.Empty);
      aapl.MarketCap = 200000;
      var disp = new Stock("DISP", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "DISP^C", "DISP-C" }, "Disney", "Disney Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id, m_lse.Id }, string.Empty);
      disp.MarketCap = 300000;
      var nflx = new Stock("NFLX", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "NFLX^C", "NFLX~C" }, "Netflix", "Netflix Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id }, string.Empty);
      nflx.MarketCap = 400000;
      var auto = new Stock("AUTO", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "AUTO^C", "AUTO@C" }, "Autozone", "Autozone Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id, m_lse.Id }, string.Empty);
      auto.MarketCap = 500000;
      var pep = new Stock("PEP", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "PEP~C", "PEP@C" }, "Pep Boys", "Pep Boys Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_lse.Id }, string.Empty);
      pep.MarketCap = 600000;
      var good = new Stock("GOOD", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "GOOD_C", "GOOD-C" }, "Goodyear", "Goodyear Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id }, string.Empty);
      good.MarketCap = 700000;
      var mich = new Stock("MICH", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, new List<string> { "MICH~C", "MICH^C" }, "Michelin", "Michelin Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_lse.Id }, string.Empty);
      mich.MarketCap = 800000;

      m_exchanges = new List<Exchange> { m_global, m_nyse, m_nasdaq, m_lse };
      m_instruments = new List<Instrument> { msft, aapl, disp, nflx, auto, pep, good, mich };
      m_emptyInstruments = new List<Instrument>();

      m_addedInstruments = new List<Instrument>();
      m_updatedInstruments = new List<Instrument>();
      m_allFileInstruments = new List<Instrument>();

      //Good example on how to Mock ILogger - https://dev.to/blueskyson/unit-testing-ilogger-with-nunit-and-moq-1fbn
      m_logger = new Mock<ILogger<CoreUI.Services.InstrumentService>>();
      m_logger.Setup(m => m.Log(
              It.IsAny<LogLevel>(),
              It.IsAny<EventId>(),
              It.IsAny<It.IsAnyType>(),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()!
          )).Callback(new InvocationAction(invocation =>
          {
            var logLevel = (LogLevel)invocation.Arguments[0];
            var eventId = (EventId)invocation.Arguments[1];
            var state = invocation.Arguments[2];
            var exception = (Exception)invocation.Arguments[3];
            var formatter = invocation.Arguments[4];
            var invokeMethod = formatter.GetType().GetMethod("Invoke");
            var logMessage = invokeMethod!.Invoke(formatter, new[] { state, exception });
            Debug.WriteLine($"InstrumentService Log: {(string)logMessage!}");
          }));

      m_application = new Mock<IApplication>();
      m_serviceProvider = new Mock<IServiceProvider>();
      m_fileSystemService = new Mock<IFileSystemService>();
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(m_fileSystemService.Object);
      m_application.SetupGet(x => x.Services).Returns(m_serviceProvider.Object);
      IApplication.Current = m_application.Object;

      m_exchangeService = new Mock<IExchangeService>();
      m_instrumentRepository = new Mock<IInstrumentRepository>();
      m_dialogService = new Mock<IDialogService>();

      m_exchangeService.SetupGet(x => x.Items).Returns(m_exchanges);
      m_dialogService.Setup(x => x.ShowStatusMessageAsync(It.IsAny<IDialogService.StatusMessageSeverity>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
      m_instrumentRepository.Setup(x => x.GetItems()).Returns(m_emptyInstruments);
      m_instrumentRepository.Setup(x => x.Update(It.IsAny<Instrument>())).Callback((Instrument x) => m_updatedInstruments.Add(x));
      m_instrumentRepository.Setup(x => x.Add(It.IsAny<Instrument>())).Callback((Instrument x) => m_addedInstruments.Add(x));
      m_instrumentCacheService = new InstrumentCacheService(m_dialogService.Object, m_instrumentRepository.Object);
      m_instrumentService = new CoreUI.Services.InstrumentService(m_logger.Object, m_exchangeService.Object, m_instrumentRepository.Object, m_instrumentCacheService, m_dialogService.Object);
    }

    //finalizers
    ~InstrumentService()
    {

    }

    //interface implementations


    //properties


    //methods
    public bool listEquals(IList<string> list1, IList<string> list2)
    {
      if (list1.Count != list2.Count)
        return false;
      foreach (var item in list2)
        if (list1.FirstOrDefault(x => x == item) == null) return false;
      return true;
    }

    public bool listEquals(IList<Guid> list1, IList<Guid> list2)
    {
      if (list1.Count != list2.Count)
        return false;
      foreach (var item in list2)
        if (list1.FirstOrDefault(x => x == item) == null) return false;
      return true;
    }

    public void postImport()
    {
      m_allFileInstruments.AddRange(m_addedInstruments);
      m_allFileInstruments.AddRange(m_updatedInstruments);
    }

    public void checkImportedInstruments(List<Instrument> expectedInstruments, List<Instrument> importedInstruments)
    {
      Assert.AreEqual(expectedInstruments.Count, importedInstruments.Count, "Loaded instrument counts are not correct");
      foreach (var instrument in importedInstruments)
      {
        var expectedInstrument = expectedInstruments.Find(x => x.Ticker == instrument.Ticker);
        Stock expectedStock = (Stock)expectedInstrument!;
        if (expectedInstrument == null)
          Assert.Fail($"Instrument {instrument.Ticker} not found in the database");
        else
        {
          Assert.IsTrue(instrument is Stock, "Instrument is not a stock");
          Stock stock = (Stock)instrument;
          Assert.AreEqual(instrument.Ticker, expectedInstrument.Ticker, "Ticker is not correct");
          Assert.AreEqual(instrument.Name, expectedInstrument.Name, "Name is not correct");
          Assert.AreEqual(instrument.Description, expectedInstrument.Description, "Description is not correct");
          Assert.AreEqual(instrument.PrimaryExchangeId, expectedInstrument.PrimaryExchangeId, "PrimaryExchangeId is not correct");
          Assert.AreEqual(instrument.InceptionDate, expectedInstrument.InceptionDate, "InceptionDate is not correct");
          Assert.AreEqual(instrument.PriceDecimals, expectedInstrument.PriceDecimals, "PriceDecimals is not correct");
          Assert.AreEqual(instrument.MinimumMovement, expectedInstrument.MinimumMovement, "MinimumMovement");
          Assert.AreEqual(instrument.BigPointValue, expectedInstrument.BigPointValue, "BigPointValue is not correct");
          Assert.AreEqual(stock.MarketCap, expectedStock.MarketCap, "MarketCap is not correct");
          Assert.AreEqual(instrument.TagStr, expectedInstrument.TagStr, "Tag is not correct");
          Assert.AreEqual(instrument.AttributeSet, expectedInstrument.AttributeSet, "AttributesSet is not correct");
          Assert.IsTrue(listEquals(instrument.SecondaryExchangeIds, expectedInstrument.SecondaryExchangeIds), "SecondaryExchangeIds is not correct");
        }
      }
    }

    //ENHANCEMENT: Can add explicit tests to add instruments to the repository and check if they are added correctly.

    [TestMethod]
    [DataRow("test.csv")]
    [DataRow("test.json")]
    public void ExportImport_Created_Success(string filename)
    {
      //load the data from the mock repository
      m_instrumentRepository.Setup(x => x.GetItems()).Returns(m_instruments);
      m_instrumentService.Refresh();
      m_instrumentCacheService.Refresh();

      //export
      MemoryStream writeStream = new MemoryStream();
      StreamWriter streamWriter = new StreamWriter(writeStream);
      m_fileSystemService.Setup(x => x.CreateText(It.IsAny<string>())).Returns(streamWriter);
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(m_fileSystemService.Object);

      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = filename;

      m_instrumentService.Export(exportSettings);

      //import
      byte[] buffer = writeStream.ToArray();
      MemoryStream readStream = new MemoryStream(buffer);
      StreamReader streamReader = new StreamReader(readStream);
      m_fileSystemService.Setup(x => x.OpenFile(It.IsAny<string>(), It.IsAny<FileStreamOptions>())).Returns(streamReader);

      var importSettings = new ImportSettings();
      importSettings.Filename = filename;
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace;

      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments(m_instruments, m_updatedInstruments);
    }

    [TestMethod]
    [DataRow("test.csv")]
    [DataRow("test.json")]
    public void ExportImport_Updated_Success(string filename)
    {
      //load the data from the mock repository
      m_instrumentRepository.Setup(x => x.GetItems()).Returns(m_instruments);
      m_instrumentService.Refresh();
      m_instrumentCacheService.Refresh();

      //export
      MemoryStream writeStream = new MemoryStream();
      StreamWriter streamWriter = new StreamWriter(writeStream);
      m_fileSystemService.Setup(x => x.CreateText(It.IsAny<string>())).Returns(streamWriter);
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(m_fileSystemService.Object);

      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = filename;
      
      m_instrumentService.Export(exportSettings);

      //import
      byte[] buffer = writeStream.ToArray();
      MemoryStream readStream = new MemoryStream(buffer);
      StreamReader streamReader = new StreamReader(readStream);
      m_fileSystemService.Setup(x => x.OpenFile(It.IsAny<string>(), It.IsAny<FileStreamOptions>())).Returns(streamReader);

      var importSettings = new ImportSettings();
      importSettings.Filename = filename;
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Update; //force call to our dummy repository
      
      m_instrumentService.Import(importSettings);

      postImport();
      checkImportedInstruments(m_instruments, m_updatedInstruments);
    }
  }
}
