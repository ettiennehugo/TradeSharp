﻿using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using TradeSharp.Common;
using TradeSharp.Data;
using IBApi;
using Microsoft.Extensions.DependencyInjection;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Meta-data stored for the contract scanner.
  /// NOTE: This is needed to "discover" contracts defined on the Interactive Brokers system since they do not have an explicit API to just
  ///       get the contract definitions. If such an explicit API becomes available this class, its associated data table and API's can be removed.
  /// </summary>
  public sealed class ContractScannerMetaData
  {
    public string Ticker { get; set; } = "";
    public DateTime LastScanDateTime { get; set; } = DateTime.Now;
  }

  /// <summary>
  /// Database to locally store data specific to Interactive Brokers, e.g. contract details.
  /// </summary>
  public sealed class Cache: SqlLiteBase
  {
    //constants
    /// <summary>
    /// Cache database tables used.
    /// </summary>
    public const string TableContracts = "Contracts";              //contract definition table
    public const string TableContractDetails = "ContractDetails";  //contract details table in cache
    public const string TableContractScannerMetaData = "ContractScannerMetaData";  //contract scanner meta-data table

    //enums


    //types


    //attributes
    static private Cache? s_instance; 
    private ServiceHost m_serviceHost;
    private string m_databaseFile;
    private Dictionary<int, Contract> m_contractsByTickerExchange;
    private Dictionary<int, Contract> m_contractsByConId;

    //constructors
    public static Cache GetInstance(ServiceHost serviceHost)
    {
      if (s_instance == null) s_instance = new Cache(serviceHost.Host.Services.GetRequiredService<ILogger<Cache>>(), serviceHost, serviceHost.Configuration);
      return s_instance;
    }

    protected Cache(ILogger logger, ServiceHost serviceHost, IPluginConfiguration configuration): base(logger)
    {
      m_logger = logger;
      m_serviceHost = serviceHost;
      string tradeSharpHome = Environment.GetEnvironmentVariable(TradeSharp.Common.Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{TradeSharp.Common.Constants.TradeSharpHome}\" not defined.");
      m_databaseFile = Path.Combine(tradeSharpHome, TradeSharp.Common.Constants.DataDir, configuration.Configuration[TradeSharp.InteractiveBrokers.Constants.CacheKey]!.ToString());
      m_contractsByTickerExchange = new Dictionary<int, Contract>();
      m_contractsByConId = new Dictionary<int, Contract>();

      //setup database connection
      m_connectionString = new SqliteConnectionStringBuilder()
      {
        DataSource = m_databaseFile,
        Mode = SqliteOpenMode.ReadWriteCreate,
      }.ToString();
      m_connection = new SqliteConnection(m_connectionString);
      m_connection.Open();

      //create the data store schema
      CreateSchema();

      CreateDefaultObjects();
    }

    //finalizers
    ~Cache()
    {
      m_connection.Close();
    }

    //interface implementations
    /// <summary>
    /// Clears the cache of all contracts and starts recaching.
    /// </summary>
    public void Clear()
    {
      m_contractsByTickerExchange.Clear();
      m_contractsByConId.Clear();
    }

    /// <summary>
    /// Returns the set of defined symbols in the contract cache as a hash set to enable fast lookup.
    /// </summary>
    public HashSet<string> GetDefinedSymbols()
    {
      HashSet<string> result = new HashSet<string>();
      using (var reader = ExecuteReader($"SELECT Symbol FROM {TableContracts}"))
        while (reader.Read())
          result.Add(reader.GetString(0));
      return result;
    }

    /// <summary>
    /// Returns the meta-data for the contract scanner.
    /// </summary>
    public List<ContractScannerMetaData> GetContractScannerMetaData()
    {
      List<ContractScannerMetaData> result = new List<ContractScannerMetaData>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableContractScannerMetaData}"))
        while (reader.Read())
        {
          ContractScannerMetaData metaData = new ContractScannerMetaData
          {
            Ticker = reader.GetString(0),
            LastScanDateTime = DateTime.Parse(reader.GetString(1))
          };

          result.Add(metaData);
        }

      return result;
    }

    /// <summary>
    /// Return the set of cached headers in the Contract table of the cache.
    /// </summary>
    public List<Contract> GetContractHeaders()
    {
      List<Contract> contracts = new List<Contract>();

      using (var reader = ExecuteReader($"SELECT * FROM {TableContracts}"))
        while (reader.Read())
        {
          Contract contract = new Contract
          {
            ConId = reader.GetInt32(0),
            Symbol = reader.GetString(1),
            SecId = reader.GetString(2),
            SecIdType = reader.GetString(3),
            SecType = reader.GetString(4),
            Exchange = reader.GetString(5),
            PrimaryExch = reader.GetString(6),
            Currency = reader.GetString(7),
            LocalSymbol = reader.GetString(8),
            TradingClass = reader.GetString(9),
            LastTradeDateOrContractMonth = reader.GetString(10)
          };
          contracts.Add(contract);
        }

      return contracts;
    }

    /// <summary>
    /// Returns the set of defined Contract data in the contract cache - this would include the ContractDetails.
    /// </summary>
    public List<Contract> GetContracts()
    {
      List<Contract> contracts = new List<Contract>();
      using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} JOIN {TableContractDetails} USING (ConId)"))
        while (reader.Read())
        {
          string secType = reader.GetString(4);
          if (secType == Constants.ContractTypeStock)
          {
            ContractStock contract = new ContractStock
            {
              ConId = reader.GetInt32(0),
              Symbol = reader.GetString(1),
              SecId = reader.GetString(2),
              SecIdType = reader.GetString(3),
              SecType = reader.GetString(4),
              Exchange = reader.GetString(5),
              PrimaryExch = reader.GetString(6),
              Currency = reader.GetString(7),
              LocalSymbol = reader.GetString(8),
              TradingClass = reader.GetString(9),
              LastTradeDateOrContractMonth = reader.GetString(10),
              Cusip = reader.GetString(11),
              LongName = FromSqlSafeString(reader.GetString(12)),
              StockType = reader.GetString(13),
              IssueDate = reader.GetString(14),
              LastTradeTime = reader.GetString(15),
              Category = FromSqlSafeString(reader.GetString(16)),
              Subcategory = FromSqlSafeString(reader.GetString(17)),
              Industry = FromSqlSafeString(reader.GetString(18)),
              Ratings = reader.GetString(19),
              TimeZoneId = reader.GetString(20),
              TradingHours = reader.GetString(21),
              LiquidHours = reader.GetString(22),
              OrderTypes = reader.GetString(23),
              MarketName = FromSqlSafeString(reader.GetString(24)),
              ValidExchanges = FromSqlSafeString(reader.GetString(25)),
              Notes = FromSqlSafeString(reader.GetString(26))
            };
            contracts.Add(contract);
          }
          else
            m_logger.LogError($"ContractForInstrument - Unsupported security type - {secType}");
        }
      return contracts;
    }

    public Contract? GetContract(int conId)
    {
      Contract? contract = null;

      //try a cache hit
      if (m_contractsByConId.TryGetValue(conId, out contract)) return contract;

      //try to load item into cache
      using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} NATURAL JOIN {TableContractDetails} ON ConId = ConId WHERE {TableContracts}.ConId = '{conId}'"))
        if (reader.Read())
        {
          string secType = reader.GetString(4);
          if (secType == Constants.ContractTypeStock)
          {
            contract = new ContractStock
            {
              ConId = reader.GetInt32(0),
              Symbol = reader.GetString(1),
              SecId = reader.GetString(2),
              SecIdType = reader.GetString(3),
              SecType = reader.GetString(4),
              Exchange = reader.GetString(5),
              PrimaryExch = reader.GetString(6),
              Currency = reader.GetString(7),
              LocalSymbol = reader.GetString(8),
              TradingClass = reader.GetString(9),
              LastTradeDateOrContractMonth = reader.GetString(10),
              //ConId = reader.GetInt32(11),  - duplicate from right table
              Cusip = reader.GetString(12),
              LongName = FromSqlSafeString(reader.GetString(13)),
              StockType = reader.GetString(14),
              IssueDate = reader.GetString(15),
              LastTradeTime = reader.GetString(16),
              Category = FromSqlSafeString(reader.GetString(17)),
              Subcategory = FromSqlSafeString(reader.GetString(18)),
              Industry = FromSqlSafeString(reader.GetString(19)),
              Ratings = reader.GetString(20),
              TimeZoneId = reader.GetString(21),
              TradingHours = reader.GetString(22),
              LiquidHours = reader.GetString(23),
              OrderTypes = reader.GetString(24),
              MarketName = FromSqlSafeString(reader.GetString(25)),
              ValidExchanges = FromSqlSafeString(reader.GetString(26)),
              Notes = FromSqlSafeString(reader.GetString(27))
            };

            m_contractsByTickerExchange[contract.Symbol.GetHashCode() + contract.Exchange.GetHashCode()] = contract;    //assumes symbol and exchange are upprcase
            m_contractsByConId[conId] = contract;
          }
          else
            m_logger.LogError($"ContractForInstrument - Unsupported security type - {secType}");
        }
      return contract;
    }

    public Contract? GetContract(string ticker, string exchange)
    {
      Contract? contract = null;
      string tickerUpper = ticker.ToUpper();
      string exchangeUpper = exchange.ToUpper();

      //try a cache hit
      if (m_contractsByTickerExchange.TryGetValue(tickerUpper.GetHashCode() + exchangeUpper.GetHashCode(), out contract)) return contract;

      using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} JOIN {TableContractDetails} ON {TableContracts}.ConId = {TableContractDetails}.ConId WHERE {TableContracts}.Symbol = '{tickerUpper}' AND {TableContracts}.Exchange = '{exchangeUpper}'"))
        if (reader.Read())
        {
          string secType = reader.GetString(4);
          if (secType == Constants.ContractTypeStock)
          {
            contract = new ContractStock
            {
              ConId = reader.GetInt32(0),
              Symbol = reader.GetString(1),
              SecId = reader.GetString(2),
              SecIdType = reader.GetString(3),
              SecType = reader.GetString(4),
              Exchange = reader.GetString(5),
              PrimaryExch = reader.GetString(6),
              Currency = reader.GetString(7),
              LocalSymbol = reader.GetString(8),
              TradingClass = reader.GetString(9),
              LastTradeDateOrContractMonth = reader.GetString(10),
              //ConId = reader.GetInt32(11),  - duplicate from right table
              Cusip = reader.GetString(12),
              LongName = FromSqlSafeString(reader.GetString(13)),
              StockType = reader.GetString(14),
              IssueDate = reader.GetString(15),
              LastTradeTime = reader.GetString(16),
              Category = FromSqlSafeString(reader.GetString(17)),
              Subcategory = FromSqlSafeString(reader.GetString(18)),
              Industry = FromSqlSafeString(reader.GetString(19)),
              Ratings = reader.GetString(20),
              TimeZoneId = reader.GetString(21),
              TradingHours = reader.GetString(22),
              LiquidHours = reader.GetString(23),
              OrderTypes = reader.GetString(24),
              MarketName = FromSqlSafeString(reader.GetString(25)),
              ValidExchanges = FromSqlSafeString(reader.GetString(26)),
              Notes = FromSqlSafeString(reader.GetString(27))
            };

            m_contractsByTickerExchange[contract.Symbol.GetHashCode() + contract.Exchange.GetHashCode()] = contract;    //assumes symbol and exchange are upprcase
            m_contractsByConId[contract.ConId] = contract;
          }
          else
            m_logger.LogError($"ContractForInstrument - Unsupported security type - {secType}");
        }

      //if we did not find the contract and the default exchange was used try to find contract based on ticker
      if (contract == null && exchange == Constants.DefaultExchange)
      {
        List<Contract> contracts = new List<Contract>();
        using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} JOIN {TableContractDetails} ON {TableContracts}.ConId = {TableContractDetails}.ConId WHERE {TableContracts}.Symbol = '{tickerUpper}'"))
          while (reader.Read())
          {
            string secType = reader.GetString(4);
            if (secType == Constants.ContractTypeStock)
            {
              contract = new ContractStock
              {
                ConId = reader.GetInt32(0),
                Symbol = reader.GetString(1),
                SecId = reader.GetString(2),
                SecIdType = reader.GetString(3),
                SecType = reader.GetString(4),
                Exchange = reader.GetString(5),
                PrimaryExch = reader.GetString(6),
                Currency = reader.GetString(7),
                LocalSymbol = reader.GetString(8),
                TradingClass = reader.GetString(9),
                LastTradeDateOrContractMonth = reader.GetString(10),
                //ConId = reader.GetInt32(11),  - duplicate from right table
                Cusip = reader.GetString(12),
                LongName = FromSqlSafeString(reader.GetString(13)),
                StockType = reader.GetString(14),
                IssueDate = reader.GetString(15),
                LastTradeTime = reader.GetString(16),
                Category = FromSqlSafeString(reader.GetString(17)),
                Subcategory = FromSqlSafeString(reader.GetString(18)),
                Industry = FromSqlSafeString(reader.GetString(19)),
                Ratings = reader.GetString(20),
                TimeZoneId = reader.GetString(21),
                TradingHours = reader.GetString(22),
                LiquidHours = reader.GetString(23),
                OrderTypes = reader.GetString(24),
                MarketName = FromSqlSafeString(reader.GetString(25)),
                ValidExchanges = FromSqlSafeString(reader.GetString(26)),
                Notes = FromSqlSafeString(reader.GetString(27))
              };

              m_contractsByTickerExchange[contract.Symbol.GetHashCode() + contract.Exchange.GetHashCode()] = contract;    //assumes symbol and exchange are upprcase
              m_contractsByConId[contract.ConId] = contract;
              contracts.Add(contract);
            }
          }

        if (contracts.Count == 1) contract = contracts[0];
      }

      return contract;
    }

    public Instrument? GetInstrument(Contract contract)
    {
      return m_serviceHost.InstrumentService.Items.FirstOrDefault((i) => i.Equals(contract.SecId));
    }

    public void UpdateContractScannerMetaData(ContractScannerMetaData metaData)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableContractScannerMetaData} (Ticker, LastScanDateTime) " +
            $"VALUES (" +
              $"'{metaData.Ticker}', " +
              $"'{metaData.LastScanDateTime.ToString("yyyy-MM-dd HH:mm:ss")}' " +
            $")"
        );
      }
    }

    public void UpdateContract(IBApi.Contract contract)
    {
      lock (this)
      {
        //NOTE: This contract can contain a lot of null string fields so we need to check for nulls.
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableContracts} (ConId, Symbol, SecType, SecId, SecIdType, Exchange, PrimaryExchange, Currency, LocalSymbol, TradingClass, LastTradeDateOrContractMonth) " +
            $"VALUES (" +
              $"{contract.ConId}, " +
              $"'{contract.Symbol}', " +
              $"'{contract.SecType}', " +
              $"'{contract.SecId ?? ""}', " +
              $"'{contract.SecIdType ?? ""}', " +
              $"'{contract.Exchange ?? ""}', " +
              $"'{contract.PrimaryExch ?? ""}', " +
              $"'{contract.Currency ?? ""}', " +
              $"'{contract.LocalSymbol ?? ""}', " +
              $"'{contract.TradingClass ?? ""}', " +
              $"'{contract.LastTradeDateOrContractMonth ?? ""}' " +
            $")"
        );

        if (m_contractsByTickerExchange.Count > 0) m_contractsByTickerExchange.Remove(contract.Symbol.GetHashCode() + contract.Exchange!.GetHashCode());    //assumes symbol and exchange are upprcase
        if (m_contractsByConId.Count > 0) m_contractsByTickerExchange.Remove(contract.ConId);
      }
    }

    public void UpdateContractDetails(ContractDetails contract)
    {
      //TODO: Currently only stocks are supported for update so this method may or may not work correctly for other instrument types.
      // - maybe add some lookup from the Contracts table to get the type for the ConId.
      lock (this)
      {
        //NOTE: This contract can contain a lot of null string fields so we need to check for nulls.
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableContractDetails} (ConId, Cusip, LongName, StockType, IssueDate, LastTradeTime, Category, SubCategory, Industry, Ratings, TimeZoneId, TradingHours, LiquidHours, OrderTypes, MarketName, ValidExchanges, Notes) " +
            $"VALUES (" +
              $"{contract.Contract.ConId}, " +
              $"'{contract.Cusip ?? ""}', " +
              $"'{ToSqlSafeString(contract.LongName ?? "")}', " +
              $"'{contract.StockType ?? ""}', " +
              $"'{contract.IssueDate ?? ""}', " +
              $"'{contract.LastTradeTime ?? ""}', " +
              $"'{ToSqlSafeString(contract.Category ?? "")}', " +
              $"'{ToSqlSafeString(contract.Subcategory ?? "")}', " +
              $"'{ToSqlSafeString(contract.Industry ?? "")}', " +
              $"'{contract.Ratings ?? ""}', " +
              $"'{contract.TimeZoneId ?? ""}', " +
              $"'{contract.TradingHours ?? ""}', " +
              $"'{contract.LiquidHours ?? ""}', " +
              $"'{contract.OrderTypes ?? ""}', " +
              $"'{ToSqlSafeString(contract.MarketName ?? "")}', " +
              $"'{ToSqlSafeString(contract.ValidExchanges ?? "")}', " +
              $"'{ToSqlSafeString(contract.Notes ?? "")}' " +
            $")"
        );

        if (m_contractsByTickerExchange.Count > 0)  m_contractsByTickerExchange.Remove(contract.Contract.Symbol.GetHashCode() + contract.Contract.Exchange.GetHashCode());    //assumes symbol and exchange are upprcase
        if (m_contractsByConId.Count > 0)  m_contractsByTickerExchange.Remove(contract.Contract.ConId);
      }
    }

    //properties


    //methods
    public override void CreateSchema()
    {
      lock (this)
      {
        CreateContractsTable();
        CreateStockContractDetails();
        CreateContractScannerTable();
      }
    }

    public override int ClearDatabase()
    {
      lock (this)
      {
        int count = 0;
        count += ExecuteCommand($"DELETE FROM {TableContracts}");
        count += ExecuteCommand($"DELETE FROM {TableContractDetails}");
        count += ExecuteCommand($"DELETE FROM {TableContractScannerMetaData}");
        return count;
      }
    }

    private void CreateContractsTable()
    {
      //NOTE: This is not the complete structure but what would be needed for stocks.
      CreateTable(TableContracts,
        @"
        ConId INT PRIMARY KEY ON CONFLICT REPLACE,
        Symbol TEXT,
        SecId TEXT,
        SecIdType TEXT,
        SecType TEXT,
        Exchange TEXT,
        PrimaryExchange TEXT,
        Currency TEXT,
        LocalSymbol TEXT,
        TradingClass TEXT,
        LastTradeDateOrContractMonth TEXT
      ");
    }

    private void CreateStockContractDetails()
    {
      //NOTE: This is not the complete structure but what would be needed for stocks.
      CreateTable(TableContractDetails,
      @"
        ConId INT PRIMARY KEY ON CONFLICT REPLACE,
        Cusip TEXT,
        LongName TEXT,
        StockType TEXT,
        IssueDate TEXT,
        LastTradeTime TEXT,
        Category TEXT,
        SubCategory TEXT,
        Industry TEXT,
        Ratings TEXT,
        TimeZoneId TEXT,
        TradingHours TEXT,
        LiquidHours TEXT,
        OrderTypes TEXT,
        MarketName TEXT,
        ValidExchanges TEXT,
        Notes TEXT
      ");
    }

    private void CreateContractScannerTable()
    {
      CreateTable(TableContractScannerMetaData,
        @"
        Ticker TEXT,
        LastScanDateTime TEXT,
        PRIMARY KEY (Ticker)
      ");
    }

  }
}
