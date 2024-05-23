using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;
using TradeSharp.InteractiveBrokers.Messages;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Implementation of a broker plugin for Interactive Brokers, this is typically an adapter between TradeSharp and Interactive Brokers.
  /// </summary>
  [ComVisible(true)]
  [Guid("617D70F7-F0D8-4BCD-8FCF-DAF41135EF16")]
  public class BrokerPlugin : TradeSharp.Data.BrokerPlugin, IInteractiveBrokersPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;
    protected ServiceHost m_ibServiceHost;
    protected string m_ip;
    protected int m_port;

    //constructors
    public BrokerPlugin() : base(Constants.DefaultName) { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_ip = (string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = int.Parse((string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey]);
      m_ibServiceHost = InteractiveBrokers.ServiceHost.GetInstance(ServiceHost, Configuration);
      m_ibServiceHost.Client.ConnectionStatus += HandleConnectionStatus;
      Commands.Add(new PluginCommand { Name = "Connect", Tooltip = "Connect to TWS API", Icon = "\uE8CE", Command = new AsyncRelayCommand(OnConnectAsync, () => !IsConnected) } );
      Commands.Add(new PluginCommand { Name = "Disconnect", Tooltip = "Disconnect from TWS API", Icon = "\uE8CD", Command = new AsyncRelayCommand(OnDisconnectAsync, () => IsConnected) } );
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      Commands.Add(new PluginCommand { Name = "Scan for Contracts", Tooltip = "Run an exhaustive search for new contracts supported by Interactive Brokers", Icon = "\uEC5A", Command = new AsyncRelayCommand(OnScanForContractsAsync, () => IsConnected) } );
      Commands.Add(new PluginCommand { Name = "Download Contracts", Tooltip = "Download the rest of the contract and contract details based off contract headers", Icon = "\uE826", Command = new AsyncRelayCommand(OnSynchronizeContractCacheAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      Commands.Add(new PluginCommand { Name = "Define Exchange", Tooltip = "Define supported Exchanges", Icon = "\uF22C", Command = new AsyncRelayCommand(OnDefineSupportedExchangesAsync) } );
      Commands.Add(new PluginCommand { Name = "Validate Non-IB Instrument Groups", Tooltip = "Validate Non-IB Instrument Groups against IB Instrument Groups", Icon = "\uE15C", Command = new AsyncRelayCommand(OnValidateInstrumentGroupsAsync) } );
      Commands.Add(new PluginCommand { Name = "Copy Classifications to Instrument Groups", Tooltip = "Copy the Interactive Brokers classifications to Instrument Groups", Icon = "\uF413", Command = new AsyncRelayCommand(OnCopyIBClassesToInstrumentGroupsAsync) } );
      Commands.Add(new PluginCommand { Name = "Validate Instruments", Tooltip = "Validate Defined Instruments against Cached Contracts", Icon = "\uE74C", Command = new AsyncRelayCommand(OnValidateInstrumentsAsync) } );
      Commands.Add(new PluginCommand { Name = "Copy Contracts to Instruments", Tooltip = "Copy the Interactive Brokers contracts to Instruments", Icon = "\uE8C8", Command = new AsyncRelayCommand(OnCopyContractsToInstrumentsAsync) } );
      raiseUpdateCommands();
    }

    public override void Dispose()
    {
      m_ibServiceHost.Client.Dispose();
      m_ibServiceHost.Cache.Dispose();
      base.Dispose();
    }

    public Task OnConnectAsync()
    {
      return Task.Run(() => {
        m_ibServiceHost.Client.Connect(m_ip, m_port);
        raiseUpdateCommands();
      });
    }

    public Task OnDisconnectAsync()
    {
      return Task.Run(() =>
      {
        m_ibServiceHost.Client.Disconnect();
        raiseUpdateCommands();
      });
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
    public bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
    public override IList<TradeSharp.Data.Account> Accounts { get => m_ibServiceHost.Accounts.Accounts; }

    //delegates
    public event EventHandler? Connected;                      //event raised when the plugin connects to the remote service
    public event EventHandler? Disconnected;                   //event raised when the plugin disconnects from the remote service

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
        raiseConnected();
      else
        raiseDisconnected();
      raiseUpdateCommands();
    }

    protected void raiseConnected() { if (Connected != null) Connected(this, new EventArgs()); }
    protected void raiseDisconnected() { if (Disconnected != null) Disconnected(this, new EventArgs()); }
  }
}
