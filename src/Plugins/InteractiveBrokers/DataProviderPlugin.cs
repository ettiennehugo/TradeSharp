﻿

//NOTE: 29 June 2024
// - Historical data support from IB is pretty much non-existent, it's not recommended to use IB for historical data, only 60 historical requests allowed per 10 minute period.
// - Keeping this file in case the data support becomes better and we can complete it.   


//using Microsoft.Extensions.Logging;
//using TradeSharp.Data;
//using System.Runtime.InteropServices;
//using TradeSharp.CoreUI.Services;
//using TradeSharp.Common;

//namespace TradeSharp.InteractiveBrokers
//{
//  /// <summary>
//  /// Data provider plugin implementation for Interactive Brokers.
//  /// NOTES: Updated 29 June 2024
//  /// - Historical data support from IB is pretty much non-existent, it's not recommended to use IB for historical data, only 60 historical requests allowed per 10 minute period.
//  /// - Currently IB only returns historical data for instruments that are still actively traded that can lead to survivorship bias.
//  /// - Currretly for seconds resolution, IB only returns data only for up to 6-months back (InstrumentAdapter will limit this as well and will log a warning if dates are requested further back than 6-months).
//  /// </summary>
//  [ComVisible(true)]
//  [Guid("D6BF3AE3-F358-4066-B177-D9763F927D67")]
//  public class DataProviderPlugin : TradeSharp.Data.DataProviderPlugin
//  {
//    //constants


//    //enums


//    //types


//    //attributes
//    protected ServiceHost m_ibServiceHost;

//    //constructors
//    public DataProviderPlugin() : base(Constants.DefaultName, $"{Constants.DefaultName} - connect via Broker Plugin") { }

//    //finalizers
//    ~DataProviderPlugin()
//    {
//      m_ibServiceHost.Client.Error -= OnError;
//    }

//    //interface implementations
//    public override void Create(ILogger logger)
//    {
//      base.Create(logger);
//      var dialogService = (IDialogService)ServiceHost.Services.GetService(typeof(IDialogService))!;
//      var database = (IDatabase)ServiceHost.Services.GetService(typeof(IDatabase))!;
//      //the broker plugin contains the connection details required for the cache etc.
//      var configurationService = (IConfigurationService)ServiceHost.Services.GetService(typeof(IConfigurationService))!;
//      configurationService.Brokers.TryGetValue(Constants.DefaultName, out IPluginConfiguration? configuration);
//      m_ibServiceHost = InteractiveBrokers.ServiceHost.GetInstance(logger, ServiceHost, dialogService, database, configuration!);
//      m_ibServiceHost.DataProviderPlugin = this;
//      m_ibServiceHost.Client.Error += OnError;
//    }

//    public override bool Request(Instrument instrument, Resolution resolution, DateTime start, DateTime end)
//    {
//      if (!IsConnected)
//      {
//        m_logger.LogError("Failed to request historical data, not connected to TWS API - connect using Broker Plugin");
//        return false;
//      }

//      IBApi.Contract? contract = m_ibServiceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

//      if (contract == null)
//        foreach (var ticker in instrument.AlternateTickers)
//        {
//          contract = m_ibServiceHost.Cache.GetContract(ticker, Constants.DefaultExchange);
//          if (contract != null) break;
//        }

//      if (contract == null) return false;

//      m_ibServiceHost.Instruments.RequestHistoricalData(contract, start, end, resolution);     
//      return true;
//    }

//    //properties
//    public override bool IsConnected { get => m_ibServiceHost.Client.IsConnected; }
//    public override int ConnectionCountMax => 1;  //IB limits the number of connections to 1 and it's also limited by 50 calls per second (9 April 2024)

//    //delegates


//    //methods
//    public void OnError(int id, int errorCode, string message, string advErrorJson, Exception? exception)
//    {
//      string errorMessage = $"{message} - (id: {id}, code: {errorCode})";
//      raiseRequestError(errorMessage, exception);
//    }

//    public void RaiseDataDownloadComplete(IBApi.Contract contract, Resolution resolution, long count)
//    {
//      var instrument = m_ibServiceHost.Cache.GetInstrument(contract);
//      if (instrument != null)
//        raiseDataDownloadComplete(instrument, resolution, count);
//      else
//        m_logger.LogWarning($"Failed to find instrument for contract: {contract.SecId} - {contract.SecIdType}");
//    }
//  }
//}
