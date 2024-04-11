using IBApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using TradeSharp.InteractiveBrokers.Messages;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using static TradeSharp.Data.Position;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter to synchronize and convert between TradeSharp and Interactive Brokers account object, this includes the basic account information, positions held in the account and changes in the positions.
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
    protected bool m_accountSummaryRequestActive;
    protected bool m_accountUpdateActive;
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
      m_accountSummaryRequestActive = true;
      m_accountUpdateActive = false;
      m_subscribedAccounts = new List<string>();
      m_instrumentService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentService>();
      m_instrumentBarDataService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentBarDataService>();
    }

    //finalizers


    //interface implementations
    public void RequestAccountSummary()
    {
      if (!m_accountSummaryRequestActive)
      {
        m_accountSummaryRequestActive = true;
        AccountIds.Clear();
        Accounts.Clear();
        m_serviceHost.Client.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", ACCOUNT_SUMMARY_TAGS);
      }
    }

    public void CancelAccountSummary()
    {
      if (m_accountSummaryRequestActive)
      {
        m_serviceHost.Client.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
        m_accountSummaryRequestActive = false;
      }
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
      foreach (string accountName in AccountIds)
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

    //methods
    public void HandleAccountSummary(AccountSummaryMessage summaryMessage)
    {
      setAccountValue("HandleAccountSummary", summaryMessage.RequestId, summaryMessage.Account, summaryMessage.Tag, summaryMessage.Value, summaryMessage.Currency); 
    }

    public void HandleAccountSummaryEnd(AccountSummaryEndMessage summaryMessageEnd)
    {
      m_accountSummaryRequestActive = false;
    }

    public void HandleUpdateAccountValue(AccountValueMessage accountValueMessage)
    {
      setAccountValue("HandleAccountValue", -1 /* account value does not have a request id */, accountValueMessage.Account, accountValueMessage.Key, accountValueMessage.Value, accountValueMessage.Currency);
    }

    public void HandleUpdatePortfolio(UpdatePortfolioMessage updatePortfolioMessage)
    {
      Account account = resolveAccount(updatePortfolioMessage.AccountName);
      Contract contract = updatePortfolioMessage.Contract;
      Position position = resolvePosition(account, contract);
      position.Size = updatePortfolioMessage.Position;
      position.MarketPrice = updatePortfolioMessage.MarketPrice;
      position.MarketValue = updatePortfolioMessage.MarketValue;
      position.AverageCost = updatePortfolioMessage.AverageCost;
      position.UnrealizedPnl = updatePortfolioMessage.UnrealizedPNL;
      position.RealizedPnl = updatePortfolioMessage.RealizedPNL;
    }

    public void HandlePosition(PositionMessage positionMessage)
    {
      Account account = resolveAccount(positionMessage.Account);
      Contract contract = positionMessage.Contract;
      Position position = resolvePosition(account, contract);
      position.Size = positionMessage.Position;
      position.MarketPrice = positionMessage.AverageCost;
      position.MarketValue = positionMessage.AverageCost;
      position.AverageCost = positionMessage.AverageCost;
      position.UnrealizedPnl = 0;
      position.RealizedPnl = 0;
    }

    public void HandlePositionEnd()
    {
      m_logger.LogTrace("HandlePositionEnd");
    }

    public void HandleOrderStatus(OrderStatusMessage orderStatusMessage)
    {
      Order? order = resolveOrder(orderStatusMessage.OrderId);
      if (order == null)
      {
        m_logger.LogWarning($"HandleOrderStatus - order not found - {orderStatusMessage.OrderId}");
        return;
      }

      order.Status = orderStatusMessage.Status == "Submitted" ? Data.Order.OrderStatus.Open : Data.Order.OrderStatus.Filled;
      order.Filled += orderStatusMessage.Filled;
      order.Remaining = orderStatusMessage.Remaining;
      order.AverageFillPrice = orderStatusMessage.AvgFillPrice;
      order.LastFillPrice = orderStatusMessage.LastFillPrice;
    }

    public void HandleOpenOrder(OpenOrderMessage openOrderMessage)
    {
      m_logger.LogInformation("TODO - HandleOpenOrder not implemented.");

      //Order? order = resolveOrder(openOrderMessage.OrderId);
      //if (order == null)
      //{
      //  m_logger.LogWarning($"HandleOpenOrder - order not found - {openOrderMessage.OrderId}");
      //  return;
      //}

      //order.Status = openOrderMessage.OrderState.Status == "" ? Data.Order.OrderStatus.Filled : Data.Order.OrderStatus.Open;   //TODO - need to complete this
      //order.Size = openOrderMessage.Order. .TotalQuantity;
      //order.
      //order.Filled = openOrderMessage.Order.Filled;
      //order.Remaining = openOrderMessage.Order.Remaining;
      //order.AverageFillPrice = openOrderMessage.Order.AverageFillPrice;
      //order.LastFillPrice = openOrderMessage.Order.LastFillPrice;     
    }

    protected Account resolveAccount(string accountName)
    {
      Account? account = (Account?)Accounts.FirstOrDefault(a => a.Name == accountName);
      if (account == null)
      {
        m_logger.LogInformation($"adding account - {accountName}");
        AccountIds.Add(accountName);
        account = new Account(accountName);
        Accounts.Add(account);
      }

      return account;
    }

    protected Position resolvePosition(Account account, Contract contract)
    {
      Position? position = account.Positions.FirstOrDefault(x => x.Instrument.Ticker == contract.Symbol.ToUpper());      
      if (position == null)
      {
        position = new Position(account, From(contract)!, PositionDirection.Long, 0, 0, 0, 0, 0, 0);
        account.Positions.Add(position);
      }

      return position;
    }

    protected Order resolveOrder(Account account, int orderId)
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
    protected void setAccountValue(string responseName, int reqId, string accountName, string key, string value, string currency)
    {
      Account account = resolveAccount(accountName);
      account.LastSyncDateTime = DateTime.Now;
      account.BaseCurrency = currency;    //NOTE: We assume account would have same currency for all values.

      if (key == AccountSummaryTags.AccountType)
      {
        account.AccountType = value;
      }
      else if (key == AccountSummaryTags.NetLiquidation)
      {
        if (double.TryParse(value, out double result))
          account.NetLiquidation = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.SettledCash)
      {
        if (double.TryParse(value, out double result))
          account.SettledCash = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.BuyingPower)
      {
        if (double.TryParse(value, out double result))
          account.BuyingPower = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.MaintMarginReq)
      {
        if (double.TryParse(value, out double result))
          account.MaintenanceMargin = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.GrossPositionValue)
      {
        if (double.TryParse(value, out double result))
          account.PositionsValue = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.AvailableFunds)
      {
        if (double.TryParse(value, out double result))
          account.AvailableFunds = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
      else if (key == AccountSummaryTags.ExcessLiquidity)
      {
        if (double.TryParse(value, out double result))
          account.ExcessLiquidity = result;
        else
          m_logger.LogWarning($"accountSummary {key} invalid value - {value}");
      }
    }

    //TBD: Do you need this method, the messages from the client is so disparate that you might not be able to use a central method like this.
    protected void setPositionValue(string accountName, Contract contract, PositionDirection direction, double size, double averageCost, double marketValue, double marketPrice, double unrealizedPnl, double realizedPnl)
    {
      Account account = resolveAccount(accountName);  

    }
  }
}
