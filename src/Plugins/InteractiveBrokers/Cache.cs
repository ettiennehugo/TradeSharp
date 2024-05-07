using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using TradeSharp.Common;
using IBApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Database to locally store data specific to Interactive Brokers, e.g. contract details.
  /// </summary>
  public sealed class Cache
  {
    //constants
    /// <summary>
    /// Cache database tables used.
    /// </summary>
    public const string TableContracts = "Contracts";              //contract definition table
    public const string TableContractDetails = "ContractDetails";  //contract details table in cache

    //enums


    //types


    //attributes
    static private Cache? s_instance; 
    private string m_databaseFile;
    private SqliteConnection m_connection;
    private string m_connectionString;
    private ILogger m_logger;
    private Dictionary<int, Contract> m_contractsByTickerExchange;
    private Dictionary<int, Contract> m_contractsByConId;

    //constructors
    public static Cache GetInstance(ServiceHost host)
    {
      if (s_instance == null) s_instance = new Cache(host.Host.Services.GetRequiredService<ILogger<Cache>>(), host.Host, host.Configuration);
      return s_instance;
    }

    protected Cache(ILogger logger, IHost host, IPluginConfiguration configuration)
    {
      m_logger = logger;
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

    //public void StartTransaction()
    //{
    //  ExecuteCommand("BEGIN TRANSACTION");
    //}

    //public void EndTransaction(bool success)
    //{
    //  if (success)
    //    ExecuteCommand("END TRANSACTION");
    //  else
    //    ExecuteCommand("ROLLBACK TRANSACTION");
    //}

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

      return contract;
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
    /// <summary>
    /// Database utility functions.
    /// </summary>
    public int ExecuteCommand(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database non-query - {command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteNonQuery();
    }

    public object? ExecuteScalar(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database scalar read - {command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteScalar();
    }

    public SqliteDataReader ExecuteReader(string command, int timeout = -1)
    {
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteReader();
    }

    public string ToSqlSafeString(string? value)
    {
      if (value == null) return "";
      return value.Replace("\'", "\'\'");
    }

    public string FromSqlSafeString(string value)
    {
      return value.Replace("\'\'", "\'");
    }

    private void CreateTable(string name, string columns)
    {
      //https://sqlite.org/lang_createtable.html
      ExecuteCommand($"CREATE TABLE IF NOT EXISTS {name} ({columns})");
    }

    private void CreateIndex(string indexName, string tableName, bool unique, string columns)
    {
      //https://sqlite.org/lang_createindex.html
      if (unique)
        ExecuteCommand($"CREATE UNIQUE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
      else
        ExecuteCommand($"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columns})");
    }

    private void DropTable(string name)
    {
      //https://sqlite.org/lang_droptable.html
      ExecuteCommand($"DROP TABLE IF EXISTS {name}");
    }

    private void DropIndex(string name)
    {
      //https://sqlite.org/lang_dropindex.html
      ExecuteCommand($"DROP INDEX IF EXISTS {name}");
    }

    public int GetRowCount(string tableName, string where)
    {
      int count = 0;

      var command = m_connection.CreateCommand();
      command.CommandText = $"SELECT * FROM {tableName} WHERE {where}";

      using (var reader = command.ExecuteReader())
      {
        while (reader.Read()) count++;
      }

      return count;
    }

    private void CreateSchema()
    {
      lock (this)
      {
        CreateContractsTable();
        CreateStockContractDetails();
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
  }
}
