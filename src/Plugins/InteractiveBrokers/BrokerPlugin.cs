using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Services;
using System.Runtime.InteropServices;

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
    protected IBApiAdapter m_clientResponseHandler;
    protected string m_ip;
    protected int m_port;


    //constructors
    public BrokerPlugin() : base("InteractiveBrokers") { }

    //finalizers
    ~BrokerPlugin()
    {
      IBApiAdapter.ReleaseInstance(); //when instance count reaches zero, the client socket will be disconnected
    }

    //interface implementations
    public override void Connect()
    {
      base.Connect();
      m_clientResponseHandler.Connect(m_ip, m_port);
      m_clientResponseHandler.RunAsync();
    }

    public override void Disconnect()
    {
      base.Disconnect();
      m_clientResponseHandler.m_clientSocket.eDisconnect();
    }

    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_ip = (string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = int.Parse((string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey]);
      m_clientResponseHandler = IBApiAdapter.GetInstance(logger, ServiceHost, Configuration);
      CustomCommands.Add(new CustomCommand { Name = "Download Contracts", Tooltip = "Download contract definitions", Icon = "\uE826", Command = new AsyncRelayCommand(OnDownloadContractsAsync, () => IsConnected) } );
    }

    public Task OnDownloadContractsAsync()
    {
      return Task.Run(() => {
        var progress = m_dialogService.ShowProgressDialog("Downloading Interactive Brokers Contracts");
        m_clientResponseHandler.DownloadContracts(progress);
      });
    }

    //properties
    public override bool IsConnected { get => m_clientResponseHandler.IsConnected; }
    public override IList<TradeSharp.Data.Account> Accounts { get => (IList<TradeSharp.Data.Account>)m_clientResponseHandler.Accounts; }
    public IBApiAdapter ClientResponseHandler { get => m_clientResponseHandler; }

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
  }
}
