using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using TradeSharp.InteractiveBrokers.Messages;
using System.Collections.ObjectModel;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Entry for the Interactive Brokers server maintenance schedule.
  /// </summary>
  public class MaintenanceScheduleEntry
  {
    public MaintenanceScheduleEntry(DayOfWeek startDay, TimeOnly startTime, TimeOnly endTime, TimeZoneInfo timeZone)
    {
      StartDay = startDay;
      StartTime = startTime;

      if (endTime < startTime)
        EndDay = startDay + 1;  //end time is on the next day
      else
        EndDay = startDay;

      EndTime = endTime;
      TimeZone = timeZone;
    }

    public DayOfWeek StartDay { get; }
    public TimeOnly StartTime { get; }
    public DayOfWeek EndDay { get; }
    public TimeOnly EndTime { get; }
    public TimeZoneInfo TimeZone { get; } = TimeZoneInfo.Local;
  }

  /// <summary>
  /// Implementation of a broker plugin for Interactive Brokers, this is typically an adapter between TradeSharp and Interactive Brokers.
  /// </summary>
  [ComVisible(true)]
  [Guid("617D70F7-F0D8-4BCD-8FCF-DAF41135EF16")]
  public class BrokerPlugin : TradeSharp.Data.BrokerPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;
    protected ServiceHost m_ibServiceHost;
    protected string m_ip;
    protected int m_port;
    protected bool m_autoConnect;
    protected bool m_autoReconnect;
    protected List<MaintenanceScheduleEntry> m_maintenanceSchedule;
    protected TimeSpan m_autoReconnectInterval;
    protected bool m_manuallyDisconnected;
    protected Timer? m_autoReconnectTimer;

    //constructors
    public BrokerPlugin() : base(Constants.DefaultName, Constants.DefaultName)
    {
      m_maintenanceSchedule = new List<MaintenanceScheduleEntry>();
      m_autoConnect = InteractiveBrokers.Constants.DefaultAutoConnect;
      m_autoReconnect = InteractiveBrokers.Constants.DefaultAutoReconnect;
      m_autoReconnectInterval = new TimeSpan(0, InteractiveBrokers.Constants.DefaultAutoReconnectIntervalMinutes, 0);
      m_manuallyDisconnected = false;
    }

    //finalizers
    ~BrokerPlugin()
    {
      m_manuallyDisconnected = true;  //edge case when application is shutdown and we're still connected we need to disable auto-reconnect
    }

    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_ip = (string)Configuration!.Configuration[InteractiveBrokers.Constants.IpKey];
      m_port = int.Parse((string)Configuration!.Configuration[InteractiveBrokers.Constants.PortKey]);
      m_autoConnect = Configuration!.Configuration.ContainsKey(InteractiveBrokers.Constants.AutoConnectKey) ? bool.Parse((string)Configuration!.Configuration[InteractiveBrokers.Constants.AutoConnectKey]) : InteractiveBrokers.Constants.DefaultAutoConnect;
      m_autoReconnect = Configuration!.Configuration.ContainsKey(InteractiveBrokers.Constants.AutoReconnectKey) ? bool.Parse((string)Configuration!.Configuration[InteractiveBrokers.Constants.AutoReconnectKey]) : InteractiveBrokers.Constants.DefaultAutoReconnect;
      parseAutoReconnectInterval();
      parseMaintenanceSchedule();
      var database = (IDatabase)ServiceHost.Services.GetService(typeof(IDatabase))!;
      m_ibServiceHost = InteractiveBrokers.ServiceHost.GetInstance(logger, ServiceHost, m_dialogService, database, Configuration);
      m_ibServiceHost.BrokerPlugin = this;
      m_ibServiceHost.Client.ConnectionStatus += HandleConnectionStatus;
      Commands.Add(new PluginCommand { Name = "Connect", Tooltip = "Connect to TWS API", Icon = "\uE8CE", Command = new AsyncRelayCommand(OnConnectAsync, () => !IsConnected) });
      Commands.Add(new PluginCommand { Name = "Disconnect", Tooltip = "Disconnect from TWS API", Icon = "\uE8CD", Command = new AsyncRelayCommand(OnDisconnectAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      Commands.Add(new PluginCommand { Name = "Accounts", Tooltip = "View accounts", Icon = "\uE923", Command = new AsyncRelayCommand(OnShowAccountsAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = "Scan for Contracts", Tooltip = "Run an exhaustive search for new contracts supported by Interactive Brokers", Icon = "\uEC5A", Command = new AsyncRelayCommand(OnScanForContractsAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = "Download Contracts", Tooltip = "Download the rest of the contract and contract details based off contract headers", Icon = "\uE826", Command = new AsyncRelayCommand(OnSynchronizeContractCacheAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      Commands.Add(new PluginCommand { Name = "Define Exchange", Tooltip = "Define supported Exchanges", Icon = "\uF22C", Command = new AsyncRelayCommand(OnDefineSupportedExchangesAsync) });
      Commands.Add(new PluginCommand { Name = "Validate Non-IB Instrument Groups", Tooltip = "Validate Non-IB Instrument Groups against IB Instrument Groups", Icon = "\uE15C", Command = new AsyncRelayCommand(OnValidateInstrumentGroupsAsync) });
      Commands.Add(new PluginCommand { Name = "Copy Classifications to Instrument Groups", Tooltip = "Copy the Interactive Brokers classifications to Instrument Groups", Icon = "\uF413", Command = new AsyncRelayCommand(OnCopyIBClassesToInstrumentGroupsAsync) });
      Commands.Add(new PluginCommand { Name = "Validate Instruments", Tooltip = "Validate Defined Instruments against Cached Contracts", Icon = "\uE74C", Command = new AsyncRelayCommand(OnValidateInstrumentsAsync) });
      Commands.Add(new PluginCommand { Name = "Copy Contracts to Instruments", Tooltip = "Copy the Interactive Brokers contracts to Instruments", Icon = "\uE8C8", Command = new AsyncRelayCommand(OnCopyContractsToInstrumentsAsync) });
      updateDescription();
      if (m_autoConnect)
        OnConnectAsync();  //auto connect if enabled, auto-connect will call raiseUpdateCommands
      else
        raiseUpdateCommands();
    }

    public Task OnConnectAsync()
    {
      return Task.Run(() =>
      {
        m_manuallyDisconnected = false;   //ensure auto-reconnect works if not manually disconnected
        m_ibServiceHost.Client.Connect(m_ip, m_port);
        raiseConnectionStatus();
        raiseUpdateCommands();
        //broker plugin does not raise the add account event here but the AccountAdapter does that when a new account is added 
      });
    }

    public Task OnDisconnectAsync()
    {
      return Task.Run(() =>
      {
        m_manuallyDisconnected = true;  //ensure auto-reconnect does not kick in when manually disconnected
        m_ibServiceHost.Client.Disconnect();
        Accounts.Clear();
        raiseAccountsUpdated();   //broker plugin raises the account removed event
        raiseConnectionStatus();
        raiseUpdateCommands();
      });
    }

    public Task OnShowAccountsAsync()
    {
      Account account = (Account)m_ibServiceHost.Accounts.Accounts.FirstOrDefault();
      return m_dialogService.ShowAccountDialogAsync(this, account);
      //return m_dialogService.ShowAccountDialogAsync(this);
    }

    public Task OnScanForContractsAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.ScanForContracts);
    }

    public Task OnDefineSupportedExchangesAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.DefineSupportedExchanges);
    }

    public Task OnSynchronizeContractCacheAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.SynchronizeContractCache);
    }

    public Task OnValidateInstrumentGroupsAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.ValidateInstrumentGroups);
    }

    public Task OnValidateInstrumentsAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.ValidateInstruments);
    }

    public Task OnCopyIBClassesToInstrumentGroupsAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.CopyIBClassesToInstrumentGroups);
    }

    public Task OnCopyContractsToInstrumentsAsync()
    {
      return Task.Run(m_ibServiceHost.Instruments.CopyContractsToInstruments);
    }

    //properties
    public override bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
    public override ObservableCollection<Data.Account> Accounts { get => m_ibServiceHost.Accounts.Accounts; }
    public string IP { get => m_ip; }
    public int Port { get => m_port; }
    public bool AutoReconnect { get => m_autoReconnect; }
    public List<MaintenanceScheduleEntry> MaintenanceSchedule { get => m_maintenanceSchedule; }

    //delegates


    //methods
    public void defineCustomProperties(Order order)
    {
      if (order is SimpleOrder)
      {

        //TODO: Define the custom properties for the order.

      }
      else if (order is ComplexOrder)
      {

        //TODO: Define the custom properties for the order.

      }
      else
      {
        m_logger.LogError("Order type not supported.");
      }
    }

    public void HandleConnectionStatus(ConnectionStatusMessage connectionStatusMessage)
    {
      if (connectionStatusMessage.IsConnected)
      {
        raiseConnectionStatus();
        m_autoReconnectTimer?.Dispose();  //cleanup the auto reconnect timer if it was setup
      }
      else
      {
        raiseConnectionStatus();
        setupAutoReconnectTimer();
      }
      raiseUpdateCommands();
    }

    protected void parseAutoReconnectInterval()
    {
      if (Configuration!.Configuration.ContainsKey(InteractiveBrokers.Constants.AutoReconnectIntervalKey))
      {
        if (TimeSpan.TryParse((string)Configuration!.Configuration[InteractiveBrokers.Constants.AutoReconnectIntervalKey], out TimeSpan autoReconnectInterval))
          m_autoReconnectInterval = autoReconnectInterval;
        else
          m_logger.LogError($"Failed to parse AutoReconnectInterval: {Configuration!.Configuration[InteractiveBrokers.Constants.AutoReconnectIntervalKey]}");
      }
    }

    protected void parseMaintenanceSchedule()
    {
      if (!Configuration!.Configuration.ContainsKey(InteractiveBrokers.Constants.MaintenanceScheduleKey)) return;
      string[] entries = ((string)Configuration!.Configuration[InteractiveBrokers.Constants.MaintenanceScheduleKey]).Split(',');
      foreach (var entry in entries)
      {
        Match match = Regex.Match(entry, InteractiveBrokers.Constants.MaintenanceScheduleEntryRegex);
        if (match.Success)
        {
          string dayStr = match.Groups[1].Value;
          string startTimeStr = match.Groups[2].Value;
          string endTimeStr = match.Groups[3].Value;
          string timezoneStr = match.Groups[4].Value;

          if (!Enum.TryParse<DayOfWeek>(dayStr, true, out DayOfWeek day))
          {
            m_logger.LogError($"Failed to parse maintenance schedule entry Day: {entry}");
            continue;
          }

          if (!TimeOnly.TryParse(startTimeStr, out TimeOnly startTime))
          {
            m_logger.LogError($"Failed to parse maintenance schedule entry Start Time: {entry}");
            continue;
          }

          if (!TimeOnly.TryParse(endTimeStr, out TimeOnly endTime))
          {
            m_logger.LogError($"Failed to parse maintenance schedule entry End Time: {entry}");
            continue;
          }

          if (!TimeZoneInfo.TryFindSystemTimeZoneById(timezoneStr, out TimeZoneInfo? timezone))
          {
            m_logger.LogError($"Failed to parse maintenance schedule entry or find TimeZone: {entry}");
            continue;
          }

          m_maintenanceSchedule.Add(new MaintenanceScheduleEntry(day, startTime, endTime, timezone));
        }
        else
          m_logger.LogError($"Failed to parse maintenance schedule entry: {entry}");
      }
    }

    // NOTES:
    //   * Auto-reconnect works best when the TWS API is setup to restart automatically under the "Lock and Exit" settings. Do not use the "Auto logoff" as that seems to raise and exception
    //     that crashes the TWS API and TS.
    protected void setupAutoReconnectTimer()
    {
      if (m_autoReconnect && !m_manuallyDisconnected)
      {
        TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);
        TimeSpan startTime = m_autoReconnectInterval;   //per default try to reconnect again after the auto reconnect interval

        //check whether we should respect the maintenance schedule
        if (MaintenanceSchedule.Count > 0)
        {
          //TBD: This might not be good for Forex instruments that trade 24/7 - might need to reconsider this.
          DayOfWeek day = DateTime.Now.DayOfWeek;
          foreach (var entry in MaintenanceSchedule)
            if (entry.StartDay == day && currentTime >= entry.StartTime && (day < entry.EndDay || currentTime <= entry.EndTime))
            {
              startTime = entry.EndTime - currentTime;  //wait until the end of the maintenance window before trying reconnect
              break;
            }
        }

        m_autoReconnectTimer = new Timer((state) => OnConnectAsync(), null, startTime, m_autoReconnectInterval);  //setup reconnection to start firing and periodically try the reconnection interval
      }
    }

    protected void updateDescription()
    {
      Description = $"TWS API Connection {m_ip}:{m_port}";
      Description += m_autoConnect ? "; Auto-connect enabled" : "; Auto-connect disabled";
      Description += m_autoReconnect ? "; Auto-reconnect enabled" : "; Auto-reconnect disabled";
      Description += $"; Auto-reconnect interval: {m_autoReconnectInterval}";
      if (MaintenanceSchedule.Count > 0)
      {
        Description += "; Maintenance Schedule: ";
        foreach (var entry in MaintenanceSchedule)
          Description += $"{entry.StartDay} {entry.StartTime}-{entry.EndTime} {entry.TimeZone.Id}, ";
        Description = Description.Substring(0, base.Description.Length - 2);  //remove the last comma and space
      }
      else
        Description += "; No maintenance schedule";
    }
  }
}
