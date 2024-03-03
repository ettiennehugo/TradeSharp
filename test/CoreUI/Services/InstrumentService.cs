using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;
using System.Diagnostics;

namespace TradeSharp.CoreUI.Testing.Services
{
  [TestClass]
  public class InstrumentService
  {
    //constants
    string[] csvWithoutIds = [
      "type,ticker,alternatetickers,name,description,exchange,inception date,price decimals,minimum movement,big point value,tag,attributes,secondary exchanges",
      "Stock,MSFT,\"MSFT^C,MSFT-C\",Microsoft,Microsoft Corporation,NYSE,2021-01-01T00:00:00,2,1,1,MSFT,3,\"Nasdaq,LSE\"",
      "3,AAPL,\"AAPL^C,AAPL~C\",Apple,Apple Corporation,NYSE,2021-01-01T00:00:00,2,1,1,AAPL,3,\"Nasdaq\",",
      "Stock,DISP,\"DISP^C,DISP-C\",Disney,Disney Corporation,NYSE,2021-01-01T00:00:00,2,1,1,DISP,3,\"Nasdaq,LSE\"",
      "3,NFLX,\"NFLX^C,NFLX~C\",Netflix,Netflix Corporation,NYSE,2021-01-01T00:00:00,2,1,1,NFLX,3,\"Nasdaq\",",
      "Stock,AUTO,\"AUTO^C,AUTO@C\",Autozone,Autozone Corporation,NYSE,2021-01-01T00:00:00,2,1,1,AUTO,3,\"Nasdaq,LSE\"",
      "3,PEP,\"PEP~C,PEP@C\",Pep Boys,Pep Boys Corporation,NYSE,2021-01-01T00:00:00,2,1,1,PEP,3,\"LSE\",",
      "Stock,GOOD,\"GOOD_C,GOOD-C\",Goodyear,Goodyear Corporation,NYSE,2021-01-01T00:00:00,2,1,1,GOOD,3,\"Nasdaq\",",
      "3,MICH,\"MICH~C,MICH^C\",Michelin,Michelin Corporation,NYSE,2021-01-01T00:00:00,2,1,1,MICH,3,\"LSE\","
    ];

    string[] csvWithIds = [
      "type,id,ticker,alternatetickers,name,description,exchange,inception date,price decimals,minimum movement,big point value,tag,attributes,secondary exchanges",
      "Stock,00000000-0000-0000-0000-000000000001,MSFT,\"MSFT^C,MSFT-C\",Microsoft,Microsoft Corporation,NYSE,2021-01-01T00:00:00,2,1,1,MSFT,3,\"Nasdaq,LSE\"",
      "3,00000000-0000-0000-0000-000000000002,AAPL,\"AAPL^C,AAPL~C\",Apple,Apple Corporation,NYSE,2021-01-01T00:00:00,2,1,1,AAPL,3,\"Nasdaq\"",
      "Stock,00000000-0000-0000-0000-000000000003,DISP,\"DISP^C,DISP-C\",Disney,Disney Corporation,NYSE,2021-01-01T00:00:00,2,1,1,DISP,3,\"Nasdaq,LSE\"",
      "3,00000000-0000-0000-0000-000000000004,NFLX,\"NFLX^C,NFLX~C\",Netflix,Netflix Corporation,NYSE,2021-01-01T00:00:00,2,1,1,NFLX,3,\"Nasdaq\"",
      "Stock,00000000-0000-0000-0000-000000000005,AUTO,\"AUTO^C,AUTO@C\",Autozone,Autozone Corporation,NYSE,2021-01-01T00:00:00,2,1,1,AUTO,3,\"Nasdaq,LSE\"",
      "3,00000000-0000-0000-0000-000000000006,PEP,\"PEP~C,PEP@C\",Pep Boys,Pep Boys Corporation,NYSE,2021-01-01T00:00:00,2,1,1,PEP,3,\"LSE\"",
      "Stock,00000000-0000-0000-0000-000000000007,GOOD,\"GOOD_C,GOOD-C\",Goodyear,Goodyear Corporation,NYSE,2021-01-01T00:00:00,2,1,1,GOOD,3,\"Nasdaq\"",
      "3,00000000-0000-0000-0000-000000000008,MICH,\"MICH~C,MICH^C\",Michelin,Michelin Corporation,NYSE,2021-01-01T00:00:00,2,1,1,MICH,3,\"LSE\""
    ];

    string[] jsonWithoutIds = [
      "[",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Ticker\": \"MSFT\",",
      "    \"AlternateTickers\": [\"MSFT^C\",\"MSFT-C\"],",
      "    \"Name\": \"Microsoft\",",
      "    \"Description\": \"Microsoft Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"MSFT\",",
      "    \"Attributes\": \"3\",",
      "    \"SecondaryExchanges\": [\"Nasdaq\",\"LSE\"]",
      "  },",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Ticker\": \"AAPL\",",
      "    \"AlternateTickers\": [\"AAPL^C\",\",AAPL~C\"],",
      "    \"Name\": \"Apple\",",
      "    \"Description\": \"Apple Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"AAPL\",",
      "    \"Attributes\": 3,",
      "    \"SecondaryExchanges\": [\"Nasdaq\"]",
      "  },",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Ticker\": \"DISP\",",
      "    \"AlternateTickers\": [\"DISP^C\",\"DISP-C\"],",
      "    \"Name\": \"Disney\",",
      "    \"Description\": \"Disney Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"DISP\",",
      "    \"Attributes\": 3,",
      "    \"SecondaryExchanges\": [\"Nasdaq\",\"LSE\"]",
      "  },",
      "  {",
      "    \"Type\": \"3\",",
      "    \"Ticker\": \"NFLX\",",
      "    \"AlternateTickers\": [\"NFLX^C\",\"NFLX~C\"],",
      "    \"Name\": \"Netflix\",",
      "    \"Description\": \"Netflix Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"NFLX\",",
      "    \"Attributes\": \"3\",",
      "    \"SecondaryExchanges\": [\"Nasdaq\"]",
      "  },",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Ticker\": \"AUTO\",",
      "    \"AlternateTickers\": [\"AUTO^C\",\"AUTO@C\"],",
      "    \"Name\": \"Autozone\",",
      "    \"Description\": \"Autozone Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"AUTO\",",
      "    \"Attributes\": \"3\",",
      "    \"SecondaryExchanges\": [\"Nasdaq\",\"LSE\"]",
      "  },",
      "  {",
      "    \"Type\": \"3\",",
      "    \"Ticker\": \"PEP\",",
      "    \"AlternateTickers\": [\"PEP~C\",\"PEP@C\"],",
      "    \"Name\": \"Pep Boys\",",
      "    \"Description\": \"Pep Boys Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"PEP\",",
      "    \"Attributes\": \"3\",",
      "    \"SecondaryExchanges\": [\"LSE\"]",
      "  },",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Ticker\": \"GOOD\",",
      "    \"AlternateTickers\": [\"GOOD_C\",\"GOOD-C\"],",
      "    \"Name\": \"Goodyear\",",
      "    \"Description\": \"Goodyear Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"GOOD\",",
      "    \"Attributes\": \"3\",",
      "    \"SecondaryExchanges\": [\"Nasdaq\"]",
      "  },",
      "  {",
      "    \"Type\": \"3\",",
      "    \"Ticker\": \"MICH\",",
      "    \"AlternateTickers\": [\"MICH~C\",\"MICH^C\"],",
      "    \"Name\": \"Michelin\",",
      "    \"Description\": \"Michelin Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"MICH\",",
      "    \"Attributes\": 3,",
      "    \"SecondaryExchanges\": [\"LSE\"]",
      "  }",
      "]"
    ];

    string[] jsonWithIds = [
      "[",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Id\": \"00000000-0000-0000-0000-000000000001\",",
      "    \"Ticker\": \"MSFT\",",
      "    \"AlternateTickers\": [\"MSFT^C\",\"MSFT-C\"],",
      "    \"Name\": \"Microsoft\",",
      "    \"Description\": \"Microsoft Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"MSFT\",",
      "    \"Attributes\": \"3\",",
      "    \"SecondaryExchanges\": [\"Nasdaq\",\"LSE\"]",
      "  },",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Id\": \"00000000-0000-0000-0000-000000000002\",",
      "    \"Ticker\": \"AAPL\",",
      "    \"AlternateTickers\": [\"AAPL^C\",\",AAPL~C\"],",
      "    \"Name\": \"Apple\",",
      "    \"Description\": \"Apple Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"AAPL\",",
      "    \"Attributes\": 3,",
      "    \"SecondaryExchanges\": [\"Nasdaq\"]",
      "  },",
      "  {",
      "    \"Type\": \"Stock\",",
      "    \"Id\": \"00000000-0000-0000-0000-000000000003\",",
      "    \"Ticker\": \"DISP\",",
      "    \"AlternateTickers\": [\"DISP^C\",\"DISP-C\"],",
      "    \"Name\": \"Disney\",",
      "    \"Description\": \"Disney Corporation\",",
      "    \"Exchange\": \"NYSE\",",
      "    \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "    \"PriceDecimals\": 2,",
      "    \"MinimumMovement\": 1,",
      "    \"BigPointValue\": 1,",
      "    \"Tag\": \"DISP\",",
      "    \"Attributes\": 3,",
      "    \"SecondaryExchanges\": [\"Nasdaq\",\"LSE\"]",
      "  },",
      "  {",
      "      \"Type\": \"3\",",
      "      \"Id\": \"00000000-0000-0000-0000-000000000004\",",
      "      \"Ticker\": \"NFLX\",",
      "      \"AlternateTickers\": [\"NFLX^C\",\"NFLX~C\"],",
      "      \"Name\": \"Netflix\",",
      "      \"Description\": \"Netflix Corporation\",",
      "      \"Exchange\": \"NYSE\",",
      "      \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "      \"PriceDecimals\": 2,",
      "      \"MinimumMovement\": 1,",
      "      \"BigPointValue\": 1,",
      "      \"Tag\": \"NFLX\",",
      "      \"Attributes\": \"3\",",
      "      \"SecondaryExchanges\": [\"Nasdaq\"]",
      "    },",
      "    {",
      "      \"Type\": \"Stock\",",
      "      \"Id\": \"00000000-0000-0000-0000-000000000005\",",
      "      \"Ticker\": \"AUTO\",",
      "      \"AlternateTickers\": [\"AUTO^C\",\"AUTO@C\"],",
      "      \"Name\": \"Autozone\",",
      "      \"Description\": \"Autozone Corporation\",",
      "      \"Exchange\": \"NYSE\",",
      "      \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "      \"PriceDecimals\": 2,",
      "      \"MinimumMovement\": 1,",
      "      \"BigPointValue\": 1,",
      "      \"Tag\": \"AUTO\",",
      "      \"Attributes\": \"3\",",
      "      \"SecondaryExchanges\": [\"Nasdaq\",\"LSE\"]",
      "    },",
      "    {",
      "      \"Type\": \"3\",",
      "      \"Id\": \"00000000-0000-0000-0000-000000000006\",",
      "      \"Ticker\": \"PEP\",",
      "      \"AlternateTickers\": [\"PEP~C\",\"PEP@C\"],",
      "      \"Name\": \"Pep Boys\",",
      "      \"Description\": \"Pep Boys Corporation\",",
      "      \"Exchange\": \"NYSE\",",
      "      \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "      \"PriceDecimals\": 2,",
      "      \"MinimumMovement\": 1,",
      "      \"BigPointValue\": 1,",
      "      \"Tag\": \"PEP\",",
      "      \"Attributes\": \"3\",",
      "      \"SecondaryExchanges\": [\"LSE\"]",
      "    },",
      "    {",
      "      \"Type\": \"Stock\",",
      "      \"Id\": \"00000000-0000-0000-0000-000000000007\",",
      "      \"Ticker\": \"GOOD\",",
      "      \"AlternateTickers\": [\"GOOD_C\",\"GOOD-C\"],",
      "      \"Name\": \"Goodyear\",",
      "      \"Description\": \"Goodyear Corporation\",",
      "      \"Exchange\": \"NYSE\",",
      "      \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "      \"PriceDecimals\": 2,",
      "      \"MinimumMovement\": 1,",
      "      \"BigPointValue\": 1,",
      "      \"Tag\": \"GOOD\",",
      "      \"Attributes\": \"3\",",
      "      \"SecondaryExchanges\": [\"Nasdaq\"]",
      "    },",
      "    {",
      "      \"Type\": \"3\",",
      "      \"Id\": \"00000000-0000-0000-0000-000000000008\",",
      "      \"Ticker\": \"MICH\",",
      "      \"AlternateTickers\": [\"MICH~C\",\"MICH^C\"],",
      "      \"Name\": \"Michelin\",",
      "      \"Description\": \"Michelin Corporation\",",
      "      \"Exchange\": \"NYSE\",",
      "      \"InceptionDate\": \"2021-01-01T00:00:00\",",
      "      \"PriceDecimals\": 2,",
      "      \"MinimumMovement\": 1,",
      "      \"BigPointValue\": 1,",
      "      \"Tag\": \"MICH\",",
      "      \"Attributes\": 3,",
      "      \"SecondaryExchanges\": [\"LSE\"]",
      "    }",
      "]"
    ];

    //enums


    //types


    //attributes
    private Mock<ILogger<CoreUI.Services.InstrumentService>> m_logger;
    private Mock<IDatabase> m_database;
    private Mock<IInstrumentRepository> m_instrumentRepository;
    private Mock<IDialogService> m_dialogService;
    private List<Instrument> m_addedInstruments;
    private List<Instrument> m_updatedInstruments;
    private List<Instrument> m_allFileInstruments;
    private List<Instrument> m_instruments;
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
      m_country = new Country(Guid.Parse("11111111-0000-0000-0000-000000000001"), Country.DefaultAttributeSet, "TagValue", "en-US");
      m_timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
      m_global = new Exchange(Exchange.InternationalId, Exchange.DefaultAttributeSet, "Global", m_country.Id, "Global", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_nyse = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000001"), Exchange.DefaultAttributeSet, "NYSE", m_country.Id, "New York Stock Exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_nasdaq = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000002"), Exchange.DefaultAttributeSet, "Nasdaq", m_country.Id, "Nasdaq", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);
      m_lse = new Exchange(Guid.Parse("10000000-0000-0000-0000-000000000003"), Exchange.DefaultAttributeSet, "LSE", m_country.Id, "London Stock Exchange", m_timeZone, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, Guid.Empty);

      //create the test stock instruments and instrument groups
      var msft = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000001"), Instrument.DefaultAttributeSet, "MSFT", InstrumentType.Stock, "MSFT", new List<string> { "MSFT^C", "MSFT-C" }, "Microsoft", "Microsoft Corporation", new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id, m_lse.Id });
      var aapl = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000002"), Instrument.DefaultAttributeSet, "AAPL", InstrumentType.Stock, "AAPL", new List<string> { "AAPL^C", "AAPL~C" }, "Apple", "Apple Corporation", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id });
      var disp = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000003"), Instrument.DefaultAttributeSet, "DISP", InstrumentType.Stock, "DISP", new List<string> { "DISP^C", "DISP-C" }, "Disney", "Disney Corporation",  new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id, new List<Guid> { m_nasdaq.Id, m_lse.Id });
      var nflx = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000004"), Instrument.DefaultAttributeSet, "NFLX", InstrumentType.Stock, "NFLX", new List<string> { "NFLX^C", "NFLX~C" }, "Netflix", "Netflix Corporation",  new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id,  new List<Guid> { m_nasdaq.Id });
      var auto = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000005"), Instrument.DefaultAttributeSet, "AUTO", InstrumentType.Stock, "AUTO", new List<string> { "AUTO^C", "AUTO@C" }, "Autozone", "Autozone Corporation",  new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id,  new List<Guid> { m_nasdaq.Id, m_lse.Id });
      var pep = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000006"), Instrument.DefaultAttributeSet, "PEP", InstrumentType.Stock, "PEP", new List<string> { "PEP~C", "PEP@C" }, "Pep Boys", "Pep Boys Corporation",  new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id,  new List<Guid> { m_lse.Id });
      var good = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000007"), Instrument.DefaultAttributeSet, "GOOD", InstrumentType.Stock, "GOOD", new List<string> { "GOOD_C", "GOOD-C" }, "Goodyear", "Goodyear Corporation",  new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id,  new List<Guid> { m_nasdaq.Id });
      var mich = new Instrument(Guid.Parse("00000000-0000-0000-0000-000000000008"), Instrument.DefaultAttributeSet, "MICH", InstrumentType.Stock, "MICH", new List<string> { "MICH~C", "MICH^C" }, "Michelin", "Michelin Corporation",  new DateTime(2021, 1, 1, 0, 0, 0,DateTimeKind.Utc), Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, m_nyse.Id,  new List<Guid> { m_lse.Id });

      m_exchanges = new List<Exchange>{ m_global, m_nyse, m_nasdaq, m_lse };
      m_instruments = new List<Instrument>{ msft, aapl, disp, nflx, auto, pep, good, mich };

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

      m_database = new Mock<IDatabase>();
      m_instrumentRepository = new Mock<IInstrumentRepository>();
      m_dialogService = new Mock<IDialogService>();

      m_database.Setup(x => x.GetExchanges()).Returns(m_exchanges);
      m_database.Setup(x => x.GetInstruments()).Returns(m_instruments);
      m_dialogService.Setup(x => x.ShowStatusMessageAsync(It.IsAny<IDialogService.StatusMessageSeverity>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
      m_instrumentRepository.Setup(x => x.GetItems()).Returns(m_instruments);
      m_instrumentRepository.Setup(x => x.Update(It.IsAny<Instrument>())).Callback((Instrument x) => m_updatedInstruments.Add(x));
      m_instrumentRepository.Setup(x => x.Add(It.IsAny<Instrument>())).Callback((Instrument x) => m_addedInstruments.Add(x));

      m_instrumentService = new CoreUI.Services.InstrumentService(m_logger.Object, m_database.Object, m_instrumentRepository.Object, m_dialogService.Object);
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

    public string outputTestFile(string filename, string[] content)
    {
      string fullFilename = Path.Combine(Path.GetTempPath(), filename);
      File.WriteAllText(fullFilename, string.Join("\n", content));
      return fullFilename;
    }

    public void postImport()
    {
      m_allFileInstruments.AddRange(m_addedInstruments);
      m_allFileInstruments.AddRange(m_updatedInstruments);
    }

    public void checkImportedInstruments()
    {
      Assert.AreEqual(m_instruments.Count, m_allFileInstruments.Count, "Loaded instrument counts are not correct");
      foreach (var instrument in m_allFileInstruments)
      {
        var definedInstrument = m_instruments.Find(x => x.Id == instrument.Id || x.Ticker == instrument.Ticker);
        if (definedInstrument == null)
          Assert.Fail($"Instrument {instrument.Ticker} not found in the database");
        else
        {
          Assert.AreEqual(instrument.Id, definedInstrument.Id, "Id is not correct");
          Assert.AreEqual(instrument.Ticker, definedInstrument.Ticker, "Ticker is not correct");
          Assert.AreEqual(instrument.Name, definedInstrument.Name, "Name is not correct");
          Assert.AreEqual(instrument.Description, definedInstrument.Description, "Description is not correct");
          Assert.AreEqual(instrument.PrimaryExchangeId, definedInstrument.PrimaryExchangeId, "PrimaryExchangeId is not correct");
          Assert.AreEqual(instrument.InceptionDate, definedInstrument.InceptionDate, "InceptionDate is not correct");
          Assert.AreEqual(instrument.PriceDecimals, definedInstrument.PriceDecimals, "PriceDecimals is not correct");
          Assert.AreEqual(instrument.MinimumMovement, definedInstrument.MinimumMovement, "MinimumMovement");
          Assert.AreEqual(instrument.BigPointValue, definedInstrument.BigPointValue, "BigPointValue is not correct");
          Assert.AreEqual(instrument.Tag, definedInstrument.Tag, "Tag is not correct");
          Assert.AreEqual(instrument.AttributeSet, definedInstrument.AttributeSet, "AttributesSet is not correct");
          Assert.IsTrue(listEquals(instrument.SecondaryExchangeIds, definedInstrument.SecondaryExchangeIds), "SecondaryExchangeIds is not correct");
        }
      }
    }

    [TestMethod]
    public void Import_CsvWithoutIds_Success()
    {
      string filename = "testInstrumentsWithoutIds.csv";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, csvWithoutIds);
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace; //force call to our dummy repository
      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments();
    }

    [TestMethod]
    public void Import_CsvWithIds_Success()
    {
      string filename = "testInstrumentsWithIds.csv";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, csvWithIds);
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace; //force call to our dummy repository
      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments();
    }

    [TestMethod]
    public void Import_JsonWithoutIds_Success()
    {
      string filename = "testInstrumentsWithoutIds.json";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, jsonWithoutIds);
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace; //force call to our dummy repository
      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments();
    }

    [TestMethod]
    public void Import_JsonWithIds_Success()
    {
      string filename = "testInstrumentsWithIds.json";
      var importSettings = new ImportSettings();
      importSettings.Filename = outputTestFile(filename, jsonWithIds);
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace; //force call to our dummy repository
      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments();
    }

    [TestMethod]
    public void Export_Csv_Success()
    {
      m_instrumentService.Refresh(); //load the data from the mock repository

      string filename = "testExportWithoutIds.csv";
      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = outputTestFile(filename, csvWithoutIds);
      var importSettings = new ImportSettings();
      importSettings.Filename = exportSettings.Filename;
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace;
      m_instrumentService.Export(exportSettings);
      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments();
    }

    [TestMethod]
    public void Export_Json_Success()
    {
      m_instrumentService.Refresh(); //load the data from the mock repository

      string filename = "testExportWithoutIds.json";
      var exportSettings = new ExportSettings();
      exportSettings.ReplaceBehavior = ExportReplaceBehavior.Replace;
      exportSettings.Filename = outputTestFile(filename, jsonWithoutIds);
      var importSettings = new ImportSettings();
      importSettings.Filename = exportSettings.Filename;
      importSettings.ReplaceBehavior = ImportReplaceBehavior.Replace;
      m_instrumentService.Export(exportSettings);
      m_instrumentService.Import(importSettings);
      postImport();
      checkImportedInstruments();
    }
  }
}
