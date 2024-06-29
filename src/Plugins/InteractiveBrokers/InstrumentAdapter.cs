using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeSharp.InteractiveBrokers.Messages;
using IBApi;
using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// Adapter to handle the retrieval of instrument definitions, their associated historical data and real-time data from Interactive Brokers.
  /// </summary>
  public class InstrumentAdapter
  {
    //constants
    public const int InstrumentIdBase = 20000000;
    public const int HistoricalIdBase = 30000000;
    public const int ContractDetailsId = InstrumentIdBase + 1;
    public const int FundamentalsId = InstrumentIdBase + 2;
   
    //enums


    //types
    /// <summary>
    /// Meta-data held for historical data request.
    /// </summary>
    private class HistoricalDataRequest
    {
      public HistoricalDataRequest(int requestId, Contract contract, Resolution resolution, DateTime fromDateTime, DateTime toDateTime)
      {
        RequestId = requestId;
        Contract = contract;
        Resolution = resolution;
        FromDateTime = fromDateTime;
        ToDateTime = toDateTime;
      }

      public int RequestId { get; set; }
      public Contract Contract { get; set; }
      public Resolution Resolution { get; set; }
      public DateTime FromDateTime { get; set; }
      public DateTime ToDateTime { get; set; }
      public long Count { get; set; }   //used internally to count the numnber of bars retrieved for this request
    }

    //attributes
    private static InstrumentAdapter? s_instance;
    internal ServiceHost m_serviceHost;
    internal ILogger m_logger;
    internal IDatabase m_database;
    internal bool m_contractRequestActive;
    internal bool m_fundamentalsRequestActive;
    internal int m_historicalRequestCounter = 0;
    private Dictionary<int, HistoricalDataRequest> m_activeHistoricalRequests;
    internal Dictionary<int, Contract> m_activeRealTimeRequests;
    private HistoricalDataRequest? m_lastHistoricalDataRequest;

    //constructors
    public static InstrumentAdapter GetInstance(ServiceHost serviceHost)
    {
      if (s_instance == null) s_instance = new InstrumentAdapter(serviceHost);
      return s_instance;
    }

    protected InstrumentAdapter(ServiceHost serviceHost)
    {
      m_serviceHost = serviceHost;
      m_logger = serviceHost.Host.Services.GetRequiredService<ILogger<InstrumentAdapter>>();
      m_database = m_serviceHost.Host.Services.GetRequiredService<IDatabase>();
      m_activeHistoricalRequests = new Dictionary<int, HistoricalDataRequest>();
      m_activeRealTimeRequests = new Dictionary<int, Contract>();
      m_contractRequestActive = false;
      m_fundamentalsRequestActive = false;
    }

    //finalizers


    //interface implementations
    public string InstrumentTypeToIBContractType(InstrumentType instrumentType)
    {
      //TBD: The contract details table contains whether a specific contract is ETF/ETN/REIT etc.
      switch (instrumentType)
      {
        case InstrumentType.ETF:    //etf's have entries under the ContractDetails in the StockType
        case InstrumentType.Stock:
          return Constants.ContractTypeStock;
        case InstrumentType.Option:
          return Constants.ContractTypeOption;
        case InstrumentType.Future:
          return Constants.ContractTypeFuture;
        case InstrumentType.Index:
          return Constants.ContractTypeIndex;
        case InstrumentType.Forex:
          return Constants.ContractTypeForex;
        case InstrumentType.MutualFund:
          return Constants.ContractTypeMutualFund;
      }

      return Constants.ContractTypeStock;
    }

    public InstrumentType IBContractTypeToInstrumentType(string contractType)
    {
      //TBD: Still need to support REIT/ETN.
      if (contractType == Constants.ContractTypeStock)
        return InstrumentType.Stock;
      if (contractType == Constants.ContractTypeETF)
        return InstrumentType.ETF;
      if (contractType == Constants.ContractTypeOption)
        return InstrumentType.Option;
      if (contractType == Constants.ContractTypeFuture)
        return InstrumentType.Future;
      if (contractType == Constants.ContractTypeIndex)
        return InstrumentType.Index;
      if (contractType == Constants.ContractTypeForex)
        return InstrumentType.Forex;
      if (contractType == Constants.ContractTypeMutualFund)
        return InstrumentType.MutualFund;

      m_logger.LogWarning($"Contract to Instrument type conversion does not support conversion of IB type \"{contractType}\"");
      return InstrumentType.Stock;
    }


    public Instrument? From(Contract contract)
    {
      return m_serviceHost.InstrumentService.Items.FirstOrDefault(x => x.Ticker == contract.Symbol.ToUpper());
    }

    public Instrument? From(ContractDetails contractDetails)
    {
      return m_serviceHost.InstrumentService.Items.FirstOrDefault(x => x.Ticker == contractDetails.UnderSymbol.ToUpper());
    }

    public Contract? From(Instrument instrument)
    {
      Exchange? exchange = m_serviceHost.ExchangeService.Items.FirstOrDefault((e) => e.Id == instrument.PrimaryExchangeId);
      if (exchange == null)
      {
        m_logger.LogError($"Failed to find exchange for instrument {instrument.Ticker}.");
        return null;
      }

      Contract? contract = m_serviceHost.Cache.GetContract(instrument.Ticker, exchange.Name);
      if (contract == null)
      {
        foreach (var ticker in instrument.AlternateTickers)
        {
          contract = m_serviceHost.Cache.GetContract(ticker, exchange.Name);
          if (contract != null) break;
        }
      }

      return contract;
    }

    public void ScanForContracts()
    {
      Commands.ContractScanner contractScanner = new Commands.ContractScanner(m_serviceHost);
      contractScanner.Run();
    }

    public void SynchronizeContractCache()
    {
      Commands.SynchronizeContractCache synchronizeContractCache = new Commands.SynchronizeContractCache(m_serviceHost);
      synchronizeContractCache.Run();
    }

    public void DefineSupportedExchanges()
    {
      Commands.DefineSupportedExchanges command = new Commands.DefineSupportedExchanges(m_serviceHost);
      command.Run();
    }

    public void ValidateInstrumentGroups()
    {
      Commands.ValidateInstrumentGroups command = new Commands.ValidateInstrumentGroups(m_serviceHost);
      command.Run();
    }

    public void CopyIBClassesToInstrumentGroups()
    {
      Commands.CopyIBClassesToInstrumentGroups command = new Commands.CopyIBClassesToInstrumentGroups(m_serviceHost);
      command.Run();
    }

    public void ValidateInstruments()
    {
      Commands.ValidateInstruments command = new Commands.ValidateInstruments(m_serviceHost);
      command.Run();
    }

    public void CopyContractsToInstruments() 
    {
      Commands.CopyContractsToInstruments command = new Commands.CopyContractsToInstruments(m_serviceHost);
      command.Run();
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

    //https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#requesting-historical-bars
    public void RequestHistoricalData(Contract contract, DateTime startDateTime, DateTime endDateTime, Resolution resolution)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");
      ////duration string requires a valid date range
      //if (startDateTime >= endDateTime)
      //{
      //  m_logger.LogError($"Invalid date range (start: {startDateTime}, end: {endDateTime}) for historical data request on ticker {contract.Symbol}.");
      //  return;
      //}

      ////compute the end date/time as UTC - IB requires the format yyyymmdd hh:mm:ss xx/xxxx where yyyymmdd and xx/xxxx are optional. E.g.: 20031126 15:59:00 US/Eastern OR
      ////yyyymmddd-hh:mm:ss time is in UTC - valid IB time zones defined here - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#contract-details
      //string endDateTimeStr = endDateTime.ToUniversalTime().ToString("yyyyMMdd-HH:mm:ss");

      ////compute the duration string based on the resolution and start date
      //string barSizeSetting = Constants.BarSize1Day;
      //string durationString;
      //switch (resolution)
      //{
      //  case Resolution.Minute:
      //    barSizeSetting = Constants.BarSize1Min;
      //    TimeSpan duration = endDateTime - startDateTime;
      //    durationString = computeDuration(duration);
      //    break;
      //  case Resolution.Hour:
      //    barSizeSetting = Constants.BarSize1Hour;
      //    duration = endDateTime - startDateTime;
      //    durationString = computeDuration(duration);
      //    break;
      //  case Resolution.Day:
      //    barSizeSetting = Constants.BarSize1Day;
      //    duration = endDateTime - startDateTime;
      //    durationString = computeDuration(duration);
      //    break;
      //  case Resolution.Week:
      //    barSizeSetting = Constants.BarSize1Week;
      //    duration = endDateTime - startDateTime;
      //    durationString = computeDuration(duration);
      //    break;
      //  case Resolution.Month:
      //    barSizeSetting = Constants.BarSize1Month;
      //    duration = endDateTime - startDateTime;
      //    durationString = computeDuration(duration);
      //    break;
      //  default:
      //    m_logger.LogError($"Unsupported resolution requested {resolution.ToString()} on ticker {contract.Symbol} for historical data.");
      //    return;   //intentional exit on error state
      //}

      //int reqId = m_historicalRequestCounter++ + HistoricalIdBase;
      //m_activeHistoricalRequests[reqId] = new HistoricalDataRequest(reqId, contract, resolution, startDateTime, endDateTime);
      //m_serviceHost.Client.ClientSocket.reqHistoricalData(reqId, contract, endDateTimeStr, durationString, barSizeSetting, "TRADES" /*whatToShow*/ , 1 /*useRTH*/, 1 /*formatDate as UTC - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#hist-format-date*/, false /*keepUpToDate*/, new List<TagValue>());
    }

    /// <summary>
    /// Request real-time data for the given instrument, IB looks like it will send info every 5-seconds (https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#live-bars) 
    /// Returns the request Id used to cancal the data request.
    /// </summary>
    public int RequestRealTimeBars(Contract contract, int barSize, string whatToShow, bool useRTH)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");

      //int reqId = m_historicalRequestCounter++ + HistoricalIdBase;
      //m_activeRealTimeRequests[reqId] = contract;
      ////NOTE: barSize is currently ignored (14 April 2024)
      //m_serviceHost.Client.ClientSocket.reqRealTimeBars(reqId, contract, barSize, whatToShow, useRTH, new List<TagValue>());
      //return reqId;
    }

    public bool CancelRealTimeBars(int requestId)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");

      //bool result = m_activeRealTimeRequests.Remove(requestId);
      //if (result)
      //  m_serviceHost.Client.ClientSocket.cancelRealTimeBars(requestId);
      //else
      //  m_logger.LogError($"Failed to cancel real-time bars request with request Id {requestId}.");
      //return result;
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
      m_serviceHost.Cache.UpdateContract(contractDetailsMessage.ContractDetails.Contract);
      m_serviceHost.Cache.UpdateContractDetails(contractDetailsMessage.ContractDetails);
    }

    public void HandleContractDetailsEnd(ContractDetailsEndMessage contractDetailsEndMessage)
    {
      m_contractRequestActive = false;
    }

    public void HandleHistoricalData(HistoricalDataMessage historicalDataMessage)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");

      ////retrieve the historical request data
      //HistoricalDataRequest? request = m_lastHistoricalDataRequest;
      //if (request == null || request.RequestId != historicalDataMessage.RequestId)
      //  request = m_activeHistoricalRequests.TryGetValue(historicalDataMessage.RequestId, out request) ? request : null;

      ////process the data response based on the request
      //if (request != null)
      //{
      //  m_lastHistoricalDataRequest = request;
      //  //NOTE: Historical data requests must be done in UTC since we assume it is in UTC here.
      //  string date = Regex.Replace(historicalDataMessage.Date, @"\s+", " ").Trim();    //response sometimes has extra whitespace spaces that TryParseExact fails to parse
      //  if (DateTime.TryParseExact(date, Constants.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dateTime))
      //  {
      //    //NOTES:
      //    // - Requests must be done as whole units of days, week, months or years so we need to make sure that the response date is within the specific requested date range.
      //    // - Volume returned is the numner of trades and not the volume in terms of total stocks that changed hands - this is NOT a good indicator of volume.
      //    if (dateTime >= request.FromDateTime && dateTime <= request.ToDateTime)
      //    {
      //      m_database.UpdateData(Constants.DefaultName, request.Contract.Symbol, request.Resolution, dateTime, historicalDataMessage.Open, historicalDataMessage.High, historicalDataMessage.Low, historicalDataMessage.Close, historicalDataMessage.Volume);
      //      m_lastHistoricalDataRequest.Count++;  //NOTE: This is correct, the HistoricalDataMessage is sent for each bar retrieved (it has a count field which contains the number of bars retrieved)
      //    }
      //  }
      //  else
      //    m_logger.LogError($"Failed to parse date {date} for historical data request entry for reqId {historicalDataMessage.RequestId}");
      //}
      //else
      //  m_logger.LogError($"Failed to find historical data request entry for reqId {historicalDataMessage.RequestId}");
    }

    public void HandleHistoricalDataEnd(HistoricalDataEndMessage historicalDataEndMessage)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");

      //if (m_activeHistoricalRequests.TryGetValue(historicalDataEndMessage.RequestId, out HistoricalDataRequest? historicalDataRequest))
      //  m_serviceHost.DataProviderPlugin.RaiseDataDownloadComplete(historicalDataRequest.Contract, historicalDataRequest.Resolution, historicalDataRequest.Count);
      //else
      //  m_logger.LogError($"Failed to find historical data request - {historicalDataEndMessage.RequestId}");

      //m_activeHistoricalRequests.Remove(historicalDataEndMessage.RequestId);
      //m_lastHistoricalDataRequest = null;
    }

    public void HandleUpdateMktDepth(DeepBookMessage updateMktDepthMessage)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");
    }

    public void HandleRealTimeBar(RealTimeBarMessage realTimeBarsMessage)
    {
      throw new NotImplementedException("InteractiveBrokers historical data not supported due to limitations from broker.");
    }

    public void HandleFundamentalsData(FundamentalsMessage fundamentalsMessage)
    {
      m_fundamentalsRequestActive = false;
    }

    /// <summary>
    /// Computes the duration string for the historical data request based on the total duration.
    /// https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#hist-step-size
    /// </summary>
    private string computeDuration(TimeSpan duration)
    {
      if (duration.TotalHours < 24)
      {
        return $"{Math.Ceiling(duration.TotalDays)} {Constants.DurationDays}";
      }
      else if (duration.TotalDays < 365)
      {
        return $"{Math.Ceiling(duration.TotalDays)} {Constants.DurationDays}";
      }
      else
      {
        return $"{Math.Ceiling(duration.TotalDays / 365)} {Constants.DurationYears}";
      }
    }
  }
}
