using System.Diagnostics;
using IBApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using TradeSharp.InteractiveBrokers.Messages;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter to synchronize and convert between TradeSharp and Interactive Brokers account object, this includes the basic account information, positions held in the account and changes in the positions.
  /// NOTE: The Handle* methods are called by another thread so we need to ensure we lock some of the adapter data when accessing it to avoid race conditions. Locking could lead to deadlocks so be coginizant
  ///       of the order of locks.
  /// </summary>
  public class AccountAdapter
  {
    //constants
    private const int ACCOUNT_ID_BASE = 10000000;
    private const int ACCOUNT_SUMMARY_ID = ACCOUNT_ID_BASE + 1;
    private const string ACCOUNT_SUMMARY_TAGS = $"{AccountSummaryTags.AccountType},{AccountSummaryTags.NetLiquidation},{AccountSummaryTags.TotalCashValue},{AccountSummaryTags.SettledCash},{AccountSummaryTags.AccruedCash},{AccountSummaryTags.BuyingPower},{AccountSummaryTags.EquityWithLoanValue},{AccountSummaryTags.PreviousEquityWithLoanValue},"
         + $"{AccountSummaryTags.GrossPositionValue},{AccountSummaryTags.ReqTEquity},{AccountSummaryTags.ReqTMargin},{AccountSummaryTags.SMA},{AccountSummaryTags.InitMarginReq},{AccountSummaryTags.MaintMarginReq},{AccountSummaryTags.AvailableFunds},{AccountSummaryTags.ExcessLiquidity},{AccountSummaryTags.Cushion},{AccountSummaryTags.FullInitMarginReq},{AccountSummaryTags.FullMaintMarginReq},{AccountSummaryTags.FullAvailableFunds},"
         + $"{AccountSummaryTags.FullExcessLiquidity},{AccountSummaryTags.LookAheadNextChange},{AccountSummaryTags.LookAheadInitMarginReq},{AccountSummaryTags.LookAheadMaintMarginReq},{AccountSummaryTags.LookAheadAvailableFunds},{AccountSummaryTags.LookAheadExcessLiquidity},{AccountSummaryTags.HighestSeverity},{AccountSummaryTags.DayTradesRemaining},{AccountSummaryTags.Leverage}";

    //enums


    //types


    //attributes
    private static AccountAdapter? s_instance;
    protected ServiceHost m_serviceHost;
    protected ILogger m_logger;
    protected List<string> m_subscribedAccounts;
    protected IInstrumentService m_instrumentService;
    protected IInstrumentBarDataService m_instrumentBarDataService;

    //constructors
    public static AccountAdapter GetInstance(ServiceHost host)
    {
      if (s_instance == null) s_instance = new AccountAdapter(host);
      return s_instance;
    }

    protected AccountAdapter(ServiceHost serviceHost)
    {
      m_logger = serviceHost.Host.Services.GetRequiredService<ILogger<AccountAdapter>>();
      m_serviceHost = serviceHost;
      AccountIds = new ObservableCollection<string>();
      Accounts = new ObservableCollection<Data.Account>();
      m_subscribedAccounts = new List<string>();
      m_instrumentService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentService>();
      m_instrumentBarDataService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentBarDataService>();
    }

    //finalizers
    ~AccountAdapter()
    {
      if (m_serviceHost.Client.IsConnected)
        UnsubscribeUpdatesAllAccounts();
    }

    //interface implementations
    public void RequestAccountSummary()
    {
      AccountIds.Clear();
      Accounts.Clear();
      m_serviceHost.Client.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", ACCOUNT_SUMMARY_TAGS);
    }

    public void CancelAccountSummary()
    {
      m_serviceHost.Client.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
    }

    public void SubscribeAccountUpdates(Account account)
    {
      SubscribeAccountUpdates(account.Name);
    }

    public void UnsubscribeAccountUpdates(Account account)
    {
      UnsubscribeAccountUpdates(account.Name);
    }

    public void SubscribeAccountUpdates(string accountName)
    {
      if (!m_subscribedAccounts.Contains(accountName))
      {
        m_subscribedAccounts.Add(accountName);
        m_serviceHost.Client.ClientSocket.reqAccountUpdates(true, accountName);
      }
    }

    public void UnsubscribeAccountUpdates(string accountName)
    {
      if (m_subscribedAccounts.Contains(accountName))
      {
        m_subscribedAccounts.Remove(accountName);
        m_serviceHost.Client.ClientSocket.reqAccountUpdates(false, accountName);
      }
    }

    public void SubscribeUpdatesAllAccounts()
    {
      var accountNames = new List<string>(AccountIds);  //need to copy the list as it may change during the loop
      foreach (string accountName in accountNames)
        SubscribeAccountUpdates(accountName);
    }

    public void UnsubscribeUpdatesAllAccounts()
    {
      foreach (string accountName in m_subscribedAccounts)
        UnsubscribeAccountUpdates(accountName);
    }

    public void RequestPositions()
    {
      m_serviceHost.Client.ClientSocket.reqPositions();
    }

    public void RequestOpenOrders()
    {
      m_serviceHost.Client.ClientSocket.reqAllOpenOrders();
    }

    public void RequestCompletedOrders()
    {
      m_serviceHost.Client.ClientSocket.reqCompletedOrders(false);  //we retrieve all orders, both submitted to the API and via TWS - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#req-completed-orders
    }

    public Instrument? From(Contract contract)
    {
      return m_instrumentService.Items.FirstOrDefault(x => x.Ticker == contract.Symbol.ToUpper());
    }

    public Instrument? From(ContractDetails contractDetails)
    {
      return m_instrumentService.Items.FirstOrDefault(x => x.Ticker == contractDetails.UnderSymbol.ToUpper());
    }

    //properties
    public ObservableCollection<string> AccountIds { get; protected set; }
    public ObservableCollection<Data.Account> Accounts { get; protected set; }

    //events


    //methods


    //Refresh the accounts, positions and orders when the connection is established.
    public void HandleConnectionStatus(ConnectionStatusMessage connectionStatusMessage)
    {
      if (connectionStatusMessage.IsConnected)
        lock (this)
        {
          //we're going to reconstruct the broker accounts, positions and orders from scratch
          m_serviceHost.DialogService.PostUIUpdate(() => Accounts.Clear());

          //request all account data - we include completed orders as well as open orders
          RequestAccountSummary();
          RequestPositions();
          SubscribeUpdatesAllAccounts();
          RequestOpenOrders();
          RequestCompletedOrders();
        }
    }

    //NOTE: Handle methods need to lock the account adapter to ensure exclusive access to the defined set of accounts.
    public void HandleAccountSummary(AccountSummaryMessage summaryMessage)
    {
      Account? account = null;
      lock (this)
      {
        account = resolveAccount(summaryMessage.Account);
        m_serviceHost.DialogService.PostUIUpdate(() => setAccountValue(account, "HandleAccountSummary", summaryMessage.RequestId, summaryMessage.Account, summaryMessage.Tag, summaryMessage.Value, summaryMessage.Currency));
      }
      if (account != null) m_serviceHost.BrokerPlugin.raiseAccountUpdated(new AccountUpdatedArgs(account));
    }

    public void HandleUpdateAccountValue(AccountValueMessage accountValueMessage)
    {
      Account? account = null;
      lock(this)
      {
        account = resolveAccount(accountValueMessage.Account);
        m_serviceHost.DialogService.PostUIUpdate(() => setAccountValue(account, "HandleAccountValue", -1 /* account value does not have a request id */, accountValueMessage.Account, accountValueMessage.Key, accountValueMessage.Value, accountValueMessage.Currency));
      }
      if (account != null) m_serviceHost.BrokerPlugin.raiseAccountUpdated(new AccountUpdatedArgs(account));
    }

    public void HandleUpdatePortfolio(UpdatePortfolioMessage updatePortfolioMessage)
    {
      Position? position = null;
      lock (this)
        m_serviceHost.DialogService.PostUIUpdate(() =>
        {
          Account account = resolveAccount(updatePortfolioMessage.Account);
          Contract contract = updatePortfolioMessage.Contract;
          position = resolvePosition(account, contract);
          position.Size = updatePortfolioMessage.Position;
          position.MarketPrice = (decimal)updatePortfolioMessage.MarketPrice;
          position.MarketValue = (decimal)updatePortfolioMessage.MarketValue;
          position.AverageCost = (decimal)updatePortfolioMessage.AverageCost;
          position.UnrealizedPnl = (decimal)updatePortfolioMessage.UnrealizedPNL;
          position.RealizedPnl = (decimal)updatePortfolioMessage.RealizedPNL;
        });
      if (position != null) m_serviceHost.BrokerPlugin.raisePositionUpdated(new PositionUpdatedArgs(position));
    }

    public void HandlePosition(PositionMessage positionMessage)
    {
      Position? position = null;
      lock (this)
        m_serviceHost.DialogService.PostUIUpdate(() =>
        {
          Account account = resolveAccount(positionMessage.Account);
          Contract contract = positionMessage.Contract;
          position = resolvePosition(account, contract);
          position.Size = positionMessage.Position;
          position.MarketPrice = (decimal)positionMessage.AverageCost;
          position.MarketValue = (decimal)positionMessage.AverageCost;
          position.AverageCost = (decimal)positionMessage.AverageCost;
          position.UnrealizedPnl = 0;
          position.RealizedPnl = 0;
        });
      if (position != null) m_serviceHost.BrokerPlugin.raisePositionUpdated(new PositionUpdatedArgs(position));
    }

    public void HandleOrderStatus(OrderStatusMessage orderStatusMessage)
    {
      Order? order = null;
      lock (this)
        m_serviceHost.DialogService.PostUIUpdate(() =>
        {
          order = resolveOrder(orderStatusMessage.OrderId);
          if (order == null)
          {
            m_logger.LogWarning($"HandleOrderStatus - order not found - {orderStatusMessage.OrderId}");
            return;
          }

          //https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#order-status-message
          string orderStatus = orderStatusMessage.Status.ToUpper();
          if (orderStatus == Constants.OrderStatusPendingSubmit)
            order.Status = Data.Order.OrderStatus.PendingSubmit;
          else if (orderStatus == Constants.OrderStatusPendingCancel)
            order.Status = Data.Order.OrderStatus.PendingCancel;
          else if (orderStatus == Constants.OrderStatusSubmitted)
            order.Status = Data.Order.OrderStatus.Open;
          else if (orderStatus == Constants.OrderStatusInactive)
            order.Status = Data.Order.OrderStatus.Inactive;
          else if (orderStatus == Constants.OrderStatusFilled)
            order.Status = Data.Order.OrderStatus.Filled;
          else if (orderStatus == Constants.OrderStatusCancelled)
            order.Status = Data.Order.OrderStatus.Cancelled;
          else
            m_logger.LogWarning($"HandleOrderStatus - unknown order status - {orderStatusMessage.Status}");

          order.Filled = orderStatusMessage.Filled == double.MaxValue ? 0.0 : orderStatusMessage.Filled;
          order.Remaining = orderStatusMessage.Remaining == double.MaxValue ? 0.0 : orderStatusMessage.Remaining;
          order.AverageFillPrice = orderStatusMessage.AvgFillPrice == double.MaxValue ? decimal.Zero : (decimal)orderStatusMessage.AvgFillPrice;
          order.LastFillPrice = orderStatusMessage.LastFillPrice == double.MaxValue ? decimal.Zero : (decimal)orderStatusMessage.LastFillPrice;
        });

      if (order != null) m_serviceHost.BrokerPlugin.raiseOrderUpdated(new OrderUpdatedArgs(order));
    }

    public void HandleOpenOrder(OpenOrderMessage openOrderMessage)
    {
      Order? order = null;
      lock (this)
        m_serviceHost.DialogService.PostUIUpdate(() =>
        {
          Account? account = resolveAccount(openOrderMessage.Order.Account);
          if (account == null)
          {
            m_logger.LogWarning($"HandleOpenOrder - account not found - {openOrderMessage.Order.Account}");
            return;
          }

          order = resolveOrder(account, openOrderMessage.OrderId);
          order.Instrument = From(openOrderMessage.Contract)!;  //intrument should exist if we're receiving an open order on it
          order.Status = openOrderMessage.OrderState.Status == "" ? Data.Order.OrderStatus.Filled : Data.Order.OrderStatus.Open;   //TODO - need to complete this
          order.Filled = openOrderMessage.Order.FilledQuantity == double.MaxValue ? 0.0 : openOrderMessage.Order.FilledQuantity;
          order.Quantity = order.Filled;
          double total = openOrderMessage.Order.TotalQuantity == double.MaxValue ? 0.0 : openOrderMessage.Order.TotalQuantity;
          order.Remaining = total - order.Filled;
          //NOTE: AverageFillPrice and LastFillPrice are not available in the OpenOrderMessage, need to check if they are available in the OrderState.
        });
      if (order != null) m_serviceHost.BrokerPlugin.raiseOrderUpdated(new OrderUpdatedArgs(order));
    }

    protected Account resolveAccount(string accountName)
    {
      Account? account = (Account?)Accounts.FirstOrDefault(a => a.Name == accountName);
      if (account == null)
      {
        m_logger.LogInformation($"Adding account - {accountName}");
        AccountIds.Add(accountName);
        account = new Account(m_serviceHost.BrokerPlugin) { Name = accountName };
        Debug.Assert(account != null);
        Accounts.Add(account);
        m_serviceHost.BrokerPlugin.raiseAccountsUpdated();    //the AccountAdapter raises the add event while the broker plugin will raise the remove event (typically when disconnection occurs)        
      }
      return account;
    }

    protected Position resolvePosition(Account account, Contract contract)
    {
      Position? position = account.Positions.FirstOrDefault(x => x.Instrument?.Ticker == contract.Symbol.ToUpper());
      if (position == null)
      {
        position = new Position(account, From(contract)!, PositionDirection.Long, 0, 0, 0, 0, 0, 0);
        account.Positions.Add(position);
      }
      return position;
    }

    protected Order resolveOrder(Data.Account account, int orderId)
    {
      Order? order = (Order?)account.Orders.FirstOrDefault(x => ((Order)x).OrderId == orderId);
      if (order == null)
      {
        order = new Order(orderId);
        account.Orders.Add(order);
      }
      return order;
    }

    protected Order? resolveOrder(int orderId)
    {
      Order? order = null;

      foreach (var account in Accounts)
      {
        order = (Order?)account.Orders.FirstOrDefault(x => ((Order)x).OrderId == orderId);
        if (order != null)
          break;
      }
      return order;
    }

    //Update account value according to the key/value pair given.
    protected Account setAccountValue(Account account, string responseName, int reqId, string accountName, string key, string value, string currency)
    {
      account.LastSyncDateTime = DateTime.Now;
      account.Currency = currency;    //NOTE: We assume account would have same currency for all values.

      //NOTE: The currency values received from IB are in double format so we need to parse them as such and convert them into
      //      the internal decimal format used by TradeSharp.
      if (key == AccountSummaryTags.AccountType)
      {
        account.AccountType = value;
      }
      else if (key == AccountSummaryTags.NetLiquidation)
      {
        if (double.TryParse(value, out double result))
          account.NetLiquidation = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.SettledCash)
      {
        if (double.TryParse(value, out double result))
          account.SettledCash = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.BuyingPower)
      {
        if (double.TryParse(value, out double result))
          account.BuyingPower = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.MaintMarginReq)
      {
        if (double.TryParse(value, out double result))
          account.MaintenanceMargin = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.GrossPositionValue)
      {
        if (double.TryParse(value, out double result))
          account.PositionsValue = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.AvailableFunds)
      {
        if (double.TryParse(value, out double result))
          account.AvailableFunds = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.ExcessLiquidity)
      {
        if (double.TryParse(value, out double result))
          account.ExcessLiquidity = result == double.MaxValue ? decimal.Zero : (decimal)result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else
      {
        //some unknown property, add it as a custom property based on simple supported types
        if (account.CustomProperties.ContainsKey(key)) account.CustomProperties.Remove(key);  //always update key values
        if (int.TryParse(value, out int intResult))
          account.CustomProperties.Add(key, new CustomProperty { Name = key, Description = key, Type = typeof(int), Value = intResult, Unit = currency });
        else if (double.TryParse(value, out double doubleResult))
          account.CustomProperties.Add(key, new CustomProperty { Name = key, Description = key, Type = typeof(double), Value = doubleResult, Unit = currency });
        else if (bool.TryParse(value, out bool boolResult))
          account.CustomProperties.Add(key, new CustomProperty { Name = key, Description = key, Type = typeof(bool), Value = boolResult, Unit = currency });
        else if (DateTime.TryParse(value, out DateTime dateTimeResult))
          account.CustomProperties.Add(key, new CustomProperty { Name = key, Description = key, Type = typeof(DateTime), Value = dateTimeResult, Unit = currency });
        else
          account.CustomProperties.Add(key, new CustomProperty { Name = key, Description = key, Type = typeof(string), Value = value, Unit = currency });
      }

      return account;
    }
  }
}
