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

    //constructors
    public BrokerPlugin() : base(Constants.DefaultName) { }

    //finalizers


    //interface implementations
    public override void Connect()
    {
      base.Connect();
      m_ibServiceHost.Client.Connect(m_ip, m_port);
      raiseConnected();
    }

    public override void Disconnect()
    {
      m_ibServiceHost.Client.Disconnect();
      base.Disconnect();
      raiseDisconnected();
    }

    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_ip = (string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = int.Parse((string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey]);
      m_ibServiceHost = InteractiveBrokers.ServiceHost.GetInstance(ServiceHost, Configuration);
      m_ibServiceHost.Client.ConnectionStatus += HandleConnectionStatus;
      CustomCommands.Add(new CustomCommand { Name = "Scanner Parameters", Tooltip = "Request market scanner parameters", Icon = "\uEC5A", Command = new AsyncRelayCommand(OnScannerParametersAsync, () => IsConnected) } );
      CustomCommands.Add(new CustomCommand { Name = "Download Contracts", Tooltip = "Cache defined instrument contract definitions", Icon = "\uE826", Command = new AsyncRelayCommand(OnSynchronizeContractCacheAsync, () => IsConnected) } );
      CustomCommands.Add(new CustomCommand { Name = "Update Industry Groups", Tooltip = "Update stock industry groupings", Icon = "\uE9D5", Command = new AsyncRelayCommand(OnUpdateInstrumentGroupsAsync, () => IsConnected) } );
      CustomCommands.Add(new CustomCommand { Name = "Validate Instrument Groups", Tooltip = "Validate Defined Instrument Groups against Cached Contracts", Icon = "\uE15C", Command = new AsyncRelayCommand(OnValidateInstrumentGroupsAsync) } );
      CustomCommands.Add(new CustomCommand { Name = "Validate Instruments", Tooltip = "Validate Defined Instruments against Cached Contracts", Icon = "\uE74C", Command = new AsyncRelayCommand(OnValidateInstrumentsAsync) } );
      if (IsConnected)
        raiseConnected();
    }

    public Task OnScannerParametersAsync()
    {
      return Task.Run(m_ibServiceHost.Client.ClientSocket.reqScannerParameters);
    }

    public Task OnSynchronizeContractCacheAsync()
    {
      return Task.Run(() => m_ibServiceHost.Instruments.SynchronizeContractCache());
    }

    public Task OnUpdateInstrumentGroupsAsync()
    {
      return Task.Run(() => m_ibServiceHost.Instruments.UpdateInstrumentGroups());
    }

    public Task OnValidateInstrumentGroupsAsync()
    {
      return Task.Run(() => m_ibServiceHost.Instruments.ValidateInstrumentGroups());
    }

    public Task OnValidateInstrumentsAsync()
    {
      return Task.Run(() => m_ibServiceHost.Instruments.ValidateInstruments());
    }

    //properties
    public override bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
    public override IList<TradeSharp.Data.Account> Accounts { get => m_ibServiceHost.Accounts.Accounts; }

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
  }
}
