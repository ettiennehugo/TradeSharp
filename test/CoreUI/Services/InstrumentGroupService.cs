using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;
using System.Diagnostics;

namespace TradeSharp.CoreUI.Testing.Services
{
  [TestClass]
  public class InstrumentGroupService
  {
    //constants
    static string[] csvWithIds = [
      "parentid,id,name,alternatenames,description,userid,tag,attributes,tickers",
      "11111111-1111-1111-1111-111111111111,12121212-1212-1212-1212-121212121212,MSCI Global Industry Classification Standard,\"MSCI GICS, MSCI GICS Standard\",MSCI Global Industry Classification Standard,0,0,0,",
      "12121212-1212-1212-1212-121212121212,13131313-1313-1313-1313-131313131313,Consumer Discretionary,\"Consumer Disc,Con Disc\",Consumer Discretionary,25,25,0,",
      "12121212-1212-1212-1212-121212121212,14141414-1414-1414-1414-141414141414,Communication Services,\"Comms Services,Coms\",Communication Services,50,50,0,",
      "13131313-1313-1313-1313-131313131313,15151515-1515-1515-1515-151515151515,Automobiles & Components,\"Auto & Comp, Auto&Comp\",Automobiles & Components,2510,2510,0,",
      "14141414-1414-1414-1414-141414141414,16161616-1616-1616-1616-161616161616,Media & Entertainment,\"Media & Ent, Media&Ent\",Media & Entertainment,5020,5020,0,",
      "13131313-1313-1313-1313-131313131313,17171717-1717-1717-1717-171717171717,Automobile Components,\"Automotive Components, Automotive\",Automobile Components,251010,251010,0,",
      "14141414-1414-1414-1414-141414141414,18181818-1818-1818-1818-181818181818,Entertainment,Entertain,Entertainment,502020,502020,0,",
      "18181818-1818-1818-1818-181818181818,19191919-1919-1919-1919-191919191919,Movies & Entertainment,\"Movies, Film Entertainment\",Movies & Entertainment,50202010,50202010,0,\"DISP,NFLX\"",
      "18181818-1818-1818-1818-181818181818,21212121-2121-2121-2121-212121212121,Interactive Home Entertainment,\"Inter Home Ent, Interactive Entertainment\",Interactive Entertainment,50202020,50202020,0,\"MSFT,AAPL\"",
      "17171717-1717-1717-1717-171717171717,22222222-2222-2222-2222-222222222222,Automotive Parts & Equipment,\"Auto Parts, Auto Parts Suppliers\",Automotive Parts & Equipment,25101010,25101010,0,\"AUTO,PEP\"",
      "17171717-1717-1717-1717-171717171717,23232323-2323-2323-2323-232323232323,Tires & Rubber,\"Tires+Rubber,TiresRubber\",Tires & Rubber,25101020,25101020,0,\"GOOD,MICH\""
    ];

    static string[] csvWithNames = [
      "parentname,name,alternatenames,description,userid,tag,attributes,tickers",
      ",MSCI Global Industry Classification Standard,\"MSCI GICS, MSCI GICS Standard\",MSCI Global Industry Classification Standard,0,0,0,,",
      "MSCI GICS,Consumer Discretionary,\"Consumer Disc,Con Disc\",Consumer Discretionary,25,25,0,",
      "MSCI Global Industry Classification Standard,Communication Services,\"Comms Services,Coms\",Communication Services,50,50,0,",
      "Con Disc,Automobiles & Components,\"Auto & Comp, Auto&Comp\",Automobiles & Components,2510,2510,0,'",
      "Coms,Media & Entertainment,\"Media & Ent, Media&Ent\",Media & Entertainment,5020,5020,0,",
      "Auto & Comp,Automobile Components,\"Automotive Components, Automotive\",Automobile Components,251010,251010,0,",
      "Media&Ent,Entertainment,Entertain,Entertainment,502020,502020,0,,",
      "Entertainment,Movies & Entertainment,\"Movies, Film Entertainment\",Movies & Entertainment,50202010,50202010,0,\"DISP,NFLX\"",
      "Entertain,Interactive Home Entertainment,\"Inter Home Ent, Interactive Entertainment\",Interactive Entertainment,50202020,50202020,0,\"MSFT,AAPL\"",
      "Automotive Components,Automotive Parts & Equipment,\"Auto Parts, Auto Parts Suppliers\",Automotive Parts & Equipment,25101010,25101010,0,\"AUTO,PEP\"",
      "Automotive,Tires & Rubber,\"Tires+Rubber,TiresRubber\",Tires & Rubber,25101020,25101020,0,\"GOOD,MICH\""
    ];

    static private string[] jsonWithNames =
    [
      "[",
      "{",
      "  \"Name\": \"MSCI Global Industry Classification Standard\",",
      "  \"AlternateNames\":[\"MSCI GICS\",\"MSCI GICS Standard\"],",
      "  \"Description\": \"MSCI Global Industry Classification Standard\",",
      "  \"UserId\": \"0\",",
      "  \"Tag\": \"0\",",
      "  \"Attributes\": \"0\",",
      "  \"Instruments\": [],",
      "  \"Children\": ",
      "  [",
      "  {",
      "      \"Name\": \"Communication Services\",",
      "      \"AlternateNames\":[\"Comms Services\", \"Coms\"],",
      "      \"Description\": \"Communication Services\",",
      "      \"UserId\": \"50\",",
      "      \"Tag\": \"50\",",
      "      \"Attributes\": \"0\",",
      "      \"Instruments\": [],",
      "      \"Children\": ",
      "      [",
      "        {",
      "          \"Name\": \"Media \u0026 Entertainment\",",
      "          \"AlternateNames\":[\"Media \u0026 Ent\",\"Media\u0026Ent\"],",
      "          \"Description\": \"Media \u0026 Entertainment\",",
      "          \"UserId\": \"5020\",",
      "          \"Tag\": \"5020\",",
      "          \"Attributes\": \"0\",",
      "          \"Instruments\": [],",
      "          \"Children\":",
      "          [",
      "            {",
      "              \"Name\": \"Entertainment\",",
      "              \"AlternateNames\":[\"Entertain\"],",
      "              \"Description\": \"Entertainment\",",
      "              \"UserId\": \"502020\",",
      "              \"Tag\": \"502020\",",
      "              \"Attributes\": \"0\",",
      "              \"Instruments\": [],",
      "              \"Children\": [",
      "                {",
      "                  \"Name\": \"Movies \u0026 Entertainment\",",
      "                  \"AlternateNames\":[\"Movies\",\"Film Entertainment\"],",
      "                  \"Description\": \"Movies \u0026 Entertainment\",",
      "                  \"UserId\": \"50202010\",",
      "                  \"Tag\": \"50202010\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"DISP\",\"NFLX\"],",
      "                  \"Children\": []",
      "              },",
      "              {",
      "                  \"Name\": \"Interactive Home Entertainment\",",
      "                  \"AlternateNames\":[\"Inter Home Ent\",\"Interactive Entertainment\"],",
      "                  \"Description\": \"Interactive Entertainment\",",
      "                  \"UserId\": \"50202020\",",
      "                  \"Tag\": \"50202020\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"MSFT\",\"AAPL\"],",
      "                  \"Children\": []",
      "              }",
      "            ]",
      "          }",
      "        ]",
      "      }",
      "    ]",
      "  },",
      "  {",
      "      \"Name\": \"Consumer Discretionary\",",
      "      \"AlternateNames\":[\"Consumer Disc\",\"Con Disc\"],",
      "      \"Description\": \"Consumer Discretionary\",",
      "      \"UserId\": \"25\",",
      "      \"Tag\": \"25\",",
      "      \"Attributes\": \"0\",",
      "      \"Instruments\": [],",
      "      \"Children\": ",
      "      [",
      "        {",
      "          \"Name\": \"Automobiles \u0026 Components\",",
      "          \"AlternateNames\":[\"Auto \u0026 Comp\",\"Auto\u0026Comp\"],",
      "          \"Description\": \"Automobiles \u0026 Components\",",
      "          \"UserId\": \"2510\",",
      "          \"Tag\": \"2510\",",
      "          \"Attributes\": \"0\",",
      "          \"Instruments\": [],",
      "          \"Children\": ",
      "          [",
      "            {",
      "              \"Name\": \"Automobile Components\",",
      "              \"AlternateNames\":[\"Automotive Components\",\"Automotive\"],",
      "              \"Description\": \"Automobile Components\",",
      "              \"UserId\": \"251010\",",
      "              \"Tag\": \"251010\",",
      "              \"Attributes\": \"0\",",
      "              \"Instruments\": [],",
      "              \"Children\": ",
      "              [",
      "                {",
      "                  \"Name\": \"Automotive Parts \u0026 Equipment\",",
      "                  \"AlternateNames\":[\"Auto Parts\",\"Auto Parts Suppliers\"],",
      "                  \"Description\": \"Automotive Parts \u0026 Equipment\",",
      "                  \"UserId\": \"25101010\",",
      "                  \"Tag\": \"25101010\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"AUTO\",\"PEP\"],",
      "                  \"Children\": []",
      "                },",
      "                {",
      "                  \"Name\": \"Tires \u0026 Rubber\",",
      "                  \"AlternateNames\":[\"Tires+Rubber\",\"TiresRubber\"],",
      "                  \"Description\": \"Tires \u0026 Rubber\",",
      "                  \"UserId\": \"25101020\",",
      "                  \"Tag\": \"25101020\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"GOOD\",\"MICH\"],",
      "                  \"Children\": []",
      "                }",
      "              ]",
      "            }",
      "          ]",
      "        }  ",
      "      ]",
      "    }",
      "  ]",
      "}",
      "]"
    ];

    static private string[] jsonWithIds =
    [
      "[",
      "{",
      "  \"Id\":\"12121212-1212-1212-1212-121212121212\",",
      "  \"Name\": \"MSCI Global Industry Classification Standard\",",
      "  \"AlternateNames\":[\"MSCI GICS\",\"MSCI GICS Standard\"],",
      "  \"Description\": \"MSCI Global Industry Classification Standard\",",
      "  \"UserId\": \"0\",",
      "  \"Tag\": \"0\",",
      "  \"Attributes\": \"0\",",
      "  \"Instruments\": [],",
      "  \"Children\": ",
      "  [",
      "  {",
      "      \"Id\":\"14141414-1414-1414-1414-141414141414\",",
      "      \"Name\": \"Communication Services\",",
      "      \"AlternateNames\":[\"Comms Services\", \"Coms\"],",
      "      \"Description\": \"Communication Services\",",
      "      \"UserId\": \"50\",",
      "      \"Tag\": \"50\",",
      "      \"Attributes\": \"0\",",
      "      \"Instruments\": [],",
      "      \"Children\": ",
      "      [",
      "        {",
      "          \"Id\":\"16161616-1616-1616-1616-161616161616\",",
      "          \"Name\": \"Media \u0026 Entertainment\",",
      "          \"AlternateNames\":[\"Media \u0026 Ent\",\"Media\u0026Ent\"],",
      "          \"Description\": \"Media \u0026 Entertainment\",",
      "          \"UserId\": \"5020\",",
      "          \"Tag\": \"5020\",",
      "          \"Attributes\": \"0\",",
      "          \"Instruments\": [],",
      "          \"Children\":",
      "          [",
      "            {",
      "              \"Id\":\"18181818-1818-1818-1818-181818181818\",",
      "              \"Name\": \"Entertainment\",",
      "              \"AlternateNames\":[\"Entertain\"],",
      "              \"Description\": \"Entertainment\",",
      "              \"UserId\": \"502020\",",
      "              \"Tag\": \"502020\",",
      "              \"Attributes\": \"0\",",
      "              \"Instruments\": [],",
      "              \"Children\": [",
      "                {",
      "                  \"Id\":\"19191919-1919-1919-1919-191919191919\",",
      "                  \"Name\": \"Movies \u0026 Entertainment\",",
      "                  \"AlternateNames\":[\"Movies\",\"Film Entertainment\"],",
      "                  \"Description\": \"Movies \u0026 Entertainment\",",
      "                  \"UserId\": \"50202010\",",
      "                  \"Tag\": \"50202010\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"DISP\",\"NFLX\"],",
      "                  \"Children\": []",
      "              },",
      "              {",
      "                  \"Id\":\"21212121-2121-2121-2121-212121212121\",",
      "                  \"Name\": \"Interactive Home Entertainment\",",
      "                  \"AlternateNames\":[\"Inter Home Ent\",\"Interactive Entertainment\"],",
      "                  \"Description\": \"Interactive Entertainment\",",
      "                  \"UserId\": \"50202020\",",
      "                  \"Tag\": \"50202020\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"MSFT\",\"AAPL\"],",
      "                  \"Children\": []",
      "              }",
      "            ]",
      "          }",
      "        ]",
      "      }",
      "    ]",
      "  },",
      "  {",
      "      \"Id\":\"13131313-1313-1313-1313-131313131313\",",
      "      \"Name\": \"Consumer Discretionary\",",
      "      \"AlternateNames\":[\"Consumer Disc\",\"Con Disc\"],",
      "      \"Description\": \"Consumer Discretionary\",",
      "      \"UserId\": \"25\",",
      "      \"Tag\": \"25\",",
      "      \"Attributes\": \"0\",",
      "      \"Instruments\": [],",
      "      \"Children\": ",
      "      [",
      "        {",
      "          \"Id\":\"15151515-1515-1515-1515-151515151515\",",
      "          \"Name\": \"Automobiles \u0026 Components\",",
      "          \"AlternateNames\":[\"Auto \u0026 Comp\",\"Auto\u0026Comp\"],",
      "          \"Description\": \"Automobiles \u0026 Components\",",
      "          \"UserId\": \"2510\",",
      "          \"Tag\": \"2510\",",
      "          \"Attributes\": \"0\",",
      "          \"Instruments\": [],",
      "          \"Children\": ",
      "          [",
      "            {",
      "              \"Id\":\"17171717-1717-1717-1717-171717171717\",",
      "              \"Name\": \"Automobile Components\",",
      "              \"AlternateNames\":[\"Automotive Components\",\"Automotive\"],",
      "              \"Description\": \"Automobile Components\",",
      "              \"UserId\": \"251010\",",
      "              \"Tag\": \"251010\",",
      "              \"Attributes\": \"0\",",
      "              \"Instruments\": [],",
      "              \"Children\": ",
      "              [",
      "                {",
      "                  \"Id\":\"22222222-2222-2222-2222-222222222222\",",
      "                  \"Name\": \"Automotive Parts \u0026 Equipment\",",
      "                  \"AlternateNames\":[\"Auto Parts\",\"Auto Parts Suppliers\"],",
      "                  \"Description\": \"Automotive Parts \u0026 Equipment\",",
      "                  \"UserId\": \"25101010\",",
      "                  \"Tag\": \"25101010\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"AUTO\",\"PEP\"],",
      "                  \"Children\": []",
      "                },",
      "                {",
      "                  \"Id\":\"23232323-2323-2323-2323-232323232323\",",
      "                  \"Name\": \"Tires \u0026 Rubber\",",
      "                  \"AlternateNames\":[\"Tires+Rubber\",\"TiresRubber\"],",
      "                  \"Description\": \"Tires \u0026 Rubber\",",
      "                  \"UserId\": \"25101020\",",
      "                  \"Tag\": \"25101020\",",
      "                  \"Attributes\": \"0\",",
      "                  \"Instruments\": [\"GOOD\",\"MICH\"],",
      "                  \"Children\": []",
      "                }",
      "              ]",
      "            }",
      "          ]",
      "        }  ",
      "      ]",
      "    }",
      "  ]",
      "}",
      "]"
    ];

    //enums


    //types


    //attributes
    private Mock<ILogger<CoreUI.Services.InstrumentGroupService>> m_logger;
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

    //constructors
    public InstrumentGroupService()
    {
      //ensure full coverage of the service with debugging output
      Debugging.InstrumentGroupImport = true;

      //create the test country, exchange, and instruments
      m_country = new Country(Guid.NewGuid(), Country.DefaultAttributeSet, "TagValue", "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_exchange = new Exchange(Guid.NewGuid(), Exchange.DefaultAttributeSet, "TagValue", m_country.Id, "TestExchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);

      //create the test stock instruments and instrument groups
      var msft = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "MSFT", InstrumentType.Stock, "MSFT", Array.Empty<string>(), "Microsoft", "Microsoft Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var aapl = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "AAPL", InstrumentType.Stock, "AAPL", Array.Empty<string>(), "Apple", "Apple Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var disp = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "DISP", InstrumentType.Stock, "DISP", Array.Empty<string>(), "Disney", "Disney Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var nflx = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "NFLX", InstrumentType.Stock, "NFLX", Array.Empty<string>(), "Netflix", "Netflix Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var auto = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "AUTO", InstrumentType.Stock, "AUTO", Array.Empty<string>(), "Autozone", "Autozone Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var pep = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "PEP", InstrumentType.Stock, "PEP", Array.Empty<string>(), "Pep Boys", "Pep Boys Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var good = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "GOOD", InstrumentType.Stock, "GOOD", Array.Empty<string>(), "Goodyear", "Goodyear Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());
      var mich = new Instrument(Guid.NewGuid(), Instrument.DefaultAttributeSet, "MICH", InstrumentType.Stock, "MICH", Array.Empty<string>(), "Michelin", "Michelin Corporation", DateTime.Now.ToUniversalTime(), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_exchange.Id, Array.Empty<Guid>());

      m_instruments = new List<Instrument>();
      m_instruments =
      [
        msft,
        aapl,
        disp,
        nflx,
        auto,
        pep,
        good,
        mich
      ];

      var msciGics = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "0", InstrumentGroup.InstrumentGroupRoot, "MSCI Global Industry Classification Standard", new List<string> { "MSCI GICS", "MSCI GICS Standard" }, "MSCI Global Industry Classification Standard", "0", Array.Empty<Guid>());
      var communicationServices = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "50", msciGics.Id, "Communication Services", new List<string> { "Coms", "Comms Services" }, "Communication Services", "50", Array.Empty<Guid>());
      var mediaAndEntertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "5020", communicationServices.Id, "Media & Entertainment", new List<string> { "Media & Ent", "Media&Ent" }, "Media & Entertainment", "5020", Array.Empty<Guid>());
      var entertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "502020", mediaAndEntertainment.Id, "Entertainment", new List<string> { "Entertain" }, "Entertainment", "502020", Array.Empty<Guid>());
      var moviesEntertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "50202010", entertainment.Id, "Movies & Entertainment", new List<string> { "Movies", "Film Entertainment" }, "Movies & Entertainment", "50202010", new List<Guid> { disp.Id, nflx.Id });
      var interactiveHomeEntertainment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "50202020", entertainment.Id, "Interactive Home Entertainment", new List<string> { "Inter Home Ent", "Interactive Entertainment" }, "Interactive Entertainment", "50202020", new List<Guid> { msft.Id, aapl.Id });
      var consumerDiscretionary = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "25", msciGics.Id, "Consumer Discretionary", new List<string> { "Consumer Disc", "Con Disc" }, "Consumer Discretionary", "25", Array.Empty<Guid>());
      var automobilesAndComponents = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "2510", consumerDiscretionary.Id, "Automobiles & Components", new List<string> { "Auto & Comp", "Auto&Comp" }, "Automobiles & Components", "2510", Array.Empty<Guid>());
      var automobileComponents = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "251010", automobilesAndComponents.Id, "Automobile Components", new List<string> { "Automotive Components", "Automotive" }, "Automobile Components", "251010", Array.Empty<Guid>());
      var automotivePartsAndEquipment = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "25101010", automobileComponents.Id, "Automotive Parts & Equipment", new List<string> { "Auto Parts", "Auto Parts Suppliers" }, "Automotive Parts & Equipment", "25101010", new List<Guid> { auto.Id, pep.Id });
      var tiresAndRubber = new InstrumentGroup(Guid.NewGuid(), (Attributes)0, "25101020", automobileComponents.Id, "Tires & Rubber", new List<string> { "Tires+Rubber", "TiresRubber" }, "Tires & Rubber", "25101020", new List<Guid> { good.Id, mich.Id });
      m_instrumentGroups = new List<InstrumentGroup>();
      m_instrumentGroups =
      [
        msciGics,
        communicationServices,
        consumerDiscretionary,
        mediaAndEntertainment,
        entertainment,
        moviesEntertainment,
        interactiveHomeEntertainment,
        automobilesAndComponents,
        automobileComponents,
        automotivePartsAndEquipment,
        tiresAndRubber
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

    public void cleanupTestFile(string filename)
    {
      string fullFilename = Path.Combine(Path.GetTempPath(), filename);
      File.Delete(fullFilename);
    }

    public void postImport()
    {
      m_allFileInstrumentGroups.AddRange(m_addedInstrumentGroups);
      m_allFileInstrumentGroups.AddRange(m_updatedInstrumentGroups);
    }

    public void checkParentIds()
    {
      foreach (var instrumentGroup in m_addedInstrumentGroups)
        Assert.AreNotEqual(instrumentGroup.ParentId, Guid.Empty, $"ParentId for added {instrumentGroup.Name} is empty.");
      foreach (var instrumentGroup in m_updatedInstrumentGroups)
        Assert.AreNotEqual(instrumentGroup.ParentId, Guid.Empty, $"ParentId for updated {instrumentGroup.Name} is empty.");
    }

    public void checkIds()
    {
      foreach (var instrumentGroup in m_addedInstrumentGroups)
        Assert.AreNotEqual(instrumentGroup.Id, Guid.Empty, $"Id for added {instrumentGroup.Name} is empty.");
      foreach (var instrumentGroup in m_updatedInstrumentGroups)
        Assert.AreNotEqual(instrumentGroup.Id, Guid.Empty, $"Id for updated {instrumentGroup.Name} is empty.");
    }

    public void checkChildParentAssociations()
    {
      foreach (var instrumentGroup in m_addedInstrumentGroups)
        Assert.IsTrue(instrumentGroup.ParentId == InstrumentGroup.InstrumentGroupRoot || m_allFileInstrumentGroups.FirstOrDefault(x => x.Id == instrumentGroup.ParentId) != null, $"ParentId {instrumentGroup.ParentId} for added {instrumentGroup.Name} not found.");
      foreach (var instrumentGroup in m_updatedInstrumentGroups)
        Assert.IsTrue(instrumentGroup.ParentId == InstrumentGroup.InstrumentGroupRoot || m_allFileInstrumentGroups.FirstOrDefault(x => x.Id == instrumentGroup.ParentId) != null, $"ParentId {instrumentGroup.ParentId} for updated {instrumentGroup.Name} not found.");
    }

    public void checkLoadedInstruments()
    {
      foreach (var instrumentGroup in m_allFileInstrumentGroups)
        foreach (var instrument in instrumentGroup.Instruments)        
          Assert.IsNotNull(m_instruments.FirstOrDefault(x => x.Id == instrument), $"Instrument {instrument} for {instrumentGroup.Name} was not found.");
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

    public void checkLoadedInstrumentGroupsAgainstStaticDefinitions()
    {
      foreach (var instrumentGroup in m_allFileInstrumentGroups)
      {
        var staticInstrumentGroup = m_instrumentGroups.FirstOrDefault(x => x.Equals(instrumentGroup));   //this might not match on the Id but on some of the other fields
        Assert.IsNotNull(staticInstrumentGroup, $"InstrumentGroup {instrumentGroup.Name} was not found in the static definitions.");
        Assert.AreEqual(staticInstrumentGroup.Name, instrumentGroup.Name, $"Name for {instrumentGroup.Name} is not correct.");
        Assert.IsTrue(listEquals(staticInstrumentGroup.AlternateNames, instrumentGroup.AlternateNames), $"AlternateNames for {instrumentGroup.Name} is not correct.");
        Assert.AreEqual(staticInstrumentGroup.Description, instrumentGroup.Description, $"Description for {instrumentGroup.Name} is not correct.");
        Assert.AreEqual(staticInstrumentGroup.UserId, instrumentGroup.UserId, $"UserId for {instrumentGroup.Name} is not correct.");
        Assert.AreEqual(staticInstrumentGroup.Tag, instrumentGroup.Tag, $"Tag for {instrumentGroup.Name} is not correct.");
        Assert.AreEqual(staticInstrumentGroup.AttributeSet, instrumentGroup.AttributeSet, $"Attributes for {instrumentGroup.Name} is not correct.");
        Assert.IsTrue(listEquals(staticInstrumentGroup.Instruments, instrumentGroup.Instruments), $"Instruments for {instrumentGroup.Name} is not correct.");
      }
    }

    [TestMethod]
    public void Import_CsvWithNames_Success()
    {
      string filename = "testWithNames.csv";      
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, csvWithNames);
      m_instrumentGroupService.Import(importSettings);
      cleanupTestFile(filename);
      postImport();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_addedInstrumentGroups.Count + m_updatedInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds();
      checkIds();
      checkChildParentAssociations();
      checkLoadedInstruments();
      checkLoadedInstrumentGroupsAgainstStaticDefinitions();
    }

    [TestMethod]
    public void Import_CsvWithIds_Success()
    {
      string filename = "testWithIds.csv";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, csvWithIds);
      m_instrumentGroupService.Import(importSettings);
      cleanupTestFile(filename);
      postImport();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_addedInstrumentGroups.Count + m_updatedInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds();
      checkIds();
      checkChildParentAssociations();
      checkLoadedInstruments();
      checkLoadedInstrumentGroupsAgainstStaticDefinitions();
    }

    [TestMethod]
    public void Import_JsonWithNames_Success()
    {
      string filename = "testWithNames.json";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, jsonWithNames);
      m_instrumentGroupService.Import(importSettings);
      cleanupTestFile(filename);
      postImport();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_addedInstrumentGroups.Count + m_updatedInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds();
      checkIds();
      checkChildParentAssociations();
      checkLoadedInstruments();
      checkLoadedInstrumentGroupsAgainstStaticDefinitions();
    }

    [TestMethod]
    public void Import_JsonWithIds_Success()
    {
      string filename = "testWithIds.json";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, jsonWithIds);
      m_instrumentGroupService.Import(importSettings);
      cleanupTestFile(filename);
      postImport();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_addedInstrumentGroups.Count + m_updatedInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds();
      checkIds();
      checkChildParentAssociations();
      checkLoadedInstruments();
      checkLoadedInstrumentGroupsAgainstStaticDefinitions();
    }

    [TestMethod]
    public void Export_Csv_Success()
    {
      m_instrumentGroupService.Refresh(); //load the data from the mock repository
      m_instrumentGroupService.SelectedNode = null;

      string filename = "testExport.csv";
      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = outputTestFile(filename, Array.Empty<string>());
      var importSettings = new ImportSettings();
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace;
      importSettings.Filename = exportSettings.Filename;
      m_instrumentGroupService.Export(exportSettings);
      m_instrumentGroupService.Import(importSettings);
      cleanupTestFile(filename);
      postImport();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_addedInstrumentGroups.Count + m_updatedInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds();
      checkIds();
      checkChildParentAssociations();
      checkLoadedInstruments();
      checkLoadedInstrumentGroupsAgainstStaticDefinitions();
    }

    [TestMethod]
    public void Export_Json_Success()
    {
      m_instrumentGroupService.Refresh(); //load the data from the mock repository
      m_instrumentGroupService.SelectedNode = null;

      string filename = "testExport.json";
      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = outputTestFile(filename, Array.Empty<string>());
      var importSettings = new ImportSettings();
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace;
      importSettings.Filename = exportSettings.Filename;
      m_instrumentGroupService.Export(exportSettings);
      m_instrumentGroupService.Import(importSettings);
      cleanupTestFile(filename);
      postImport();

      //check that the elements added are correct in terms of number
      Assert.AreEqual(m_instrumentGroups.Count, m_addedInstrumentGroups.Count + m_updatedInstrumentGroups.Count, "Number of loaded instrument groups are not correct.");

      //check node consistency
      checkParentIds();
      checkIds();
      checkChildParentAssociations();
      checkLoadedInstruments();
      checkLoadedInstrumentGroupsAgainstStaticDefinitions();
    }
  }
}
