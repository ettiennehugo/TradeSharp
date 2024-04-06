using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using TradeSharp.Common;
using IBApi;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Database to locally store data specific to Interactive Brokers, e.g. contract details.
  /// </summary>
  public sealed class IBCache
  {
    //constants
    /// <summary>
    /// Cache database tables used.
    /// </summary>
    public const string TableContracts = "Contracts";           //base table for contract mapping table
    public const string TableStockContracts = "StockContracts"; //stock specific contract details

    //enums


    //types


    //attributes
    private string m_databaseFile;
    private SqliteConnection m_connection;
    private string m_connectionString;
    private ILogger m_logger;

    //constructors
    public IBCache(ILogger logger, IPluginConfiguration configuration)
    {
      m_logger = logger;

      string tradeSharpHome = Environment.GetEnvironmentVariable(TradeSharp.Common.Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{TradeSharp.Common.Constants.TradeSharpHome}\" not defined.");
      m_databaseFile = Path.Combine(tradeSharpHome, TradeSharp.Common.Constants.DataDir, configuration.Configuration[TradeSharp.InteractiveBrokers.Constants.CacheKey]!.ToString());

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
    ~IBCache()
    {
      m_connection.Close();
    }

    //interface implementations


    //properties


    //methods
    /// <summary>
    /// Database utility functions.
    /// </summary>
    public int ExecuteCommand(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database non-query - ${command}");
      var commandObj = m_connection.CreateCommand();
      commandObj.CommandTimeout = timeout >= 0 ? timeout : m_connection.DefaultTimeout;
      commandObj.CommandText = command;
      return commandObj.ExecuteNonQuery();
    }

    public object? ExecuteScalar(string command, int timeout = -1)
    {
      if (Debugging.DatabaseCalls) m_logger.LogInformation($"Database scalar read - ${command}");
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

    public string ToSqlSafeString(string value)
    {
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
      CreateTable(TableStockContracts,
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

    public Contract? ContractForInstrument(string ticker, string exchange)
    {
      Contract? contract = null;
      using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} NATURAL JOIN {TableStockContracts} ON ConId = ConId WHERE {TableContracts}.Symbol = '{ticker}' AND {TableContracts}.PrimaryExchange = '{exchange}'"))
        if (reader.Read())
        {
          string secType = reader.GetString(4);
          if (secType == IBClient.ContractTypeStock)
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
              LongName = reader.GetString(13),
              StockType = reader.GetString(14),
              IssueDate = reader.GetString(15),
              LastTradeTime = reader.GetString(16),
              Category = reader.GetString(17),
              Subcategory = reader.GetString(18),
              Industry = reader.GetString(19),
              Ratings = reader.GetString(20),
              TimeZoneId = reader.GetString(21),
              TradingHours = reader.GetString(22),
              LiquidHours = reader.GetString(23),
              OrderTypes = reader.GetString(24),
              MarketName = reader.GetString(25),
              ValidExchanges = reader.GetString(26),
              Notes = reader.GetString(27)
            };
          }
          else
            m_logger.LogError($"ContractForInstrument - Unsupported security type - {secType}");
        }

      return contract;
    }

    //public Contract? ContractForInstrument(Instrument instrument)
    //{
    //  Exchange? primaryExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
    //  if (primaryExchange == null) return null;
    //  Contract? contract = ContractForInstrument(instrument.Ticker, primaryExchange.Name);
    //  if (contract == null)
    //    foreach (string ticker in instrument.AlternateTickers)
    //    {
    //      contract = ContractForInstrument(ticker, primaryExchange.Name);
    //      if (contract != null) break;
    //    }
    //  return contract;
    //}

    public void UpdateContract(IBApi.Contract contract)
    {
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableContracts} (ConId, Symbol, SecType, SecId, SecIdType, Exchange, PrimaryExchange, Currency, LocalSymbol, TradingClass, LastTradeDateOrContractMonth) " +
            $"VALUES (" +
              $"{contract.ConId}, " +
              $"'{contract.Symbol}', " +
              $"'{contract.SecType}', " +
              $"'{contract.SecId}', " +
              $"'{contract.SecIdType}', " +
              $"'{contract.Exchange}', " +
              $"'{contract.PrimaryExch}', " +
              $"'{contract.Currency}', " +
              $"'{contract.LocalSymbol}', " +
              $"'{contract.TradingClass}', " +
              $"'{contract.LastTradeDateOrContractMonth}' " +
            $")"
        );
      }
    }

    public void UpdateContractDetails(ContractDetails contract)
    {
      //TODO: Currently only stocks are supported for update so this method may or may not work correctly for other instrument types.
      // - maybe add some lookup from the Contracts table to get the type for the ConId.
      lock (this)
      {
        ExecuteCommand(
          $"INSERT OR REPLACE INTO {TableStockContracts} (ConId, Cusip, LongName, StockType, IssueDate, LastTradeTime, Category, SubCategory, Industry, Ratings, TimeZoneId, TradingHours, LiquidHours, OrderTypes, MarketName, ValidExchanges, Notes) " +
            $"VALUES (" +
              $"{contract.UnderConId}, " +
              $"'{contract.Cusip}', " +
              $"'{contract.LongName}', " +
              $"'{contract.StockType}', " +
              $"'{contract.IssueDate}', " +
              $"'{contract.LastTradeTime}', " +
              $"'{contract.Category}', " +
              $"'{contract.Subcategory}', " +
              $"'{contract.Industry}', " +
              $"'{contract.Ratings}', " +
              $"'{contract.TimeZoneId}', " +
              $"'{contract.TradingHours}', " +
              $"'{contract.LiquidHours}', " +
              $"'{contract.OrderTypes}', " +
              $"'{contract.MarketName}', " +
              $"'{contract.ValidExchanges}', " +
              $"'{contract.Notes}' " +
            $")"
        );
      }
    }
  }
}
