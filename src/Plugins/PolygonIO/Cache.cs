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
    public string Acronym { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Mic { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OperatingMic { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
  }

  public class Tickers
  {
    public string Ticker { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string Cik { get; set; } = string.Empty;
    public string CompositeFigi { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.Now;
    public string Locale { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PrimaryExchange { get; set; } = string.Empty;
    public string ShareClassFigi { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
  }

  /// <summary>
  /// Ticker details structure.
  /// </summary>
  public class TickerDetails
  {
    public string Ticker { get; set; } = string.Empty;
    public string TickerRoot { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string PrimaryExchange { get; set; } = string.Empty;
    public string Cik { get; set; } = string.Empty;
    public string CompositeFigi { get; set; } = string.Empty;
    public string ShareClassFigi { get; set; } = string.Empty;
    public long ShareClassSharesOutstanding { get; set; }
    public double MarketCap { get; set; }
    public int TotalEmployees { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string SicCode { get; set; } = string.Empty;
    public string SicDescription { get; set; } = string.Empty;
    public string HomepageUrl { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public DateTime ListDate { get; set; } = TradeSharp.Common.Constants.DefaultMinimumDateTime;
    public int RoundLot { get; set; }
    public long WeightedSharesOutstanding { get; set; }
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
    public const string TableTickerDetails = "TickerDetails";

    //enums


    //types


    //attributes
    static protected Cache? s_instance = null;
    protected IPluginConfiguration m_configuration;
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
      createTickersTable();
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
            Acronym = FromSqlSafeString(reader.GetString(0)), // Acronym
            AssetClass = FromSqlSafeString(reader.GetString(1)), // AssetClass
            Id = reader.GetInt32(2), // Id
            Locale = FromSqlSafeString(reader.GetString(3)), // Locale
            Mic = FromSqlSafeString(reader.GetString(4)), // Mic
            Name = FromSqlSafeString(reader.GetString(5)), // Name
            OperatingMic = FromSqlSafeString(reader.GetString(6)), // OperatingMic
            ParticipantId = FromSqlSafeString(reader.GetString(7)), // ParticipantId
            Type = FromSqlSafeString(reader.GetString(8)), // Type
            Url = FromSqlSafeString(reader.GetString(9)) // Url
          };
          exchanges.Add(exchange);
        }
      }

      return exchanges;
    }

    public Instrument? From(Tickers ticker)
    {
      return m_instrumentService.Items.FirstOrDefault(i => i.Ticker == ticker.Ticker || i.AlternateTickers.Contains(ticker.Ticker));
    }

    public Instrument? From(TickerDetails ticker)
    {
      return m_instrumentService.Items.FirstOrDefault(i => i.Ticker == ticker.Ticker || i.AlternateTickers.Contains(ticker.Ticker));
    }

    public TickerDetails? From(Instrument instrument)
    {
      TickerDetails? tickerDetails = GetTickerDetails(instrument.Ticker);
      if (tickerDetails != null) return tickerDetails;

      foreach (var altTicker in instrument.AlternateTickers)
      {
        tickerDetails = GetTickerDetails(altTicker);
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
            Acronym = FromSqlSafeString(reader.GetString(0)), // Acronym
            AssetClass = FromSqlSafeString(reader.GetString(1)), // AssetClass
            Id = reader.GetInt32(2), // Id
            Locale = FromSqlSafeString(reader.GetString(3)), // Locale
            Mic = FromSqlSafeString(reader.GetString(4)), // Mic
            Name = FromSqlSafeString(reader.GetString(5)), // Name
            OperatingMic = FromSqlSafeString(reader.GetString(6)), // OperatingMic
            ParticipantId = FromSqlSafeString(reader.GetString(7)), // ParticipantId
            Type = FromSqlSafeString(reader.GetString(8)), // Type
            Url = FromSqlSafeString(reader.GetString(9)) // Url
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
              $"'{ToSqlSafeString(exchange.Acronym)}'," +
              $"'{ToSqlSafeString(exchange.AssetClass)}'," +
              $"{exchange.Id}, " +
              $"'{ToSqlSafeString(exchange.Locale)}', " +
              $"'{ToSqlSafeString(exchange.Mic)}', " +
              $"'{ToSqlSafeString(exchange.Name)}', " +
              $"'{ToSqlSafeString(exchange.OperatingMic)}', " +
              $"'{ToSqlSafeString(exchange.ParticipantId)}', " +
              $"'{ToSqlSafeString(exchange.Type)}', " +
              $"'{ToSqlSafeString(exchange.Url)}'" +
            $")"
        );
      }
    }

    public IList<Tickers> GetTickers()
    {
      var tickers = new List<Tickers>();
      using (var reader = ExecuteReader($"SELECT * FROM {TableTickers}"))
      {
        while (reader.Read())
        {
          var ticker = new Tickers
          {
            Ticker = FromSqlSafeString(reader.GetString(0)), // Ticker
            Active = reader.GetBoolean(1), // Active
            Cik = FromSqlSafeString(reader.GetString(2)), // Cik
            CompositeFigi = FromSqlSafeString(reader.GetString(3)), // CompositeFigi
            CurrencyName = FromSqlSafeString(reader.GetString(4)), // CurrencyName
            LastUpdatedUtc = DateTime.FromBinary((long)reader.GetDecimal(5)), // LastUpdatedUtc
            Locale = FromSqlSafeString(reader.GetString(6)), // Locale
            Market = FromSqlSafeString(reader.GetString(7)), // Market
            Name = FromSqlSafeString(reader.GetString(8)), // Name
            PrimaryExchange = FromSqlSafeString(reader.GetString(9)), // PrimaryExchange
            ShareClassFigi = FromSqlSafeString(reader.GetString(10)), // ShareClassFigi
            Type = FromSqlSafeString(reader.GetString(11)) // Type
          };
          tickers.Add(ticker);
        }
      }

      return tickers;
    }

    public IList<TickerDetails> GetTickerDetails()
    {
      var tickers = new List<TickerDetails>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableTickerDetails}"))
      {
        while (reader.Read())
        {
          var ticker = new TickerDetails
          {
            Ticker = FromSqlSafeString(reader.GetString(0)), // Ticker
            TickerRoot = FromSqlSafeString(reader.GetString(1)), // TickerRoot
            Type = FromSqlSafeString(reader.GetString(2)), // Type
            Name = FromSqlSafeString(reader.GetString(3)), // Name
            Description = FromSqlSafeString(reader.GetString(4)), // Description
            Market = FromSqlSafeString(reader.GetString(5)), // Market
            Locale = FromSqlSafeString(reader.GetString(6)), // Locale
            CurrencyName = FromSqlSafeString(reader.GetString(7)), // CurrencyName
            PrimaryExchange = FromSqlSafeString(reader.GetString(8)), // PrimaryExchange
            Cik = FromSqlSafeString(reader.GetString(9)), // Cik
            CompositeFigi = FromSqlSafeString(reader.GetString(10)), // CompositeFigi
            ShareClassFigi = FromSqlSafeString(reader.GetString(11)), // ShareClassFigi
            ShareClassSharesOutstanding = reader.GetInt64(12), // ShareClassSharesOutstanding
            MarketCap = reader.GetInt64(13), // MarketCap
            TotalEmployees = reader.GetInt32(14), // TotalEmployees
            Phone = FromSqlSafeString(reader.GetString(15)), // Phone
            Address = FromSqlSafeString(reader.GetString(16)), // Address
            City = FromSqlSafeString(reader.GetString(17)), // City
            State = FromSqlSafeString(reader.GetString(18)), // State
            PostalCode = FromSqlSafeString(reader.GetString(19)), // PostalCode
            SicCode = FromSqlSafeString(reader.GetString(20)), // SicCode
            SicDescription = FromSqlSafeString(reader.GetString(21)), // SicDescription
            HomepageUrl = FromSqlSafeString(reader.GetString(22)), // HomepageUrl
            LogoUrl = FromSqlSafeString(reader.GetString(23)), // LogoUrl
            IconUrl = FromSqlSafeString(reader.GetString(24)), // IconUrl
            ListDate = DateTime.FromBinary((long)reader.GetDecimal(25)), // ListDate
            RoundLot = reader.GetInt32(26), // RoundLot
            WeightedSharesOutstanding = reader.GetInt64(27), // WeightedSharesOutstanding
            Active = reader.GetBoolean(28) // Active
          };
          tickers.Add(ticker);
        }
      }

      return tickers;
    }

    public void UpdateTickers(Tickers ticker)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableTickers} (Ticker, Active, Cik, CompositeFigi, CurrencyName, LastUpdatedUtc, Locale, Market, Name, PrimaryExchange, ShareClassFigi, Type) " +
            $"VALUES (" +
              $"'{ToSqlSafeString(ticker.Ticker)}'," +
              $"{ticker.Active}, " +
              $"'{ToSqlSafeString(ticker.Cik)}', " +
              $"'{ToSqlSafeString(ticker.CompositeFigi)}', " +
              $"'{ToSqlSafeString(ticker.CurrencyName.ToUpper())}', " +
              $"{ticker.LastUpdatedUtc.ToBinary()}, " +
              $"'{ToSqlSafeString(ticker.Locale)}', " +
              $"'{ToSqlSafeString(ticker.Market)}', " +
              $"'{ToSqlSafeString(ticker.Name)}', " +
              $"'{ToSqlSafeString(ticker.PrimaryExchange)}', " +
              $"'{ToSqlSafeString(ticker.ShareClassFigi)}', " +
              $"'{ToSqlSafeString(ticker.Type)}'" +
            $")"
        );
      }
    }

    public TickerDetails? GetTickerDetails(string ticker)
    {
      TickerDetails? tickerDetails = null;
      using (var reader = ExecuteReader($"SELECT * FROM {TableTickerDetails} WHERE Ticker = '{ticker}'"))
      {
        if (reader.Read())
        {
          tickerDetails = new TickerDetails
          {
            Ticker = FromSqlSafeString(reader.GetString(0)), // Ticker
            TickerRoot = FromSqlSafeString(reader.GetString(1)), // TickerRoot
            Type = FromSqlSafeString(reader.GetString(2)), // Type
            Name = FromSqlSafeString(reader.GetString(3)), // Name
            Description = FromSqlSafeString(reader.GetString(4)), // Description
            Market = FromSqlSafeString(reader.GetString(5)), // Market
            Locale = FromSqlSafeString(reader.GetString(6)), // Locale
            CurrencyName = FromSqlSafeString(reader.GetString(7)), // CurrencyName
            PrimaryExchange = FromSqlSafeString(reader.GetString(8)), // PrimaryExchange
            Cik = FromSqlSafeString(reader.GetString(9)), // Cik
            CompositeFigi = FromSqlSafeString(reader.GetString(10)), // CompositeFigi
            ShareClassFigi = FromSqlSafeString(reader.GetString(11)), // ShareClassFigi
            ShareClassSharesOutstanding = reader.GetInt64(12), // ShareClassSharesOutstanding
            MarketCap = reader.GetDouble(13), // MarketCap
            TotalEmployees = reader.GetInt32(14), // TotalEmployees
            Phone = FromSqlSafeString(reader.GetString(15)), // Phone
            Address = FromSqlSafeString(reader.GetString(16)), // Address
            City = FromSqlSafeString(reader.GetString(17)), // City
            State = FromSqlSafeString(reader.GetString(18)), // State
            PostalCode = FromSqlSafeString(reader.GetString(19)), // PostalCode
            SicCode = FromSqlSafeString(reader.GetString(20)), // SicCode
            SicDescription = FromSqlSafeString(reader.GetString(21)), // SicDescription
            HomepageUrl = FromSqlSafeString(reader.GetString(22)), // HomepageUrl
            LogoUrl = FromSqlSafeString(reader.GetString(23)), // LogoUrl
            IconUrl = FromSqlSafeString(reader.GetString(24)), // IconUrl
            ListDate = DateTime.FromBinary((long)reader.GetDecimal(25)), // ListDate
            RoundLot = reader.GetInt32(26), // RoundLot
            WeightedSharesOutstanding = reader.GetInt64(27), // WeightedSharesOutstanding
            Active = reader.GetBoolean(28) // Active
          };
          return tickerDetails;
        }
      }

      return tickerDetails;
    }

    public void UpdateTickerDetails(TickerDetails tickerDetails)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableTickerDetails} (Ticker, TickerRoot, Type, Name, Description, Market, MarketCap, Locale, CurrencyName, PrimaryExchange, Cik, CompositeFigi, ShareClassFigi, ShareClassSharesOutstanding, TotalEmployees, PhoneNumber, Address, City, State, PostalCode, SicCode, SicDescription, HomepageUrl, LogoUrl, IconUrl, ListDate, RoundLot, WeightedSharesOutstanding) " +
            $"VALUES (" +
              $"'{ToSqlSafeString(tickerDetails.Ticker)}'," +
              $"'{ToSqlSafeString(tickerDetails.TickerRoot)}'," +
              $"'{ToSqlSafeString(tickerDetails.Type)}', " +
              $"'{ToSqlSafeString(tickerDetails.Name)}', " +
              $"'{ToSqlSafeString(tickerDetails.Description)}', " +
              $"'{ToSqlSafeString(tickerDetails.Market)}', " +
              $"{tickerDetails.MarketCap}, " +
              $"'{ToSqlSafeString(tickerDetails.Locale)}', " +
              $"'{ToSqlSafeString(tickerDetails.CurrencyName.ToUpper())}', " +
              $"'{ToSqlSafeString(tickerDetails.PrimaryExchange)}', " +
              $"'{ToSqlSafeString(tickerDetails.Cik)}', " +
              $"'{ToSqlSafeString(tickerDetails.CompositeFigi)}', " +
              $"'{ToSqlSafeString(tickerDetails.ShareClassFigi)}', " +
              $"{tickerDetails.ShareClassSharesOutstanding}, " +
              $"{tickerDetails.TotalEmployees}, " +
              $"'{ToSqlSafeString(tickerDetails.Phone)}', " +
              $"'{ToSqlSafeString(tickerDetails.Address)}', " +
              $"'{ToSqlSafeString(tickerDetails.City)}', " +
              $"'{ToSqlSafeString(tickerDetails.State)}', " +
              $"'{ToSqlSafeString(tickerDetails.PostalCode)}', " +
              $"'{ToSqlSafeString(tickerDetails.SicCode)}', " +
              $"'{ToSqlSafeString(tickerDetails.SicDescription)}', " +
              $"'{ToSqlSafeString(tickerDetails.HomepageUrl)}', " +
              $"'{ToSqlSafeString(tickerDetails.LogoUrl)}', " +
              $"'{ToSqlSafeString(tickerDetails.IconUrl)}', " +
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

    protected void createTickersTable()
    {
      CreateTable(TableTickers, @"
        Ticker TEXT PRIMARY KEY,
        Active BOOLEAN,
        Cik TEXT,
        CompositeFigi TEXT,
        CurrencyName TEXT,
        LastUpdatedUtc NUMBER,
        Locale TEXT,
        Market TEXT,
        Name TEXT,
        PrimaryExchange TEXT,
        ShareClassFigi TEXT,
        Type TEXT");
    }

    protected void createTickerDetailsTable()
    {
      CreateTable(TableTickerDetails, @"
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
            ShareClassSharesOutstanding NUMBER,
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
