using IBApi;
using Microsoft.Extensions.Logging;

namespace Tradesharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter singleton for TWS API client responses to work with the TradeSharp broker and data plugins.
  /// It acts as a certral storage for common information required to interact with the TWS API.
  /// https://interactivebrokers.github.io/tws-api/client_wrapper.html#The
  /// https://interactivebrokers.github.io/tws-api/interfaceIBApi_1_1EWrapper.html
  /// </summary>
  public class IBApiAdapter : EWrapper
  {
    //constants


    //enums


    //types


    //attributes
    static protected IBApiAdapter? m_instance = null;
    protected Thread? m_responseReaderThread = null;
    public EClientSocket m_clientSocket;
    public EReaderSignal m_readerSignal;
    protected int m_nextValidId;
    protected ILogger m_logger;
    protected string m_ip;
    protected int m_port;
    protected List<string> m_accounts;

    //constructors
    static public IBApiAdapter GetInstance(ILogger logger)
    {
      if (m_instance == null) m_instance = new IBApiAdapter(logger);
      return m_instance;
    }

    protected IBApiAdapter(ILogger logger)
    {
      m_logger = logger;
      m_readerSignal = new EReaderMonitorSignal();
      m_clientSocket = new EClientSocket(this, m_readerSignal);
      m_ip = "";
      m_port = -1;
      m_nextValidId = -1;
      m_accounts = new List<string>();
    }

    ~IBApiAdapter()
    {
      m_clientSocket.eDisconnect(); //this will terminate the response reader thread
    }

    //finalizers


    //interface implementations


    //properties
    public bool IsConnected { get => m_clientSocket.IsConnected(); }
    public int NextValidId { get => m_nextValidId; }
    public string IP { get => m_ip; }
    public int Port { get => m_port; }
    public IList<string> Accounts { get => m_accounts; }

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

    public void accountDownloadEnd(string account)
    {      
      throw new NotImplementedException();
    }

    public void accountSummary(int reqId, string account, string tag, string value, string currency)
    {
      throw new NotImplementedException();
    }

    public void accountSummaryEnd(int reqId)
    {
      throw new NotImplementedException();
    }

    public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
    {
      throw new NotImplementedException();
    }

    public void accountUpdateMultiEnd(int requestId)
    {
      throw new NotImplementedException();
    }

    public void bondContractDetails(int reqId, ContractDetails contract)
    {
      throw new NotImplementedException();
    }

    public void commissionReport(CommissionReport commissionReport)
    {
      throw new NotImplementedException();
    }

    public void completedOrder(Contract contract, Order order, OrderState orderState)
    {
      throw new NotImplementedException();
    }

    public void completedOrdersEnd()
    {
      throw new NotImplementedException();
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
      throw new NotImplementedException();
    }

    public void contractDetailsEnd(int reqId)
    {
      throw new NotImplementedException();
    }

    public void currentTime(long time)
    {
      throw new NotImplementedException();
    }

    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
    {
      throw new NotImplementedException();
    }

    public void displayGroupList(int reqId, string groups)
    {
      throw new NotImplementedException();
    }

    public void displayGroupUpdated(int reqId, string contractInfo)
    {
      throw new NotImplementedException();
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

    public void execDetails(int reqId, Contract contract, Execution execution)
    {
      throw new NotImplementedException();
    }

    public void execDetailsEnd(int reqId)
    {
      throw new NotImplementedException();
    }

    public void familyCodes(FamilyCode[] familyCodes)
    {
      throw new NotImplementedException();
    }

    public void fundamentalData(int reqId, string data)
    {
      throw new NotImplementedException();
    }

    public void headTimestamp(int reqId, string headTimestamp)
    {
      throw new NotImplementedException();
    }

    public void histogramData(int reqId, HistogramEntry[] data)
    {
      throw new NotImplementedException();
    }

    public void historicalData(int reqId, Bar bar)
    {
      throw new NotImplementedException();
    }

    public void historicalDataEnd(int reqId, string start, string end)
    {
      throw new NotImplementedException();
    }

    public void historicalDataUpdate(int reqId, Bar bar)
    {
      throw new NotImplementedException();
    }

    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
    {
      throw new NotImplementedException();
    }

    public void historicalNewsEnd(int requestId, bool hasMore)
    {
      throw new NotImplementedException();
    }

    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
    {
      throw new NotImplementedException();
    }

    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
    {
      throw new NotImplementedException();
    }

    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
    {
      throw new NotImplementedException();
    }

    public void managedAccounts(string accountsList)
    {
      m_accounts = accountsList.Split(',').ToList();
    }

    public void marketDataType(int reqId, int marketDataType)
    {
      throw new NotImplementedException();
    }

    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
    {
      throw new NotImplementedException();
    }

    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
    {
      throw new NotImplementedException();
    }

    public void newsArticle(int requestId, int articleType, string articleText)
    {
      throw new NotImplementedException();
    }

    public void newsProviders(NewsProvider[] newsProviders)
    {
      throw new NotImplementedException();
    }

    public void nextValidId(int orderId)
    {
      m_nextValidId = orderId;
    }

    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
    {
      throw new NotImplementedException();
    }

    public void openOrderEnd()
    {
      throw new NotImplementedException();
    }

    public void orderBound(long orderId, int apiClientId, int apiOrderId)
    {
      throw new NotImplementedException();
    }

    public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
    {
      throw new NotImplementedException();
    }

    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
    {
      throw new NotImplementedException();
    }

    public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
    {
      throw new NotImplementedException();
    }

    public void position(string account, Contract contract, double pos, double avgCost)
    {
      throw new NotImplementedException();
    }

    public void positionEnd()
    {
      throw new NotImplementedException();
    }

    public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
    {
      throw new NotImplementedException();
    }

    public void positionMultiEnd(int requestId)
    {
      throw new NotImplementedException();
    }

    public void realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count)
    {
      throw new NotImplementedException();
    }

    public void receiveFA(int faDataType, string faXmlData)
    {
      throw new NotImplementedException();
    }

    public void rerouteMktDataReq(int reqId, int conId, string exchange)
    {
      throw new NotImplementedException();
    }

    public void rerouteMktDepthReq(int reqId, int conId, string exchange)
    {
      throw new NotImplementedException();
    }

    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
    {
      throw new NotImplementedException();
    }

    public void scannerDataEnd(int reqId)
    {
      throw new NotImplementedException();
    }

    public void scannerParameters(string xml)
    {
      throw new NotImplementedException();
    }

    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
    {
      throw new NotImplementedException();
    }

    public void securityDefinitionOptionParameterEnd(int reqId)
    {
      throw new NotImplementedException();
    }

    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
    {
      throw new NotImplementedException();
    }

    public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
    {
      throw new NotImplementedException();
    }

    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
    {
      throw new NotImplementedException();
    }

    public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttriblast, string exchange, string specialConditions)
    {
      throw new NotImplementedException();
    }

    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
    {
      throw new NotImplementedException();
    }

    public void tickByTickMidPoint(int reqId, long time, double midPoint)
    {
      throw new NotImplementedException();
    }

    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
    {
      throw new NotImplementedException();
    }

    public void tickGeneric(int tickerId, int field, double value)
    {
      throw new NotImplementedException();
    }

    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
    {
      throw new NotImplementedException();
    }

    public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
    {
      throw new NotImplementedException();
    }

    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
      throw new NotImplementedException();
    }

    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
    {
      throw new NotImplementedException();
    }

    public void tickSize(int tickerId, int field, int size)
    {
      throw new NotImplementedException();
    }

    public void tickSnapshotEnd(int tickerId)
    {
      throw new NotImplementedException();
    }

    public void tickString(int tickerId, int field, string value)
    {
      throw new NotImplementedException();
    }

    public void updateAccountTime(string timestamp)
    {
      throw new NotImplementedException();
    }

    public void updateAccountValue(string key, string value, string currency, string accountName)
    {
      throw new NotImplementedException();
    }

    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
    {
      throw new NotImplementedException();
    }

    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth)
    {
      throw new NotImplementedException();
    }

    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
    {
      throw new NotImplementedException();
    }

    public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
    {
      throw new NotImplementedException();
    }

    public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
    {
      throw new NotImplementedException();
    }

    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
    {
      throw new NotImplementedException();
    }

    public void verifyCompleted(bool isSuccessful, string errorText)
    {
      throw new NotImplementedException();
    }

    public void verifyMessageAPI(string apiData)
    {
      throw new NotImplementedException();
    }
  }
}
