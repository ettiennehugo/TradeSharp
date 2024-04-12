using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.InteractiveBrokers.Messages;
using IBApi;
using System.Net;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter to handle the retrieval of instrument definitions, their associated historical data and real-time data from Interactive Brokers.
  /// </summary>
  public class InstrumentAdapter
  {
    //constants
    private const int InstrumentIdBase = 20000000;
    public const int HistoricalIdBase = 30000000;
    public const int ContractDetailsId = InstrumentIdBase + 1;
    public const int FundamentalsId = InstrumentIdBase + 2;
    public const int ContractDetailsRequestSleepTime = 25; //sleep time between requests in milliseconds - set limit to be under 50 requests per second https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#requests-limitations

    //enums


    //types


    //attributes
    private static InstrumentAdapter? s_instance;
    protected ServiceHost m_serviceHost;
    private ILogger m_logger;
    private ICountryService m_countryService;
    private IExchangeService m_exchangeService;
    private IInstrumentGroupService m_instrumentGroupService;
    private IInstrumentService m_instrumentService;
    private bool m_contractRequestActive;
    private bool m_fundamentalsRequestActive;
    private int m_historicalRequestCounter = 0;

    //constructors
    public static InstrumentAdapter GetInstance(ServiceHost serviceHost)
    {
      if (s_instance == null) s_instance = new InstrumentAdapter(serviceHost);
      return s_instance;
    }

    protected InstrumentAdapter(ServiceHost serviceHost)
    {
      m_logger = serviceHost.Host.Services.GetRequiredService<ILogger<InstrumentAdapter>>();
      m_serviceHost = serviceHost;
      m_countryService = m_serviceHost.Host.Services.GetRequiredService<ICountryService>();
      m_exchangeService = m_serviceHost.Host.Services.GetRequiredService<IExchangeService>();
      m_instrumentGroupService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentGroupService>();
      m_instrumentService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentService>();
      m_contractRequestActive = false;
      m_fundamentalsRequestActive = false;
    }

    //finalizers


    //interface implementations
    public string InstrumentTypeToIBContractType(InstrumentType instrumentType)
    {
      switch (instrumentType)
      {
        case InstrumentType.ETF:    //etf's trade like stocks under IB - NOTE: This will make converting back to InstrumentType ambiguous.
        case InstrumentType.Stock:
          return Client.ContractTypeStock;
        case InstrumentType.Option:
          return Client.ContractTypeOption;
        case InstrumentType.Future:
          return Client.ContractTypeFuture;
        case InstrumentType.Index:
          return Client.ContractTypeIndex;
        case InstrumentType.Forex:
          return Client.ContractTypeForex;
        case InstrumentType.MutualFund:
          return Client.ContractTypeMutualFund;
      }

      return Client.ContractTypeStock;
    }

    public InstrumentType IBContractTypeToInstrumentType(string contractType)
    {
      if (contractType == Client.ContractTypeStock)
        //NOTE: The instrument might be an ETF, but we'll treat it as a stock for now.
        return InstrumentType.Stock;
      if (contractType == Client.ContractTypeOption)
        return InstrumentType.Option;
      if (contractType == Client.ContractTypeFuture)
        return InstrumentType.Future;
      if (contractType == Client.ContractTypeIndex)
        return InstrumentType.Index;
      if (contractType == Client.ContractTypeForex)
        return InstrumentType.Forex;
      if (contractType == Client.ContractTypeMutualFund)
        return InstrumentType.MutualFund;
      return InstrumentType.Stock;
    }

    public void SynchronizeContractCache()
    {
      m_contractRequestActive = true;
      foreach (var instrument in m_instrumentService.Items)
      {
        var currency = "USD";
        var exchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
        var country = m_countryService.Items.FirstOrDefault(c => c.Id == exchange?.CountryId);
        if (country != null) currency = country?.CountryInfo?.RegionInfo.ISOCurrencySymbol;
        //NOTE: We always sync to SMART exchange irrepsecitve of the exchange given.
        var contract = new IBApi.Contract { Symbol = instrument.Ticker, SecType = InstrumentTypeToIBContractType(instrument.Type), Exchange = "SMART", Currency = currency };
        m_serviceHost.Client.ClientSocket.reqContractDetails(InstrumentIdBase, contract);
        Thread.Sleep(ContractDetailsRequestSleepTime);    //throttle requests to avoid exceeding the hard limit imposed by IB
      }
    }

    //NOTE: This method will only be effective after synchronizing the contract cache.
    public void UpdateInstrumentGroups()
    {

      //TODO
      m_logger.LogError("UpdateInstrumentGroups not implemented.");

    }

    public void RequestScannerParameters()
    {
      m_serviceHost.Client.ClientSocket.reqScannerParameters();
    }

    public void RequestContractDetails(Contract contract)
    {
      if (!m_contractRequestActive)
      {
        m_contractRequestActive = true;
        m_serviceHost.Client.ClientSocket.reqContractDetails(ContractDetailsId, contract);
      }
      else
        m_logger.LogError($"Failed to retrieve contract details {contract.ConId}, {contract.Symbol} - other contract request is active.");
    }

    public void RequestFundamentals(Contract contract, string reportType)
    {
      if (!m_fundamentalsRequestActive)
      {
        m_fundamentalsRequestActive = true;
        m_serviceHost.Client.ClientSocket.reqFundamentalData(FundamentalsId, contract, reportType, new List<TagValue>());
      }
    }


    //TODO: See how to clean up this method to use more strict types than freeform strings - use enums instead.

    public void RequestHistoricalData(Contract contract, string endDateTime, string durationString, string barSizeSetting, string whatToShow, int useRTH, bool keepUpToDate)
    {
      
      m_serviceHost.Client.ClientSocket.reqHistoricalData(m_historicalRequestCounter++ + HistoricalIdBase, contract, endDateTime, durationString, barSizeSetting, whatToShow, useRTH, 1, keepUpToDate, new List<TagValue>());
    }

    public void RequestRealTimeBars(Contract contract, int barSize, string whatToShow, bool useRTH)
    {
      
      //TODO: Add lookup table for real-time subscription with the associated reqId's used since it's used for the cancellation as well.

      m_serviceHost.Client.ClientSocket.reqRealTimeBars(m_historicalRequestCounter++ + HistoricalIdBase, contract, barSize, whatToShow, useRTH, new List<TagValue>());
    }
    
    public void CancelRealTimeBars(Contract contract, int barSize)
    {

      //TODO: Add lookup table for real-time subscription.

      m_serviceHost.Client.ClientSocket.cancelRealTimeBars(m_historicalRequestCounter + HistoricalIdBase);
    }

    //properties


    //methods
    public void HandleScannerParameters(string parametersXml)
    {
      m_logger.LogInformation($"Scanner parameters:\n{parametersXml}");
    }

    public void HandleRequestError(int requestId)
    {
      if (requestId == ContractDetailsId)
        m_contractRequestActive = false;
      else if (requestId == FundamentalsId)
        m_fundamentalsRequestActive = false;
    }

    public void HandleContractDetails(ContractDetailsMessage contractDetailsMessage)
    {
      //m_serviceHost.Cache.UpdateContract(contractDetailsMessage.ContractDetails.Contract);
      m_serviceHost.Cache.UpdateContractDetails(contractDetailsMessage.ContractDetails);
    }

    public void HandleContractDetailsEnd(ContractDetailsEndMessage contractDetailsEndMessage)
    {
      m_contractRequestActive = false;
    }

    public void HandleHistoricalData(HistoricalDataMessage historicalDataMessage)
    {

    }

    public void HandleHistoricalDataEnd(HistoricalDataEndMessage historicalDataEndMessage)
    {

    }

    public void HandleUpdateMktDepth(DeepBookMessage updateMktDepthMessage)
    {

    }

    public void HandleRealTimeBar(RealTimeBarMessage realTimeBarsMessage)
    {

    }

    public void HandleFundamentalsData(FundamentalsMessage fundamentalsMessage)
    {
      //TODO: Store fundamentals data.
      m_fundamentalsRequestActive = false;
    }

  }
}
