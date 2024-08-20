using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;
using System.Diagnostics;
using TradeSharp.CoreUI.Common;

namespace TradeSharp.CoreUI.Testing.Services
{
  [TestClass]
  public class InstrumentGroupService
  {
    //constants


    //enums


    //types


    //attributes
    private Mock<ILogger<CoreUI.Services.InstrumentGroupService>> m_logger;
    private Mock<IApplication> m_application;
    private Mock<IServiceProvider> m_serviceProvider;
    private Mock<IFileSystemService> m_fileSystemService;
    private Mock<IDatabase> m_database;
    private Mock<IInstrumentService> m_instrumentService;
    private Mock<IDialogService> m_dialogService;
    private List<InstrumentGroup> m_instrumentGroups;
    private List<InstrumentGroup> m_addedInstrumentGroups;
    private List<InstrumentGroup> m_updatedInstrumentGroups;
    private List<InstrumentGroup> m_allFileInstrumentGroups;
    private List<Instrument> m_instruments;
    private Mock<IInstrumentGroupRepository> m_instrumentGroupRepository;
    private CoreUI.Services.InstrumentGroupService m_instrumentGroupService;
    private TimeZoneInfo m_timeZone;
    private Country m_country;
    private Exchange m_exchange;
    private Instrument m_msft;
    private Instrument m_aapl;
    private Instrument m_goog;
    private Instrument m_disp;
    private Instrument m_nflx;
    private Instrument m_roku;
    private Instrument m_auto;
    private Instrument m_pep;
    private Instrument m_orly;
    private Instrument m_good;
    private Instrument m_mich;
    private Instrument m_fire;
    private InstrumentGroup m_msciGics;
    private InstrumentGroup m_communicationServices;
    private InstrumentGroup m_mediaAndEntertainment;
    private InstrumentGroup m_entertainment;
    private InstrumentGroup m_moviesEntertainment;
    private InstrumentGroup m_interactiveHomeEntertainment;
    private InstrumentGroup m_consumerDiscretionary;
    private InstrumentGroup m_automobilesAndComponents;
    private InstrumentGroup m_automobileComponents;
    private InstrumentGroup m_automotivePartsAndEquipment;
    private InstrumentGroup m_tiresAndRubber;


    //constructors
    public InstrumentGroupService()
    {
      //ensure full coverage of the service with debugging output
      Debugging.InstrumentGroupImport = true;

      //create the test country, exchange, and instruments
      m_country = new Country(Guid.NewGuid(), Country.DefaultAttributes, "TagValue", "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributes, "TagValue", m_country.Id, "TestExchange", Array.Empty<string>(), m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty, string.Empty);

      //create the test stock instruments and instrument groups
      m_msft = new Stock("MSFT", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Microsoft", "Microsoft Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_aapl = new Stock("AAPL", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Apple", "Apple Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_goog = new Stock("GOOG", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Google", "Google Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_disp = new Stock("DISP", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Disney", "Disney Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_nflx = new Stock("NFLX", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Netflix", "Netflix Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_roku = new Stock("ROKU", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Roku", "Roku Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_auto = new Stock("AUTO", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Autozone", "Autozone Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_pep = new Stock("PEP", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Pep Boys", "Pep Boys Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_orly = new Stock("ORLY", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "O'Reiley", "O'Reiley Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_good = new Stock("GOOD", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Goodyear", "Goodyear Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_mich = new Stock("MICH", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Michelin", "Michelin Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);
      m_fire = new Stock("FIRE", Instrument.DefaultAttributes, TagValue.EmptyJson, InstrumentType.Stock, Array.Empty<string>(), "Firestone", "Firestone Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>(), string.Empty);

      m_instruments = new List<Instrument>();
      m_instruments =
      [
        m_msft,
        m_aapl,
        m_goog,
        m_disp,
        m_nflx,
        m_roku,
        m_auto,
        m_pep,
        m_orly,
        m_good,
        m_mich,
        m_fire,
      ];

      m_msciGics = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, InstrumentGroup.InstrumentGroupRoot, "MSCI Global Industry Classification Standard", new List<string> { "MSCI GICS", "MSCI GICS Standard" }, "MSCI Global Industry Classification Standard", "0", Array.Empty<string>());
      m_communicationServices = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_msciGics.Id, "Communication Services", new List<string> { "Coms", "Comms Services" }, "Communication Services", "50", Array.Empty<string>());
      m_mediaAndEntertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_communicationServices.Id, "Media & Entertainment", new List<string> { "Media & Ent", "Media&Ent" }, "Media & Entertainment", "5020", Array.Empty<string>());
      m_entertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_mediaAndEntertainment.Id, "Entertainment", new List<string> { "Entertain" }, "Entertainment", "502020", Array.Empty<string>());
      m_moviesEntertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_entertainment.Id, "Movies & Entertainment", new List<string> { "Movies", "Film Entertainment" }, "Movies & Entertainment", "50202010", new List<string> { m_disp.Ticker, m_nflx.Ticker });
      m_interactiveHomeEntertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_entertainment.Id, "Interactive Home Entertainment", new List<string> { "Inter Home Ent", "Interactive Entertainment" }, "Interactive Entertainment", "50202020", new List<string> { m_msft.Ticker, m_aapl.Ticker });
      m_consumerDiscretionary = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_msciGics.Id, "Consumer Discretionary", new List<string> { "Consumer Disc", "Con Disc" }, "Consumer Discretionary", "25", Array.Empty<string>());
      m_automobilesAndComponents = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_consumerDiscretionary.Id, "Automobiles & Components", new List<string> { "Auto & Comp", "Auto&Comp" }, "Automobiles & Components", "2510", Array.Empty<string>());
      m_automobileComponents = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_automobilesAndComponents.Id, "Automobile Components", new List<string> { "Automotive Components", "Automotive" }, "Automobile Components", "251010", Array.Empty<string>());
      m_automotivePartsAndEquipment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_automobileComponents.Id, "Automotive Parts & Equipment", new List<string> { "Auto Parts", "Auto Parts Suppliers" }, "Automotive Parts & Equipment", "25101010", new List<string> { m_auto.Ticker, m_pep.Ticker });
      m_tiresAndRubber = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, TagValue.EmptyJson, m_automobileComponents.Id, "Tires & Rubber", new List<string> { "Tires+Rubber", "TiresRubber" }, "Tires & Rubber", "25101020", new List<string> { m_good.Ticker, m_mich.Ticker });
      m_instrumentGroups = new List<InstrumentGroup>();
      m_instrumentGroups =
      [
        m_msciGics,
        m_communicationServices,
        m_consumerDiscretionary,
        m_mediaAndEntertainment,
        m_entertainment,
        m_moviesEntertainment,
        m_interactiveHomeEntertainment,
        m_automobilesAndComponents,
        m_automobileComponents,
        m_automotivePartsAndEquipment,
        m_tiresAndRubber
      ];

      m_addedInstrumentGroups = new List<InstrumentGroup>();
      m_updatedInstrumentGroups = new List<InstrumentGroup>();
      m_allFileInstrumentGroups = new List<InstrumentGroup>();

      //Good example on how to Mock ILogger - https://dev.to/blueskyson/unit-testing-ilogger-with-nunit-and-moq-1fbn
      m_logger = new Mock<ILogger<CoreUI.Services.InstrumentGroupService>>();
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
            Debug.WriteLine($"InstrumentGroupService Log: {(string)logMessage!}");
          }));

      m_application = new Mock<IApplication>();
      m_serviceProvider = new Mock<IServiceProvider>();
      m_fileSystemService = new Mock<IFileSystemService>();
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(m_fileSystemService.Object);
      m_application.SetupGet(x => x.Services).Returns(m_serviceProvider.Object);
      IApplication.Current = m_application.Object;

      m_database = new Mock<IDatabase>();
      m_instrumentGroupRepository = new Mock<IInstrumentGroupRepository>();
      m_instrumentService = new Mock<IInstrumentService>();
      m_dialogService = new Mock<IDialogService>();

      m_database.Setup(x => x.GetInstrumentGroups()).Returns(m_instrumentGroups);
      m_database.Setup(x => x.GetInstruments()).Returns(m_instruments);
      m_dialogService.Setup(x => x.ShowStatusMessageAsync(It.IsAny<IDialogService.StatusMessageSeverity>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
      m_instrumentService.Setup(x => x.Items).Returns(m_instruments);
      m_instrumentGroupRepository.Setup(x => x.GetItems()).Returns(m_instrumentGroups);
      m_instrumentGroupRepository.Setup(x => x.Update(It.IsAny<InstrumentGroup>())).Callback((InstrumentGroup x) => m_updatedInstrumentGroups.Add(x));
      m_instrumentGroupRepository.Setup(x => x.Add(It.IsAny<InstrumentGroup>())).Callback((InstrumentGroup x) => m_addedInstrumentGroups.Add(x));

      m_instrumentGroupService = new CoreUI.Services.InstrumentGroupService(m_logger.Object, m_database.Object, m_instrumentGroupRepository.Object, m_instrumentService.Object, m_dialogService.Object);
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public string outputTestFile(string filename, string[] content)
    {
      string fullFilename = Path.Combine(Path.GetTempPath(), filename);
      File.WriteAllText(fullFilename, string.Join("\n", content));
      return fullFilename;
    }

    public void mergedAddedUpdated()
    {
      m_allFileInstrumentGroups.AddRange(m_addedInstrumentGroups);
      m_allFileInstrumentGroups.AddRange(m_updatedInstrumentGroups);
    }

    public void checkParentIds(List<InstrumentGroup> expectedGroups, List<InstrumentGroup> importedGroups)
    {
      foreach (var instrumentGroup in importedGroups)
        Assert.AreNotEqual(instrumentGroup.ParentId, Guid.Empty, $"ParentId for added {instrumentGroup.Name} is empty.");
    }

    public void checkIds(List<InstrumentGroup> expectedGroups, List<InstrumentGroup> importedGroups, bool checkExpected)
    {
      foreach (var instrumentGroup in importedGroups)
      {
        Assert.AreNotEqual(instrumentGroup.Id, Guid.Empty, $"Id for added {instrumentGroup.Name} is empty.");
        if (checkExpected) Assert.IsNotNull(expectedGroups.FirstOrDefault(x => x.Id == instrumentGroup.Id), $"Id {instrumentGroup.Id} for added {instrumentGroup.Name} not found.");
      }
    }

    public void checkChildParentAssociations(List<InstrumentGroup> expectedGroups, List<InstrumentGroup> importedGroups)
    {
      foreach (var instrumentGroup in importedGroups)
        Assert.IsTrue(instrumentGroup.ParentId == InstrumentGroup.InstrumentGroupRoot || m_allFileInstrumentGroups.FirstOrDefault(x => x.Id == instrumentGroup.ParentId) != null, $"ParentId {instrumentGroup.ParentId} for added {instrumentGroup.Name} not found.");
    }

    public void checkLoadedInstruments(List<InstrumentGroup> expectedGroups, List<InstrumentGroup> importedGroups)
    {
      foreach (var instrumentGroup in importedGroups)
      {
        var expectedGroup = expectedGroups.FirstOrDefault(x => x.Equals(instrumentGroup));
        foreach (var instrument in expectedGroup.Instruments)
          Assert.IsNotNull(instrumentGroup.Instruments.FirstOrDefault(x => x.Equals(instrument)), $"Instrument {instrument} for {instrumentGroup.Name} was not found.");
      }
    }


    public void checkLoadedInstrumentGroups(List<InstrumentGroup> expectedGroups, List<InstrumentGroup> importedGroups)
    {
      Assert.AreEqual(expectedGroups.Count, importedGroups.Count, "Expected and imported instrument group counts are not the same.");
      foreach (var importedGroup in importedGroups)
      {
        var expectedGroup = expectedGroups.FirstOrDefault(x => x.Equals(importedGroup));   //this might not match on the Id but on some of the other fields
        Assert.IsNotNull(expectedGroup, $"InstrumentGroup {importedGroup.Name} was not found in the static definitions.");
        Assert.AreEqual(expectedGroup.Name, importedGroup.Name, $"Name for {importedGroup.Name} is not correct.");
        Assert.IsTrue(listEquals(expectedGroup.AlternateNames, importedGroup.AlternateNames), $"AlternateNames for {importedGroup.Name} is not correct.");
        Assert.AreEqual(expectedGroup.Description, importedGroup.Description, $"Description for {importedGroup.Name} is not correct.");
        Assert.AreEqual(expectedGroup.UserId, importedGroup.UserId, $"UserId for {importedGroup.Name} is not correct.");
        Assert.AreEqual(expectedGroup.TagStr, importedGroup.TagStr, $"Tag for {importedGroup.Name} is not correct.");
        Assert.AreEqual(expectedGroup.AttributeSet, importedGroup.AttributeSet, $"Attributes for {importedGroup.Name} is not correct.");
        Assert.IsTrue(listEquals(expectedGroup.Instruments, importedGroup.Instruments), $"Instruments for {importedGroup.Name} is not correct.");
      }
    }

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

    [TestMethod]
    [DataRow("test.csv")]
    [DataRow("test.json")]
    public void ExportImport_Success(string filename)
    {
      //load the instrument groups from the mock repository
      m_instrumentGroupService.Refresh();

      //export
      MemoryStream writeStream = new MemoryStream();
      StreamWriter streamWriter = new StreamWriter(writeStream);
      m_fileSystemService.Setup(x => x.CreateText(It.IsAny<string>())).Returns(streamWriter);
      m_serviceProvider.Setup(x => x.GetService(typeof(IFileSystemService))).Returns(m_fileSystemService.Object);

      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = filename;

      m_instrumentGroupService.Export(exportSettings);

      //import
      byte[] buffer = writeStream.ToArray();
      MemoryStream readStream = new MemoryStream(buffer);
      StreamReader streamReader = new StreamReader(readStream);
      m_fileSystemService.Setup(x => x.OpenFile(It.IsAny<string>(), It.IsAny<FileStreamOptions>())).Returns(streamReader);

      var importSettings = new ImportSettings();
      importSettings.Filename = filename;
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace;

      m_instrumentGroupService.Import(importSettings);
      mergedAddedUpdated();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_allFileInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds(m_instrumentGroups, m_allFileInstrumentGroups);
      checkIds(m_instrumentGroups, m_allFileInstrumentGroups, false);
      checkChildParentAssociations(m_instrumentGroups, m_allFileInstrumentGroups);
      checkLoadedInstruments(m_instrumentGroups, m_allFileInstrumentGroups);
      checkLoadedInstrumentGroups(m_instrumentGroups, m_allFileInstrumentGroups);
    }
  }
}
