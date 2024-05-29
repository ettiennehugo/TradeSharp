using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Services;
using TradeSharp.Common;

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
    protected IInstrumentService m_instrumentService;
    protected ServiceHost m_ibServiceHost;

    //constructors
    public DataProviderPlugin() : base(Constants.DefaultName, $"{Constants.DefaultName} - connect via Broker Plugin") { }

    //finalizers


    //interface implementations
    public override void Create(ILogger logger)
    {
      base.Create(logger);
      m_dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
      m_instrumentService = (IInstrumentService)ServiceHost.Services.GetService(typeof(IInstrumentService))!;
      //the broker plugin contains the connection details required for the cache etc.
      var configurationService = (IConfigurationService)ServiceHost.Services.GetService(typeof(IConfigurationService))!;
      configurationService.Brokers.TryGetValue(Constants.DefaultName, out IPluginConfiguration? configuration);
      m_ibServiceHost = InteractiveBrokers.ServiceHost.GetInstance(ServiceHost, configuration);
    }

    public override bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end)
    {
      if (!IsConnected)
      {
        m_logger.LogError("Failed to request historical data, not connected to TWS API - connect using Broker Plugin");
        return false;
      }

      IBApi.Contract? contract = m_ibServiceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

      if (contract == null)
        foreach (var ticker in instrument.AlternateTickers)
        {
          contract = m_ibServiceHost.Cache.GetContract(ticker, Constants.DefaultExchange);
          if (contract != null) break;
        }

      if (contract == null) return false;

      m_ibServiceHost.Instruments.RequestHistoricalData(contract, start, end, resolution);     
      return true;
    }

    //properties
    public override bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
    public override int ConnectionCountMax => 1;  //IB limits the number of connections to 1 and it's also limited by 50 calls per second (9 April 2024)

    //delegates


    //methods


  }
}
