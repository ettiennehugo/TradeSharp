using IBApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System.Reflection;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter singleton for TWS API client responses to work with the TradeSharp broker and data plugins.
  /// It acts as a certral storage for common information required to interact with the TWS API.
  /// https://interactivebrokers.github.io/tws-api/client_wrapper.html#The
  /// https://interactivebrokers.github.io/tws-api/interfaceIBApi_1_1EWrapper.html
  /// Pacing violations - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#historical-pacing-limitations
  /// </summary>
  public class IBApiAdapter : EWrapper
  {
    //constants
    /// <summary>
    /// Cache database tables used.
    /// </summary>
    public const string TableContracts = "Contracts";           //base table for contract mapping table
    public const string TableStockContracts = "StockContracts"; //stock specific contract details

    /// <summary>
    /// Supported contract types.
    /// </summary>
    public const string ContractTypeStock = "STK";
    public const string ContractTypeCFD = "CFD";
    public const string ContractTypeBond = "BOND";
    public const string ContractTypeFuture = "FUT";
    public const string ContractTypeFutureOption = "FOP";
    public const string ContractTypeOption = "OPT";
    public const string ContractTypeIndex = "IND";
    public const string ContractTypeForex = "CASH";
    public const string ContractTypeMutualFund = "FUND";
    public const string ContractTypeFutureCombo = "BAG";
    public const string ContractTypeStockCombo = "CS";
    public const string ContractTypeIndexCombo = "IND";
    public const string ContractTypeCommodity = "CMDTY";

    /// <summary>
    /// Historical durations and bar size types.
    /// </summary>
    public const string DurationSeconds = "S";
    public const string DurationDays = "D";
    public const string DurationWeeks = "W";
    public const string DurationMonths = "M";
    public const string DurationYears = "Y";

    /// <summary>
    /// Supported bar sizes.
    /// </summary>
    public const string BarSize1Sec = "1";
    public const string BarSize5Sec = "5";
    public const string BarSize10Sec = "10";
    public const string BarSize15Sec = "15";
    public const string BarSize30Sec = "30";
    public const string BarSize1Min = "1";
    public const string BarSize2Min = "2";
    public const string BarSize3Min = "3";
    public const string BarSize5Min = "5";
    public const string BarSize10Min = "10";
    public const string BarSize15Min = "15";
    public const string BarSize20Min = "20";
    public const string BarSize30Min = "30";
    public const string BarSize1Hour = "1";
    public const string BarSize2Hour = "2";
    public const string BarSize3Hour = "3";
    public const string BarSize4Hour = "4";
    public const string BarSize8Hour = "8";
    public const string BarSize1Day = "1";
    public const string BarSize1Week = "1";
    public const string BarSize1Month = "1";

    /// <summary>
    /// LoadContractsAsync configuration to update the contract cache.
    /// </summary>
    protected static List<string> SpecialTickerPatterns = new List<string> { "@" }; //special ticker patterns to search contracts under in LoadContractsAsync on top of the alphabetical letters. 
    protected const int ContractMatchRequestSleepTime = 1000; //sleep time between contracts matching requests in milliseconds - required by InteractiveBrokers to not flood the API (there's a hard limit of 50 requests per second https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#requests-limitations)
    protected const int ContractDetailsRequestSleepTime = 100; //sleep time between requests in milliseconds - set limit to be under 50 requests per second https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#requests-limitations

    //enums


    //types


    //attributes
    static protected IBApiAdapter? m_instance = null;
    protected Thread? m_responseReaderThread = null;
    public EClientSocket m_clientSocket;
    public EReaderSignal m_readerSignal;
    protected int m_nextValidReqId;
    protected int m_nextValidOrderId;
    protected ILogger m_logger;
    protected string m_ip;
    protected int m_port;
    protected List<string> m_accountIds;
    protected List<TradeSharp.Data.Account> m_accounts;
    protected IHost m_serviceHost;
    protected IExchangeService m_exchangeService;
    protected IInstrumentGroupService m_instrumentGroupService;
    protected IInstrumentService m_instrumentService;
    protected string m_databaseFile;
    protected IPluginConfiguration m_configuration;
    private SqliteConnection m_connection;
    private string m_connectionString;

    //constructors
    static public IBApiAdapter GetInstance(ILogger logger, IHost serviceHost, IPluginConfiguration configuration)
    {
      if (m_instance == null) m_instance = new IBApiAdapter(logger, serviceHost, configuration);
      return m_instance;
    }

    protected IBApiAdapter(ILogger logger, IHost serviceHost, IPluginConfiguration configuration)
    {
      //setup basic attributes
      m_logger = logger;
      m_readerSignal = new EReaderMonitorSignal();
      m_clientSocket = new EClientSocket(this, m_readerSignal);
      m_ip = "";
      m_port = -1;
      m_nextValidReqId = 0;
      m_nextValidOrderId = -1;
      m_accountIds = new List<string>();
      m_accounts = new List<TradeSharp.Data.Account>();
      m_serviceHost = serviceHost;
      m_configuration = configuration;
      m_exchangeService = m_serviceHost.Services.GetRequiredService<IExchangeService>();
      m_instrumentGroupService = m_serviceHost.Services.GetRequiredService<IInstrumentGroupService>();
      m_instrumentService = m_serviceHost.Services.GetRequiredService<IInstrumentService>();
      string tradeSharpHome = Environment.GetEnvironmentVariable(TradeSharp.Common.Constants.TradeSharpHome) ?? throw new ArgumentException($"Environment variable \"{TradeSharp.Common.Constants.TradeSharpHome}\" not defined.");
      m_databaseFile = Path.Combine(tradeSharpHome, TradeSharp.Common.Constants.ConfigurationDir, configuration.Configuration[TradeSharp.InteractiveBrokers.Constants.CacheKey]!.ToString());

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

    ~IBApiAdapter()
    {
      m_clientSocket.eDisconnect(); //this will terminate the response reader thread
      m_connection.Close();
    }

    //finalizers


    //interface implementations


    //properties
    public bool IsConnected { get => m_clientSocket.IsConnected(); }
    public int NextRequestId { get => m_nextValidReqId++; }
    public int NextOrderId { get => m_nextValidOrderId++; }
    public string IP { get => m_ip; }
    public int Port { get => m_port; }
    public IList<string> AccountIds { get => m_accountIds; }
    public IList<TradeSharp.Data.Account> Accounts { get => m_accounts; }

    //methods
    public void Connect(string ip, int port)
    {
      if (!m_clientSocket.IsConnected())
      {
        m_ip = ip;
        m_port = port;
        m_clientSocket.eConnect(m_ip, m_port, 0);
      }
      else
        if (m_ip != ip || m_port != port) m_logger.LogWarning($"Attempting to connect to a different IP and port than the current connection. (ConnectedIP - {m_ip}, RequestedIP - {ip}, ConnectedPort - {m_port}, RequestedPort - {port})");
    }

    /// <summary>
    /// Runs the response reader thread if not started yet.
    /// </summary>
    public void RunAsync()
    {
      if (m_responseReaderThread == null)
      {
        m_responseReaderThread = new Thread(Run);
        m_responseReaderThread.Start();
      }
    }

    protected void Run()
    {
      //main message processing loop, is terminated as soon as socket is disconnected 
      while (IsConnected)
      {
        m_readerSignal.waitForSignal();
        m_readerSignal.issueSignal();
      }
    }

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

    protected void CreateSchema()
    {
      lock (this)
      {
        CreateContractsTable();
        CreateStockContractDetails();
      }
    }

    protected void CreateContractsTable()
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
        LastTradeDateOrContractMonth TEXT
      ");
    }

    protected void CreateStockContractDetails()
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

    // Async method to download the list of contracts supported from the TWS API.
    // NOTE: There does not seem to be an easy way to get the list of all contracts supported by the TWS API so
    //       we need to load the contracts based on ticker matches.
    // https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#stock-symbol-search
    protected void LoadContractsAsync()
    {
      var workerThread = new Thread(() =>
      {
        //request basic contract information for all contracts
        foreach (string pattern in SpecialTickerPatterns)
        {
          m_clientSocket.reqMatchingSymbols(NextRequestId, pattern);
          Thread.Sleep(ContractMatchRequestSleepTime);
        }

        for (char pattern = 'A'; pattern <= 'Z'; pattern++)
        {
          m_clientSocket.reqMatchingSymbols(NextRequestId, pattern.ToString());
          Thread.Sleep(ContractMatchRequestSleepTime);
        }

        //request contract details for all stock type contracts (we only support stocks at this time)
        //https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#contract-details
        using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} WHERE SecType = '{ContractTypeStock}'"))
          while (reader.Read())
          {
            m_clientSocket.reqContractDetails(NextRequestId, new IBApi.Contract { ConId = reader.GetInt32(0), Symbol = reader.GetString(0), SecType = reader.GetString(0), Exchange = reader.GetString(0), Currency = reader.GetString(0) });
            Thread.Sleep(ContractDetailsRequestSleepTime);
          }
      });
    }

    public Contract? ContractForInstrument(string ticker, string exchange)
    {
      Contract? contract = null;
      using (var reader = ExecuteReader($"SELECT * FROM {TableContracts} NATURAL JOIN {TableStockContracts} ON ConId = ConId WHERE {TableContracts}.Symbol = '{ticker}' AND {TableContracts}.PrimaryExchange = '{exchange}'"))
        if (reader.Read())
        {
          string secType = reader.GetString(4);
          if (secType == ContractTypeStock)
          {
            contract = new ContractStock
            {
              ConId = reader.GetInt32(0),
              Symbol = reader.GetString(1),
              SecId = reader.GetString(2),
              SecIdType = reader.GetString(3),
              SecType = reader.GetString(4),
              Exchange = reader.GetString(5),
              PrimaryExchange = reader.GetString(6),
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

    public Contract? ContractForInstrument(Instrument instrument)
    {
      Exchange? primaryExchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
      if (primaryExchange == null) return null;
      Contract? contract = ContractForInstrument(instrument.Ticker, primaryExchange.Name);
      if (contract == null)
        foreach (string ticker in instrument.AlternateTickers)
        {
          contract = ContractForInstrument(ticker, primaryExchange.Name);
          if (contract != null) break;
        }
      return contract;
    }

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

    /// <summary>
    /// Callback functions used by the TWS API client.
    /// </summary>
    public void accountDownloadEnd(string account)
    {
      m_logger.LogTrace($"accountDownloadEnd - {account}");
    }

    protected void setAccountValue(string responseName, int reqId, string accountName, string key, string value, string currency)
    {
      TradeSharp.InteractiveBrokers.Account? account = (TradeSharp.InteractiveBrokers.Account?)m_accounts.FirstOrDefault(a => a.Name == accountName);

      if (account == null)
      {
        m_logger.LogWarning($"{responseName} account not found - {accountName}");
        return;
      }

      account.setLastSyncDateTime(DateTime.Now);
      account.setBaseCurrency(currency);    //NOTE: We assume account would have same currency for all values.

      if (key == AccountSummaryTags.NetLiquidation)
      {
        if (double.TryParse(value, out double result))
          account.setNetLiquidation(result);
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.SettledCash)
      {
        if (double.TryParse(value, out double result))
          account.setSettledCash(double.Parse(value));
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.BuyingPower)
      {
        if (double.TryParse(value, out double result))
          account.setBuyingPower(double.Parse(value));
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.MaintMarginReq)
      {
        if (double.TryParse(value, out double result))
          account.setMaintenanceMargin(double.Parse(value));
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.GrossPositionValue)
      {
        if (double.TryParse(value, out double result))
          account.setPositionsValue(double.Parse(value));
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.AvailableFunds)
      {
        if (double.TryParse(value, out double result))
          account.setAvailableFunds(double.Parse(value));
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.ExcessLiquidity)
      {
        if (double.TryParse(value, out double result))
          account.setExcessLiquidity(double.Parse(value));
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
    }

    public void accountSummary(int reqId, string accountName, string key, string value, string currency)
    {
      setAccountValue("accountSummary", reqId, accountName, key, value, currency);
    }

    public void accountSummaryEnd(int reqId)
    {
      m_logger.LogTrace($"accountSummaryEnd - {reqId}");
      //after receiving account summary, request account position updates to ensure account positions are in sync
      m_clientSocket.reqPositions();
    }

    public void accountUpdateMulti(int reqId, string accountName, string modelCode, string key, string value, string currency)
    {
      setAccountValue("accountUpdateMulti", reqId, accountName, key, value, currency);
    }

    public void accountUpdateMultiEnd(int reqId)
    {
      m_logger.LogTrace($"accountUpdateMultiEnd - {reqId}");
    }

    public void bondContractDetails(int reqId, ContractDetails contract)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void commissionReport(CommissionReport commissionReport)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void completedOrder(IBApi.Contract contract, IBApi.Order order, OrderState orderState)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void completedOrdersEnd()
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void connectAck()
    {
      m_logger.LogInformation("TWS API Client Connected");
    }

    public void connectionClosed()
    {
      m_logger.LogInformation("TWS API disconnected");
    }

    public void contractDetails(int reqId, ContractDetails contractDetails)
    {
      m_logger.LogInformation($"contractDetails - {reqId} SecType: {contractDetails.UnderSecType} Symbol: {contractDetails.UnderSymbol}");
      UpdateContractDetails(contractDetails);
    }

    public void contractDetailsEnd(int reqId)
    {
      m_logger.LogInformation($"contractDetailsEnd - {reqId}");
    }

    public void currentTime(long time)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void displayGroupList(int reqId, string groups)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void displayGroupUpdated(int reqId, string contractInfo)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void error(Exception e)
    {
      //https://interactivebrokers.github.io/tws-api/message_codes.html
      m_logger.LogError(e, "TWS API Client Error");
    }

    public void error(string str)
    {
      m_logger.LogError(str);
    }

    public void error(int id, int errorCode, string errorMsg)
    {
      m_logger.LogError("TWS API Error: {0} {1} {2}", id, errorCode, errorMsg);
    }

    public void execDetails(int reqId, IBApi.Contract contract, Execution execution)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void execDetailsEnd(int reqId)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void familyCodes(FamilyCode[] familyCodes)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void fundamentalData(int reqId, string data)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void headTimestamp(int reqId, string headTimestamp)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void histogramData(int reqId, HistogramEntry[] data)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    // To get first timestamp at which market data is availabble - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#earliest-data
    // Requesting historical data - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#historical-bars
    public void historicalData(int reqId, Bar bar)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalDataEnd(int reqId, string start, string end)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalDataUpdate(int reqId, Bar bar)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalNewsEnd(int requestId, bool hasMore)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void managedAccounts(string accountsList)
    {
      m_accountIds = accountsList.Split(',').ToList();

      //reconstruct the accounts list
      m_accounts.Clear();
      foreach (string account in m_accountIds) m_accounts.Add(new TradeSharp.InteractiveBrokers.Account(account));

      //request account summary to initialize accounts data
      m_clientSocket.reqAccountSummary(NextRequestId, "All", AccountSummaryTags.GetAllTags());
    }

    public void marketDataType(int reqId, int marketDataType)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void newsArticle(int reqId, int articleType, string articleText)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void newsProviders(NewsProvider[] newsProviders)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void nextValidId(int orderId)
    {
      m_nextValidOrderId = orderId;
    }

    public void openOrder(int orderId, IBApi.Contract contract, IBApi.Order order, OrderState orderState)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void openOrderEnd()
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void orderBound(long orderId, int apiClientId, int apiOrderId)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void position(string account, IBApi.Contract contract, double pos, double avgCost)
    {



      //TOOD - update account positions
      // - We need to map the contracts to Instrument to update the positions


      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");


    }

    public void positionEnd()
    {
      m_logger.LogTrace("positionsEnd called");
    }

    public void positionMulti(int requestId, string account, string modelCode, IBApi.Contract contract, double pos, double avgCost)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void positionMultiEnd(int requestId)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void receiveFA(int faDataType, string faXmlData)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void rerouteMktDataReq(int reqId, int conId, string exchange)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void rerouteMktDepthReq(int reqId, int conId, string exchange)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void scannerDataEnd(int reqId)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void scannerParameters(string xml)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void securityDefinitionOptionParameterEnd(int reqId)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    //Works with reqMatchingSymbols and called from LoadContractsAsync.
    //https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#request-stock-symbol
    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
    {
      foreach (ContractDescription contractDescription in contractDescriptions)
        UpdateContract(contractDescription.Contract);
    }

    public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttriblast, string exchange, string specialConditions)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickByTickMidPoint(int reqId, long time, double midPoint)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickGeneric(int tickerId, int field, double value)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickSize(int tickerId, int field, int size)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickSnapshotEnd(int tickerId)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void tickString(int tickerId, int field, string value)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void updateAccountTime(string timestamp)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void updateAccountValue(string key, string value, string currency, string accountName)
    {
      setAccountValue("updateAccountValue", -1, accountName, key, value, currency);
    }

    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void updatePortfolio(IBApi.Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void verifyCompleted(bool isSuccessful, string errorText)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }

    public void verifyMessageAPI(string apiData)
    {
      m_logger.LogWarning($"NotImplemented - {MethodBase.GetCurrentMethod()!.Name}");
    }
  }
}
