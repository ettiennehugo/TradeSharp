using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.CoreUI.Services;
using TradeSharp.InteractiveBrokers.Messages;
using TradeSharp.CoreUI.Common;

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
  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin, IInteractiveBrokersPlugin, IMassDownload
  {
    //constants


    //enums


    //types


    //attributes
    protected IDialogService m_dialogService;
    protected IInstrumentService m_instrumentService;
    protected ServiceHost m_ibServiceHost;
    protected string m_ip;
    protected int m_port;

    //constructors
    public DataProviderPlugin() : base(Constants.DefaultName) { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_instrumentService = (IInstrumentService)ServiceHost.Services.GetService(typeof(IInstrumentService))!;
      m_ip = (string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.IpKey];
      m_port = int.Parse((string)Configuration!.Configuration[TradeSharp.InteractiveBrokers.Constants.PortKey]);
      m_ibServiceHost = InteractiveBrokers.ServiceHost.GetInstance(ServiceHost, Configuration);
      m_ibServiceHost.Client.ConnectionStatus += HandleConnectionStatus;
      Commands.Add(new PluginCommand { Name = "Connect", Tooltip = "Connect to TWS API", Icon = "\uE8CE", Command = new AsyncRelayCommand(OnConnectAsync, () => !IsConnected) });
      Commands.Add(new PluginCommand { Name = "Disconnect", Tooltip = "Disconnect from TWS API", Icon = "\uE8CD", Command = new AsyncRelayCommand(OnDisconnectAsync, () => IsConnected) });
      Commands.Add(new PluginCommand { Name = PluginCommand.Separator });
      raiseUpdateCommands();
    }

    public override void Dispose()
    {
      if (m_ibServiceHost.Client.IsConnected) m_ibServiceHost.Client.Disconnect();
      base.Dispose();
    }

    public override object Request(string ticker, Resolution resolution, DateTime start, DateTime end)
    {


      throw new NotImplementedException();



    }

    //properties
    public bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
    public override int ConnectionCountMax => 1;  //IB limits the number of connections to 1 and it's also limited by 50 calls per second (9 April 2024)

    //delegates
    public event EventHandler? Connected;                      //event raised when the plugin connects to the remote service
    public event EventHandler? Disconnected;                   //event raised when the plugin disconnects from the remote service

    //methods
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

    public void Download(DateTime start, DateTime end, Resolution resolution, IList<string> tickers)
    {
      IProgressDialog progress = m_dialogService.CreateProgressDialog("Mass Data Download", m_logger);
      progress.StatusMessage = $"Downloading data from InteractiveBrokers for {tickers.Count} instruments between {start} and {end} at {resolution} resolution";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = tickers.Count;
      progress.ShowAsync();

      foreach (var ticker in tickers)
      {
        progress.LogInformation($"Requesting data for {ticker}");
        Request(ticker, resolution, start, end);
        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested)
        {
          progress.StatusMessage += " - Cancelled";
          break;
        }
      }

      progress.Complete = true;
    }
  }
}
