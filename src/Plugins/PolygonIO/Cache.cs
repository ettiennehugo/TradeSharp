using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.PolygonIO
{
  /// <summary>
  /// Exchange details structure.
  /// </summary>
  public class Exchange
  {
    public string Acronym { get; set; }
    public string AssetClass { get; set; }
    public int Id { get; set; }
    public string Locale { get; set; }
    public string Mic { get; set; }
    public string Name { get; set; }
    public string OperatingMic { get; set; }
    public string ParticipantId { get; set; }
    public string Type { get; set; }
    public string Url { get; set; }
  }

  /// <summary>
  /// Ticker details structure.
  /// </summary>
  public class TickerDetails
  {
    public string Ticker { get; set; }
    public string TickerRoot { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Market { get; set; }
    public string Locale { get; set; }
    public string CurrencyName { get; set; }
    public string PrimaryExchange { get; set; }
    public string Cik { get; set; }
    public string CompositeFigi { get; set; }
    public string ShareClassFigi { get; set; }
    public string ShareClassSharesOutstanding { get; set; }
    public string MarketCap { get; set; }
    public string TotalEmployees { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string SicCode { get; set; }
    public string SicDescription { get; set; }
    public string HomepageUrl { get; set; }
    public string LogoUrl { get; set; }
    public string IconUrl { get; set; }
    public DateTime ListDate { get; set; }
    public string RoundLot { get; set; }
    public string WeightedSharesOutstanding { get; set; }
    public bool Active { get; set; }
  }

  /// <summary>
  /// Local cache for Poligon.io provided data.
  /// </summary>
  public class Cache : SqlLiteBase
  {
    //constants
    public const string TableExchanges = "Exchanges";
    public const string TableTickers = "Tickers";

    //enums


    //types


    //attributes
    static protected Cache? s_instance = null;
    protected IPluginConfiguration m_configuration;
    protected string m_databaseFile;
    protected IInstrumentService m_instrumentService;

    //properties


    //constructors
    protected Cache(ILogger logger, string connectionString, IPluginConfiguration configuration) : base(logger, connectionString, true)
    {
      m_configuration = configuration;
    }

    //finalizers


    //interface implementations
    public static Cache GetInstance(ILogger logger, IPluginConfiguration configuration)
    {
      if (s_instance == null)
      {
        string tradeSharpHome = Environment.GetEnvironmentVariable(TradeSharp.Common.Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{TradeSharp.Common.Constants.TradeSharpHome}\" not defined.");
        string databaseFile = Path.Combine(tradeSharpHome, TradeSharp.Common.Constants.DataDir, configuration.Configuration[PolygonIO.Constants.CacheKey]!.ToString());
        s_instance = new Cache(logger, databaseFile, configuration);
      }
      return s_instance;
    }

    //methods
    public override int ClearDatabase() { return 0; }

    public override void CreateSchema()
    {
      createExchangeTable();
      createTickerDetailsTable();
    }

    public override void CreateDefaultObjects() { }

    public IList<Exchange> GetExchanges()
    {
      var exchanges = new List<Exchange>();
      using (var reader = ExecuteReader($"SELECT * FROM {TableExchanges}"))
      {
        while (reader.Read())
        {
          var exchange = new Exchange
          {
            Acronym = reader.GetString(0), // Acronym
            AssetClass = reader.GetString(1), // AssetClass
            Id = reader.GetInt32(2), // Id
            Locale = reader.GetString(3), // Locale
            Mic = reader.GetString(4), // Mic
            Name = reader.GetString(5), // Name
            OperatingMic = reader.GetString(6), // OperatingMic
            ParticipantId = reader.GetString(7), // ParticipantId
            Type = reader.GetString(8), // Type
            Url = reader.GetString(9) // Url
          };
          exchanges.Add(exchange);
        }
      }

      return exchanges;
    }

    public Instrument? From(TickerDetails ticker)
    {
      return m_instrumentService.Items.FirstOrDefault(i => i.Ticker == ticker.Ticker || i.AlternateTickers.Contains(ticker.Ticker));
    }

    public TickerDetails? From(Instrument instrument)
    {
      TickerDetails? tickerDetails = GetTicker(instrument.Ticker);
      if (tickerDetails != null) return tickerDetails;

      foreach (var altTicker in instrument.AlternateTickers)
      {
        tickerDetails = GetTicker(altTicker);
        if (tickerDetails != null) return tickerDetails;
      }

      return tickerDetails;
    }

    public Exchange? GetExchange(string mic)
    {
      Exchange? exchange = null;
      using (var reader = ExecuteReader($"SELECT * FROM {TableExchanges} WHERE Mic = '{mic}'"))
      {
        if (reader.Read())
        {
          exchange = new Exchange
          {
            Acronym = reader.GetString(0), // Acronym
            AssetClass = reader.GetString(1), // AssetClass
            Id = reader.GetInt32(2), // Id
            Locale = reader.GetString(3), // Locale
            Mic = reader.GetString(4), // Mic
            Name = reader.GetString(5), // Name
            OperatingMic = reader.GetString(6), // OperatingMic
            ParticipantId = reader.GetString(7), // ParticipantId
            Type = reader.GetString(8), // Type
            Url = reader.GetString(9) // Url
          };
          return exchange;
        }
      }

      return exchange;
    }

    public void UpdateExchange(Exchange exchange)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableExchanges} (Acronym, AssetClass, Id, Locale, Mic, Name, OperatingMic, ParticipantId, Type, Url) " +
            $"VALUES (" +
              $"'{exchange.Acronym}'," +
              $"'{exchange.AssetClass}'," +
              $"{exchange.Id}, " +
              $"'{exchange.Locale}', " +
              $"'{exchange.Mic}', " +
              $"'{exchange.Name}', " +
              $"'{exchange.OperatingMic}', " +
              $"'{exchange.ParticipantId}', " +
              $"'{exchange.Type}', " +
              $"'{exchange.Url}'" +
            $")"
        );
      }
    }

    public IList<TickerDetails> GetTickers()
    {
      var tickers = new List<TickerDetails>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableTickers}"))
      {
        while (reader.Read())
        {
          var ticker = new TickerDetails
          {
            Ticker = reader.GetString(0), // Ticker
            TickerRoot = reader.GetString(1), // TickerRoot
            Type = reader.GetString(2), // Type
            Name = reader.GetString(3), // Name
            Description = reader.GetString(4), // Description
            Market = reader.GetString(5), // Market
            Locale = reader.GetString(6), // Locale
            CurrencyName = reader.GetString(7), // CurrencyName
            PrimaryExchange = reader.GetString(8), // PrimaryExchange
            Cik = reader.GetString(9), // Cik
            CompositeFigi = reader.GetString(10), // CompositeFigi
            ShareClassFigi = reader.GetString(11), // ShareClassFigi
            ShareClassSharesOutstanding = reader.GetString(12), // ShareClassSharesOutstanding
            MarketCap = reader.GetString(13), // MarketCap
            TotalEmployees = reader.GetString(14), // TotalEmployees
            Phone = reader.GetString(15), // Phone
            Address = reader.GetString(16), // Address
            City = reader.GetString(17), // City
            State = reader.GetString(18), // State
            PostalCode = reader.GetString(19), // PostalCode
            SicCode = reader.GetString(20), // SicCode
            SicDescription = reader.GetString(21), // SicDescription
            HomepageUrl = reader.GetString(22), // HomepageUrl
            LogoUrl = reader.GetString(23), // LogoUrl
            IconUrl = reader.GetString(24), // IconUrl
            ListDate = DateTime.FromBinary((long)reader.GetDecimal(25)), // ListDate
            RoundLot = reader.GetString(26), // RoundLot
            WeightedSharesOutstanding = reader.GetString(27), // WeightedSharesOutstanding
            Active = reader.GetBoolean(28) // Active
          };
          tickers.Add(ticker);
        }
      }

      return tickers;
    }

    public TickerDetails? GetTicker(string ticker)
    {
      TickerDetails? tickerDetails = null;
      using (var reader = ExecuteReader($"SELECT * FROM {TableTickers} WHERE Ticker = '{ticker}'"))
      {
        if (reader.Read())
        {
          tickerDetails = new TickerDetails
          {
            Ticker = reader.GetString(0), // Ticker
            TickerRoot = reader.GetString(1), // TickerRoot
            Type = reader.GetString(2), // Type
            Name = reader.GetString(3), // Name
            Description = reader.GetString(4), // Description
            Market = reader.GetString(5), // Market
            Locale = reader.GetString(6), // Locale
            CurrencyName = reader.GetString(7), // CurrencyName
            PrimaryExchange = reader.GetString(8), // PrimaryExchange
            Cik = reader.GetString(9), // Cik
            CompositeFigi = reader.GetString(10), // CompositeFigi
            ShareClassFigi = reader.GetString(11), // ShareClassFigi
            ShareClassSharesOutstanding = reader.GetString(12), // ShareClassSharesOutstanding
            MarketCap = reader.GetString(13), // MarketCap
            TotalEmployees = reader.GetString(14), // TotalEmployees
            Phone = reader.GetString(15), // Phone
            Address = reader.GetString(16), // Address
            City = reader.GetString(17), // City
            State = reader.GetString(18), // State
            PostalCode = reader.GetString(19), // PostalCode
            SicCode = reader.GetString(20), // SicCode
            SicDescription = reader.GetString(21), // SicDescription
            HomepageUrl = reader.GetString(22), // HomepageUrl
            LogoUrl = reader.GetString(23), // LogoUrl
            IconUrl = reader.GetString(24), // IconUrl
            ListDate = DateTime.FromBinary((long)reader.GetDecimal(25)), // ListDate
            RoundLot = reader.GetString(26), // RoundLot
            WeightedSharesOutstanding = reader.GetString(27), // WeightedSharesOutstanding
            Active = reader.GetBoolean(28) // Active
          };
          return tickerDetails;
        }
      }

      return tickerDetails;
    }

    public void UpdateTicker(TickerDetails tickerDetails)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableTickers} (Ticker, TickerRoot, Type, Name, Description, Market, MarketCap, Locale, CurrencyName, PrimaryExchange, Cik, CompositeFigi, ShareClassFigi, ShareClassSharesOutstanding, TotalEmployees, PhoneNumber, Address, City, State, PostalCode, SicCode, SicDescription, HomepageUrl, LogoUrl, IconUrl, ListDate, RoundLot, WeightedSharesOutstanding) " +
            $"VALUES (" +
              $"'{tickerDetails.Ticker}'," +
              $"'{tickerDetails.TickerRoot}'," +
              $"'{tickerDetails.Type}', " +
              $"'{tickerDetails.Name}', " +
              $"'{tickerDetails.Description}', " +
              $"'{tickerDetails.Market}', " +
              $"'{tickerDetails.MarketCap}', " +
              $"'{tickerDetails.Locale}', " +
              $"'{tickerDetails.CurrencyName}', " +
              $"'{tickerDetails.PrimaryExchange}', " +
              $"'{tickerDetails.Cik}', " +
              $"'{tickerDetails.CompositeFigi}', " +
              $"'{tickerDetails.ShareClassFigi}', " +
              $"'{tickerDetails.ShareClassSharesOutstanding}', " +
              $"'{tickerDetails.TotalEmployees}', " +
              $"'{tickerDetails.Phone}', " +
              $"'{tickerDetails.Address}', " +
              $"'{tickerDetails.City}', " +
              $"'{tickerDetails.State}', " +
              $"'{tickerDetails.PostalCode}', " +
              $"'{tickerDetails.SicCode}', " +
              $"'{tickerDetails.SicDescription}', " +
              $"'{tickerDetails.HomepageUrl}', " +
              $"'{tickerDetails.LogoUrl}', " +
              $"'{tickerDetails.IconUrl}', " +
              $"{tickerDetails.ListDate.ToBinary()}, " +
              $"{tickerDetails.RoundLot}, " +
              $"'{tickerDetails.WeightedSharesOutstanding}'" +
            $")"
        );
      }
    }

    protected void createExchangeTable()
    {
      CreateTable(TableExchanges, @"
        Acronym TEXT,
        AssetClass TEXT,
        Id INTEGER PRIMARY KEY,
        Locale TEXT,
        Mic TEXT,
        Name TEXT,
        OperatingMic TEXT,
        ParticipantId TEXT,
        Type TEXT,
        Url TEXT");
    }

    protected void createTickerDetailsTable()
    {
      //"results": {
      //    "active": true,
      //"address": {
      //      "address1": "One Apple Park Way",
      //  "city": "Cupertino",
      //  "postal_code": "95014",
      //  "state": "CA"
      //},
      //"branding": {
      //  "icon_url": "https://api.polygon.io/v1/reference/company-branding/d3d3LmFwcGxlLmNvbQ/images/2022-01-10_icon.png",
      //  "logo_url": "https://api.polygon.io/v1/reference/company-branding/d3d3LmFwcGxlLmNvbQ/images/2022-01-10_logo.svg"
      //},
      //"cik": "0000320193",
      //"composite_figi": "BBG000B9XRY4",
      //"currency_name": "usd",
      //"description": "Apple designs a wide variety of consumer electronic devices, including smartphones (iPhone), tablets (iPad), PCs (Mac), smartwatches (Apple Watch), AirPods, and TV boxes (Apple TV), among others. The iPhone makes up the majority of Apple's total revenue. In addition, Apple offers its customers a variety of services such as Apple Music, iCloud, Apple Care, Apple TV+, Apple Arcade, Apple Card, and Apple Pay, among others. Apple's products run internally developed software and semiconductors, and the firm is well known for its integration of hardware, software and services. Apple's products are distributed online as well as through company-owned stores and third-party retailers. The company generates roughly 40% of its revenue from the Americas, with the remainder earned internationally.",
      //"homepage_url": "https://www.apple.com",
      //"list_date": "1980-12-12",
      //"locale": "us",
      //"market": "stocks",
      //"market_cap": 2771126040150,
      //"name": "Apple Inc.",
      //"phone_number": "(408) 996-1010",
      //"primary_exchange": "XNAS",
      //"round_lot": 100,
      //"share_class_figi": "BBG001S5N8V8",
      //"share_class_shares_outstanding": 16406400000,
      //"sic_code": "3571",
      //"sic_description": "ELECTRONIC COMPUTERS",
      //"ticker": "AAPL",
      //"ticker_root": "AAPL",
      //"total_employees": 154000,
      //"type": "CS",
      //"weighted_shares_outstanding": 16334371000

      CreateTable(TableTickers, @"
            Ticker TEXT PRIMARY KEY,
            TickerRoot TEXT,
            Type TEXT,
            Name TEXT,
            Description TEXT,
            Market TEXT,
            MarketCap NUMBER,
            Locale TEXT,
            CurrencyName TEXT,
            PrimaryExchange TEXT,
            Cik TEXT,
            CompositeFigi TEXT,
            ShareClassFigi TEXT,
            ShareClassSharesOutstanding TEXT,
            TotalEmployees NUMBER,
            PhoneNumber TEXT,
            Address TEXT,
            City TEXT,
            State TEXT,
            PostalCode TEXT,
            SicCode TEXT,
            SicDescription TEXT,
            HomepageUrl TEXT,
            LogoUrl TEXT,
            IconUrl TEXT,
            ListDate NUMBER,
            RoundLot NUMBER,
            WeightedSharesOutstanding NUMBER,
            Active BOOLEAN");
    }
  }
}
