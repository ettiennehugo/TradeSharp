using IBApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeSharp.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.InteractiveBrokers.Messages;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter singleton for TWS API client responses to work with the TradeSharp broker and data plugins.
  /// It acts as a certral storage for common information required to interact with the TWS API.
  /// https://interactivebrokers.github.io/tws-api/client_wrapper.html#The
  /// https://interactivebrokers.github.io/tws-api/interfaceIBApi_1_1EWrapper.html
  /// Pacing violations - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#historical-pacing-limitations
  /// </summary>
  public class Client : EWrapper, IDisposable
  {
    //constants


    //enums


    //types


    //attributes
    static protected Client? m_instance = null;
    protected CancellationTokenSource? m_cancelResponseReaderThread;
    protected Thread? m_responseReaderThread;
    protected EReaderSignal? m_readerSignal;
    protected EReader m_responseReader;
    protected int m_nextRequestId;
    protected int m_nextOrderId;
    protected ILogger m_logger;
    protected string m_ip;
    protected int m_port;
    protected ServiceHost m_serviceHost;
    protected IDialogService m_dialogService;
    protected IPluginConfiguration m_configuration;
    protected bool m_connected;

    //constructors
    static public Client GetInstance(ServiceHost serviceHost)
    {
      if (m_instance == null) m_instance = new Client(serviceHost);
      return m_instance;
    }

    protected Client(ServiceHost serviceHost)
    {
      //setup basic attributes
      m_serviceHost = serviceHost;
      m_logger = serviceHost.Host.Services.GetRequiredService<ILogger<Cache>>();
      m_readerSignal = new EReaderMonitorSignal();
      ClientSocket = new EClientSocket(this, m_readerSignal);
      m_connected = false;
      m_ip = "";
      m_port = -1;
      m_nextRequestId = 1;
      m_nextOrderId = -1;
      m_dialogService = serviceHost.Host.Services.GetRequiredService<IDialogService>();
    }

    public void Dispose()
    {
      if (ClientSocket.IsConnected()) ClientSocket.eDisconnect();

      //shutdown the response reader thread - we need to set the cancellation token then issue a dummy signal before
      //deallocating the response components
      if (m_cancelResponseReaderThread != null) m_cancelResponseReaderThread.Cancel();      
      
      m_readerSignal!.issueSignal();  //this will signal a response in the processing loop of ResponseReaderMain to unblock the thread
      m_readerSignal = null;
    }

    //finalizers


    //interface implementations


    //properties
    public EClientSocket ClientSocket { get; private set; }
    public bool IsConnected { get => m_connected && ClientSocket.IsConnected(); } //this client must be connected to TWS and TWS needs to be connected to the server
    public int NextRequestId { get => m_nextRequestId++; }
    public int NextOrderId { get => m_nextOrderId++; }
    public string IP { get => m_ip; }
    public int Port { get => m_port; }

    //methods
    public void Connect(string ip, int port)
    {
      if (!ClientSocket.IsConnected())
      {
        m_ip = ip;
        m_port = port;

        try
        {
          m_nextOrderId = -1; //make sure we wait for the next valid order ID from IB before continueing
          ClientSocket.eConnect(m_ip, m_port, 0); //this call should be synchronous to ensure handshake is done before creating the reader thread below, if this is done too quickly the TWS API immediately disconnects
          Thread.Sleep(1000); //wait for the connection to be established before continuing otherwise the TWS API will disconnect/enter an error state
          var time = DateTime.Now;
          var waitTime = new TimeSpan(5000000000);  //wait 5-seconds
          while (NextOrderId <= 0) { if (DateTime.Now - time > waitTime) break; } //wait for initial handshake to complete, we need to limit the wait otherwise this thread will hang the application

          if (!ClientSocket.IsConnected())
          {
            m_logger.LogError($"Connection to IP {m_ip} and port {m_port} failed - check that IB Gateway is running and check port settings in TradeSharp config file.");
            m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Error, "", $"Connection to IP {m_ip} and port {m_port} failed - check that IB Gateway is running and check port settings in TradeSharp config file.");
          }
          else
            ClientSocket.setServerLogLevel(5);    //log level for detailed message (not sure whether 5 is the Details value but it looks like it in TWS)
        }
        catch (Exception e)
        {
          m_logger.LogError($"Connection to IP {m_ip} and port {m_port} failed with exception - {e.Message}");
        }

        //creates the response reader that would wait for responses and queue them to the EWrapper
        //NOTE: The response reader MUST be created after the initial handshake above otherwise none of the callbacks will work.
        if (m_responseReader == null)
        {
          m_responseReader = new EReader(ClientSocket, m_readerSignal);
          m_responseReader.Start();
        }

        //create the response reader thread that would loop the above response reader
        if (m_responseReaderThread == null)
        {
          m_cancelResponseReaderThread = new CancellationTokenSource();
          m_responseReaderThread = new Thread(ResponseReaderMain);
          m_responseReaderThread.Name = "IBApi Response Reader";
          m_responseReaderThread.Start();
        }

        //raise connected event
        m_connected = ClientSocket.IsConnected();
        if (m_connected)
        {
          var tmp = ConnectionStatus;
          if (tmp != null)
            ThreadPool.QueueUserWorkItem(t => tmp(new ConnectionStatusMessage(m_connected)), null);
        }
      }
      else if (m_ip != ip || m_port != port)
      {
        m_logger.LogWarning($"Attempting to connect to a different IP and port than the current connection. (ConnectedIP - {m_ip}, RequestedIP - {ip}, ConnectedPort - {m_port}, RequestedPort - {port})");
        m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Warning, "", $"Attempting to connect to a different IP and port than the current connection. (ConnectedIP - {m_ip}, RequestedIP - {ip}, ConnectedPort - {m_port}, RequestedPort - {port})");
      }
    }

    public void Disconnect()
    {
      ClientSocket.eDisconnect();
      m_connected = false;
      var tmp = ConnectionStatus;
      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ConnectionStatusMessage(false)), null);
    }

    /// <summary>
    /// Main processing loop to read resonses from the TWS API.
    /// </summary>
    protected void ResponseReaderMain()
    {
      while (!m_cancelResponseReaderThread!.IsCancellationRequested)
      {
        m_readerSignal!.waitForSignal();
        m_responseReader.processMsgs();
      }

      m_cancelResponseReaderThread.Dispose();
      m_cancelResponseReaderThread = null;
      m_responseReaderThread = null;
    }

    public Task<Contract> ResolveContractAsync(int conId, string exchange)
    {
      var reqId = new Random(DateTime.Now.Millisecond).Next();
      var resolveResult = new TaskCompletionSource<Contract>();
      var resolveContract_Error = new Action<int, int, string, string, Exception>((id, code, msg, advancedOrderRejectJson, ex) =>
      {
        if (reqId != id)
          return;

        resolveResult.SetResult(null);
      });
      var resolveContract = new Action<ContractDetailsMessage>(msg =>
      {
        if (msg.RequestId == reqId)
          resolveResult.SetResult(msg.ContractDetails.Contract);
      });
      var contractDetailsEnd = new Action<ContractDetailsEndMessage>(msg =>
      {
        if (reqId == msg.RequestId && !resolveResult.Task.IsCompleted)
          resolveResult.SetResult(null);
      });

      var tmpError = Error;
      var tmpContractDetails = ContractDetails;
      var tmpContractDetailsEnd = ContractDetailsEnd;

      Error = resolveContract_Error;
      ContractDetails = resolveContract;
      ContractDetailsEnd = contractDetailsEnd;

      resolveResult.Task.ContinueWith(t =>
      {
        Error = tmpError;
        ContractDetails = tmpContractDetails;
        ContractDetailsEnd = tmpContractDetailsEnd;
      });

      ClientSocket.reqContractDetails(reqId, new Contract { ConId = conId, Exchange = exchange });

      return resolveResult.Task;
    }

    public Task<Contract[]> ResolveContractAsync(string secType, string symbol, string currency, string exchange)
    {
      var reqId = new Random(DateTime.Now.Millisecond).Next();
      var res = new TaskCompletionSource<IBApi.Contract[]>();
      var contractList = new List<IBApi.Contract>();
      var resolveContract_Error = new Action<int, int, string, string, Exception>((id, code, msg, advancedOrderRejectJson, ex) =>
      {
        if (reqId != id)
          return;

        res.SetResult(new IBApi.Contract[0]);
      });
      var contractDetails = new Action<ContractDetailsMessage>(msg =>
      {
        if (reqId != msg.RequestId)
          return;

        contractList.Add(msg.ContractDetails.Contract);
      });
      var contractDetailsEnd = new Action<ContractDetailsEndMessage>(msg =>
      {
        if (reqId == msg.RequestId)
          res.SetResult(contractList.ToArray());
      });

      var tmpError = Error;
      var tmpContractDetails = ContractDetails;
      var tmpContractDetailsEnd = ContractDetailsEnd;

      Error = resolveContract_Error;
      ContractDetails = contractDetails;
      ContractDetailsEnd = contractDetailsEnd;

      res.Task.ContinueWith(t =>
      {
        Error = tmpError;
        ContractDetails = tmpContractDetails;
        ContractDetailsEnd = tmpContractDetailsEnd;
      });

      ClientSocket.reqContractDetails(reqId, new Contract { SecType = secType, Symbol = symbol, Currency = currency, Exchange = exchange });

      return res.Task;
    }

    public event Action<int, int, string, string, Exception> Error;

    void EWrapper.error(Exception e)
    {
      m_logger.LogError($"Client error - {e.Message}");
      var tmp = Error;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(0, 0, null, null, e));
    }

    void EWrapper.error(string errorMessage)
    {
      m_logger.LogError($"Client error - {errorMessage}");
      var tmp = Error;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(0, 0, errorMessage, null, null), null);
    }

    public void error(int id, int errorCode, string errorMessage)
    {
      m_logger.LogError($"Client error - {id}, {errorCode}, {errorMessage}");
      var tmp = Error;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(id, errorCode, errorMessage, null, null), null);
    }

    void EWrapper.connectAck()
    {
      if (ClientSocket.AsyncEConnect)
        ClientSocket.startApi();
    }

    public event Action<ConnectionStatusMessage> ConnectionStatus;

    void EWrapper.connectionClosed()
    {
      m_connected = false;
      var tmp = ConnectionStatus;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ConnectionStatusMessage(m_connected)), null);
    }

    void EWrapper.nextValidId(int orderId)
    {
      m_connected = true;
      m_nextOrderId = orderId;
      var tmp = ConnectionStatus;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ConnectionStatusMessage(true)), null);

      m_nextOrderId = orderId;
    }

    public event Action<long> CurrentTime;

    void EWrapper.currentTime(long time)
    {
      var tmp = CurrentTime;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(time), null);
    }

    public event Action<TickPriceMessage> TickPrice;

    void EWrapper.tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
      var tmp = TickPrice;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickPriceMessage(tickerId, field, price, attribs)), null);
    }

    public event Action<TickSizeMessage> TickSize;

    void EWrapper.tickSize(int tickerId, int field, int size)
    {
      var tmp = TickSize;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickSizeMessage(tickerId, field, size)), null);
    }

    public event Action<int, int, string> TickString;

    void EWrapper.tickString(int tickerId, int tickType, string value)
    {
      var tmp = TickString;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(tickerId, tickType, value), null);
    }

    public event Action<TickGenericMessage> TickGeneric;

    void EWrapper.tickGeneric(int tickerId, int field, double value)
    {
      var tmp = TickGeneric;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickGenericMessage(tickerId, field, value)), null);
    }

    public event Action<int, int, double, string, double, int, string, double, double> TickEFP;

    void EWrapper.tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
    {
      var tmp = TickEFP;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(tickerId, tickType, basisPoints, formattedBasisPoints, impliedFuture, holdDays, futureLastTradeDate, dividendImpact, dividendsToLastTradeDate), null);
    }

    public event Action<int> TickSnapshotEnd;

    void EWrapper.tickSnapshotEnd(int tickerId)
    {
      var tmp = TickSnapshotEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(tickerId), null);
    }

    public event Action<int, DeltaNeutralContract> DeltaNeutralValidation;

    void EWrapper.deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
    {
      var tmp = DeltaNeutralValidation;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, deltaNeutralContract), null);
    }

    public event Action<ManagedAccountsMessage> ManagedAccounts;

    void EWrapper.managedAccounts(string accountsList)
    {
      var tmp = ManagedAccounts;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ManagedAccountsMessage(accountsList)), null);
    }

    public event Action<TickOptionMessage> TickOptionCommunication;

    //void EWrapper.tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
    void EWrapper.tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
    {
      var tmp = TickOptionCommunication;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickOptionMessage(tickerId, field, impliedVolatility, delta, optPrice, pvDividend, gamma, vega, theta, undPrice)), null);
    }

    public event Action<AccountSummaryMessage> AccountSummary;

    void EWrapper.accountSummary(int reqId, string account, string tag, string value, string currency)
    {
      var tmp = AccountSummary;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new AccountSummaryMessage(reqId, account, tag, value, currency)), null);
    }

    public event Action<AccountSummaryEndMessage> AccountSummaryEnd;

    void EWrapper.accountSummaryEnd(int reqId)
    {
      var tmp = AccountSummaryEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new AccountSummaryEndMessage(reqId)), null);
    }

    public event Action<AccountValueMessage> UpdateAccountValue;

    void EWrapper.updateAccountValue(string key, string value, string currency, string accountName)
    {
      var tmp = UpdateAccountValue;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new AccountValueMessage(key, value, currency, accountName)), null);
    }

    public event Action<UpdatePortfolioMessage> UpdatePortfolio;

    void EWrapper.updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
    {
      var tmp = UpdatePortfolio;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new UpdatePortfolioMessage(contract, position, marketPrice, marketValue, averageCost, unrealizedPNL, realizedPNL, accountName)), null);
    }

    public event Action<UpdateAccountTimeMessage> UpdateAccountTime;

    void EWrapper.updateAccountTime(string timestamp)
    {
      var tmp = UpdateAccountTime;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new UpdateAccountTimeMessage(timestamp)), null);
    }

    public event Action<AccountDownloadEndMessage> AccountDownloadEnd;

    void EWrapper.accountDownloadEnd(string account)
    {
      var tmp = AccountDownloadEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new AccountDownloadEndMessage(account)), null);
    }

    public event Action<OrderStatusMessage> OrderStatus;

    void EWrapper.orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
    {
      var tmp = OrderStatus;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new OrderStatusMessage(orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice)), null);
    }

    public event Action<OpenOrderMessage> OpenOrder;

    void EWrapper.openOrder(int orderId, Contract contract, IBApi.Order order, OrderState orderState)
    {
      var tmp = OpenOrder;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new OpenOrderMessage(orderId, contract, order, orderState)), null);
    }

    public event Action OpenOrderEnd;

    void EWrapper.openOrderEnd()
    {
      var tmp = OpenOrderEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(), null);
    }

    public event Action<ContractDetailsMessage> ContractDetails;

    void EWrapper.contractDetails(int reqId, ContractDetails contractDetails)
    {
      var tmp = ContractDetails;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ContractDetailsMessage(reqId, contractDetails)), null);
    }

    public event Action<ContractDetailsEndMessage> ContractDetailsEnd;

    void EWrapper.contractDetailsEnd(int reqId)
    {
      var tmp = ContractDetailsEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ContractDetailsEndMessage(reqId)), null);
    }

    public event Action<ExecutionMessage> ExecDetails;

    void EWrapper.execDetails(int reqId, Contract contract, Execution execution)
    {
      var tmp = ExecDetails;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ExecutionMessage(reqId, contract, execution)), null);
    }

    public event Action<int> ExecDetailsEnd;

    void EWrapper.execDetailsEnd(int reqId)
    {
      var tmp = ExecDetailsEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId), null);
    }

    public event Action<CommissionReport> CommissionReport;

    void EWrapper.commissionReport(CommissionReport commissionReport)
    {
      var tmp = CommissionReport;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(commissionReport), null);
    }

    public event Action<FundamentalsMessage> FundamentalData;

    void EWrapper.fundamentalData(int reqId, string data)
    {
      var tmp = FundamentalData;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new FundamentalsMessage(data)), null);
    }

    public event Action<HistoricalDataMessage> HistoricalData;

    void EWrapper.historicalData(int reqId, Bar bar)
    {
      var tmp = HistoricalData;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HistoricalDataMessage(reqId, bar)), null);
    }

    public event Action<HistoricalDataEndMessage> HistoricalDataEnd;

    void EWrapper.historicalDataEnd(int reqId, string startDate, string endDate)
    {
      var tmp = HistoricalDataEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HistoricalDataEndMessage(reqId, startDate, endDate)), null);
    }

    public event Action<MarketDataTypeMessage> MarketDataType;

    void EWrapper.marketDataType(int reqId, int marketDataType)
    {
      var tmp = MarketDataType;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new MarketDataTypeMessage(reqId, marketDataType)), null);
    }

    public event Action<DeepBookMessage> UpdateMktDepth;

    void EWrapper.updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
    {
      var tmp = UpdateMktDepth;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new DeepBookMessage(tickerId, position, operation, side, price, size, "", false)), null);
    }

    public event Action<DeepBookMessage> UpdateMktDepthL2;

    void EWrapper.updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth)
    {
      var tmp = UpdateMktDepthL2;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new DeepBookMessage(tickerId, position, operation, side, price, size, marketMaker, isSmartDepth)), null);
    }

    public event Action<int, int, string, string> UpdateNewsBulletin;

    void EWrapper.updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
    {
      var tmp = UpdateNewsBulletin;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(msgId, msgType, message, origExchange), null);
    }

    public event Action<PositionMessage> Position;

    void EWrapper.position(string account, Contract contract, double pos, double avgCost)
    {
      var tmp = Position;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new PositionMessage(account, contract, pos, avgCost)), null);
    }

    public event Action PositionEnd;

    void EWrapper.positionEnd()
    {
      var tmp = PositionEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(), null);
    }

    public event Action<RealTimeBarMessage> RealTimeBar;

    void EWrapper.realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count)
    {
      var tmp = RealTimeBar;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new RealTimeBarMessage(reqId, date, open, high, low, close, volume, WAP, count)), null);
    }

    public event Action<string> ScannerParameters;

    void EWrapper.scannerParameters(string xml)
    {
      var tmp = ScannerParameters;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(xml), null);
    }

    public event Action<ScannerMessage> ScannerData;

    void EWrapper.scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
    {
      var tmp = ScannerData;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new ScannerMessage(reqId, rank, contractDetails, distance, benchmark, projection, legsStr)), null);
    }

    public event Action<int> ScannerDataEnd;

    void EWrapper.scannerDataEnd(int reqId)
    {
      var tmp = ScannerDataEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId), null);
    }

    public event Action<AdvisorDataMessage> ReceiveFA;

    void EWrapper.receiveFA(int faDataType, string faXmlData)
    {
      var tmp = ReceiveFA;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new AdvisorDataMessage(faDataType, faXmlData)), null);
    }

    public event Action<BondContractDetailsMessage> BondContractDetails;

    void EWrapper.bondContractDetails(int requestId, ContractDetails contractDetails)
    {
      var tmp = BondContractDetails;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new BondContractDetailsMessage(requestId, contractDetails)), null);
    }

    public event Action<string> VerifyMessageAPI;

    void EWrapper.verifyMessageAPI(string apiData)
    {
      var tmp = VerifyMessageAPI;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(apiData), null);
    }
    public event Action<bool, string> VerifyCompleted;

    void EWrapper.verifyCompleted(bool isSuccessful, string errorText)
    {
      var tmp = VerifyCompleted;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(isSuccessful, errorText), null);
    }

    public event Action<string, string> VerifyAndAuthMessageAPI;

    void EWrapper.verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
    {
      var tmp = VerifyAndAuthMessageAPI;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(apiData, xyzChallenge), null);
    }

    public event Action<bool, string> VerifyAndAuthCompleted;

    void EWrapper.verifyAndAuthCompleted(bool isSuccessful, string errorText)
    {
      var tmp = VerifyAndAuthCompleted;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(isSuccessful, errorText), null);
    }

    public event Action<int, string> DisplayGroupList;

    void EWrapper.displayGroupList(int reqId, string groups)
    {
      var tmp = DisplayGroupList;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, groups), null);
    }

    public event Action<int, string> DisplayGroupUpdated;

    void EWrapper.displayGroupUpdated(int reqId, string contractInfo)
    {
      var tmp = DisplayGroupUpdated;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, contractInfo), null);
    }

    public event Action<PositionMultiMessage> PositionMulti;

    //void EWrapper.positionMulti(int reqId, string account, string modelCode, Contract contract, decimal pos, double avgCost)
    void EWrapper.positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
    {
      var tmp = PositionMulti;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new PositionMultiMessage(requestId, account, modelCode, contract, pos, avgCost)), null);
    }

    public event Action<int> PositionMultiEnd;

    void EWrapper.positionMultiEnd(int reqId)
    {
      var tmp = PositionMultiEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId), null);
    }

    public event Action<AccountUpdateMultiMessage> AccountUpdateMulti;

    void EWrapper.accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency)
    {
      var tmp = AccountUpdateMulti;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new AccountUpdateMultiMessage(reqId, account, modelCode, key, value, currency)), null);
    }

    public event Action<int> AccountUpdateMultiEnd;

    void EWrapper.accountUpdateMultiEnd(int reqId)
    {
      var tmp = AccountUpdateMultiEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId), null);
    }

    public event Action<SecurityDefinitionOptionParameterMessage> SecurityDefinitionOptionParameter;

    void EWrapper.securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
    {
      var tmp = SecurityDefinitionOptionParameter;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new SecurityDefinitionOptionParameterMessage(reqId, exchange, underlyingConId, tradingClass, multiplier, expirations, strikes)), null);
    }

    public event Action<int> SecurityDefinitionOptionParameterEnd;

    void EWrapper.securityDefinitionOptionParameterEnd(int reqId)
    {
      var tmp = SecurityDefinitionOptionParameterEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId), null);
    }

    public event Action<SoftDollarTiersMessage> SoftDollarTiers;

    void EWrapper.softDollarTiers(int reqId, SoftDollarTier[] tiers)
    {
      var tmp = SoftDollarTiers;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new SoftDollarTiersMessage(reqId, tiers)), null);
    }

    public event Action<FamilyCode[]> FamilyCodes;

    void EWrapper.familyCodes(FamilyCode[] familyCodes)
    {
      var tmp = FamilyCodes;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(familyCodes), null);
    }

    public event Action<SymbolSamplesMessage> SymbolSamples;

    void EWrapper.symbolSamples(int reqId, ContractDescription[] contractDescriptions)
    {
      var tmp = SymbolSamples;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new SymbolSamplesMessage(reqId, contractDescriptions)), null);
    }


    public event Action<DepthMktDataDescription[]> MktDepthExchanges;

    void EWrapper.mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
    {
      var tmp = MktDepthExchanges;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(depthMktDataDescriptions), null);
    }

    public event Action<TickNewsMessage> TickNews;

    void EWrapper.tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
    {
      var tmp = TickNews;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickNewsMessage(tickerId, timeStamp, providerCode, articleId, headline, extraData)), null);
    }

    public event Action<int, Dictionary<int, KeyValuePair<string, char>>> SmartComponents;

    void EWrapper.smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
    {
      var tmp = SmartComponents;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, theMap), null);
    }

    public event Action<TickReqParamsMessage> TickReqParams;

    void EWrapper.tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
    {
      var tmp = TickReqParams;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickReqParamsMessage(tickerId, minTick, bboExchange, snapshotPermissions)), null);
    }

    public event Action<NewsProvider[]> NewsProviders;

    void EWrapper.newsProviders(NewsProvider[] newsProviders)
    {
      var tmp = NewsProviders;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(newsProviders), null);
    }

    public event Action<NewsArticleMessage> NewsArticle;

    void EWrapper.newsArticle(int requestId, int articleType, string articleText)
    {
      var tmp = NewsArticle;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new NewsArticleMessage(requestId, articleType, articleText)), null);
    }

    public event Action<HistoricalNewsMessage> HistoricalNews;

    void EWrapper.historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
    {
      var tmp = HistoricalNews;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HistoricalNewsMessage(requestId, time, providerCode, articleId, headline)), null);
    }

    public event Action<HistoricalNewsEndMessage> HistoricalNewsEnd;

    void EWrapper.historicalNewsEnd(int requestId, bool hasMore)
    {
      var tmp = HistoricalNewsEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HistoricalNewsEndMessage(requestId, hasMore)), null);
    }

    public event Action<HeadTimestampMessage> HeadTimestamp;

    void EWrapper.headTimestamp(int reqId, string headTimestamp)
    {
      var tmp = HeadTimestamp;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HeadTimestampMessage(reqId, headTimestamp)), null);
    }

    public event Action<HistogramDataMessage> HistogramData;

    void EWrapper.histogramData(int reqId, HistogramEntry[] data)
    {
      var tmp = HistogramData;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HistogramDataMessage(reqId, data)), null);
    }

    public event Action<HistoricalDataMessage> HistoricalDataUpdate;

    void EWrapper.historicalDataUpdate(int reqId, Bar bar)
    {
      var tmp = HistoricalDataUpdate;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new HistoricalDataMessage(reqId, bar)), null);
    }

    public event Action<int, int, string> RerouteMktDataReq;

    void EWrapper.rerouteMktDataReq(int reqId, int conId, string exchange)
    {
      var tmp = RerouteMktDataReq;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, conId, exchange), null);
    }

    public event Action<int, int, string> RerouteMktDepthReq;

    void EWrapper.rerouteMktDepthReq(int reqId, int conId, string exchange)
    {
      var tmp = RerouteMktDepthReq;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, conId, exchange), null);
    }

    public event Action<MarketRuleMessage> MarketRule;

    void EWrapper.marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
    {
      var tmp = MarketRule;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new MarketRuleMessage(marketRuleId, priceIncrements)), null);
    }

    public event Action<PnLMessage> PnL;

    void EWrapper.pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
    {
      var tmp = PnL;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new PnLMessage(reqId, dailyPnL, unrealizedPnL, realizedPnL)), null);
    }

    public event Action<PnLSingleMessage> PnLSingle;

    void EWrapper.pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
    {
      var tmp = PnLSingle;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new PnLSingleMessage(reqId, pos, dailyPnL, unrealizedPnL, realizedPnL, value)), null);
    }

    public event Action<HistoricalTickMessage> HistoricalTick;

    void EWrapper.historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
    {
      var tmp = HistoricalTick;

      if (tmp != null)
        ticks.ToList().ForEach(tick => ThreadPool.QueueUserWorkItem(t => tmp(new HistoricalTickMessage(reqId, tick.Time, tick.Price, tick.Size)), null));
    }

    public event Action<HistoricalTickBidAskMessage> HistoricalTickBidAsk;

    void EWrapper.historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
    {
      var tmp = HistoricalTickBidAsk;

      if (tmp != null)
        ticks.ToList().ForEach(tick => ThreadPool.QueueUserWorkItem(t =>
            tmp(new HistoricalTickBidAskMessage(reqId, tick.Time, tick.TickAttribBidAsk, tick.PriceBid, tick.PriceAsk, tick.SizeBid, tick.SizeAsk)), null));
    }

    public event Action<HistoricalTickLastMessage> HistoricalTickLast;

    void EWrapper.historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
    {
      var tmp = HistoricalTickLast;

      if (tmp != null)
        ticks.ToList().ForEach(tick => ThreadPool.QueueUserWorkItem(t =>
            tmp(new HistoricalTickLastMessage(reqId, tick.Time, tick.TickAttribLast, tick.Price, tick.Size, tick.Exchange, tick.SpecialConditions)), null));
    }

    public event Action<TickByTickAllLastMessage> TickByTickAllLast;

    void EWrapper.tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttribLast, string exchange, string specialConditions)
    {
      var tmp = TickByTickAllLast;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickByTickAllLastMessage(reqId, tickType, time, price, size, tickAttribLast, exchange, specialConditions)), null);
    }

    public event Action<TickByTickBidAskMessage> TickByTickBidAsk;

    //void EWrapper.tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk)
    void EWrapper.tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
    {
      var tmp = TickByTickBidAsk;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickByTickBidAskMessage(reqId, time, bidPrice, askPrice, bidSize, askSize, tickAttribBidAsk)), null);
    }

    public event Action<TickByTickMidPointMessage> TickByTickMidPoint;

    void EWrapper.tickByTickMidPoint(int reqId, long time, double midPoint)
    {
      var tmp = TickByTickMidPoint;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new TickByTickMidPointMessage(reqId, time, midPoint)), null);
    }

    public event Action<OrderBoundMessage> OrderBound;

    void EWrapper.orderBound(long orderId, int apiClientId, int apiOrderId)
    {
      var tmp = OrderBound;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new OrderBoundMessage(orderId, apiClientId, apiOrderId)), null);
    }

    public event Action<CompletedOrderMessage> CompletedOrder;

    void EWrapper.completedOrder(Contract contract, IBApi.Order order, OrderState orderState)
    {
      var tmp = CompletedOrder;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(new CompletedOrderMessage(contract, order, orderState)), null);
    }

    public event Action CompletedOrdersEnd;

    void EWrapper.completedOrdersEnd()
    {
      var tmp = CompletedOrdersEnd;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(), null);
    }

    public event Action<int, string> WshMetaData;

    public void wshMetaData(int reqId, string dataJson)
    {
      var tmp = WshMetaData;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, dataJson), null);
    }

    public event Action<int, string> WshEventData;

    public void wshEventData(int reqId, string dataJson)
    {
      var tmp = WshEventData;

      if (tmp != null)
        ThreadPool.QueueUserWorkItem(t => tmp(reqId, dataJson), null);
    }
  }
}
