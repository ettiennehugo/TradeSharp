using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;
using TradeSharp.InteractiveBrokers.Messages;


namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Data provider plugin implementation for Interactive Brokers.
  /// NOTES: Updated 14 April 2024
  /// - Currently IB only returns historical data for instruments that are still actively traded that can lead to survivorship bias.
  /// - Currretly for seconds resolution, IB only returns data only for up to 6-months back (InstrumentAdapter will limit this as well and will log a warning if dates are requested further back than 6-months).
  /// </summary>
  [ComVisible(true)]
  [Guid("D6BF3AE3-F358-4066-B177-D9763F927D67")]
  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin
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
    public DataProviderPlugin() : base(Constants.DefaultName) { }

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
      if (IsConnected)
        raiseConnected();
    }

    public override object Request(string ticker, Resolution resolution, DateTime start, DateTime end)
    {
      throw new NotImplementedException();
    }

    //properties
    public override bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
    public override int ConnectionCountMax => 1;  //IB limits the number of connections to 1 and it's also limited by 50 calls per second (9 April 2024)

    //methods

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
