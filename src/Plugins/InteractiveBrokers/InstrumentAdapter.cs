using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Common;
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
    public const int IntraRequestSleep = 25; //sleep time between requests in milliseconds - set limit to be under 50 requests per second https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#requests-limitations

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
    }

    //attributes
    private static InstrumentAdapter? s_instance;
    protected ServiceHost m_serviceHost;
    private ILogger m_logger;
    private IDialogService m_dialogService;
    private ICountryService m_countryService;
    private IExchangeService m_exchangeService;
    private IInstrumentGroupService m_instrumentGroupService;
    private IInstrumentService m_instrumentService;
    private IDatabase m_database;
    private bool m_contractRequestActive;
    private bool m_fundamentalsRequestActive;
    private int m_historicalRequestCounter = 0;
    private Dictionary<int, HistoricalDataRequest> m_activeHistoricalRequests;
    private Dictionary<int, Contract> m_activeRealTimeRequests;

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
      m_dialogService = serviceHost.Host.Services.GetRequiredService<IDialogService>();
      m_countryService = m_serviceHost.Host.Services.GetRequiredService<ICountryService>();
      m_exchangeService = m_serviceHost.Host.Services.GetRequiredService<IExchangeService>();
      m_instrumentGroupService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentGroupService>();
      m_instrumentService = m_serviceHost.Host.Services.GetRequiredService<IInstrumentService>();
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
      //TBD: The contract details table contains whether a specific contract is ETF/ETN/REIT etc.
      if (contractType == Constants.ContractTypeStock)
        //NOTE: The instrument might be an ETF, but we'll treat it as a stock for now.
        return InstrumentType.Stock;
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
      return InstrumentType.Stock;
    }

    public void SynchronizeContractCache()
    {
      m_serviceHost.Cache.Clear();    //ensure cache starts fresh after the update
      IProgressDialog progress = m_dialogService.ShowProgressDialog("Synchronizing Contract Cache", m_logger);
      progress.StatusMessage = "Synchronizing Contract Cache from Instrument Definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_instrumentService.Items.Count;
      progress.ShowAsync();
      m_contractRequestActive = true;

      foreach (var instrument in m_instrumentService.Items)
      {
        var currency = "USD";
        var exchange = m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
        var country = m_countryService.Items.FirstOrDefault(c => c.Id == exchange?.CountryId);
        if (country != null) currency = country?.CountryInfo?.RegionInfo.ISOCurrencySymbol;
        //NOTE: We always sync to SMART exchange irrepsecitve of the exchange given.
        var contract = new IBApi.Contract { Symbol = instrument.Ticker, SecType = InstrumentTypeToIBContractType(instrument.Type), Exchange = Constants.DefaultExchange, Currency = currency };
        m_serviceHost.Client.ClientSocket.reqContractDetails(InstrumentIdBase, contract);
        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
        Thread.Sleep(IntraRequestSleep);    //throttle requests to avoid exceeding the hard limit imposed by IB
      }

      progress.Complete = true;
    }

    //NOTE: This method will only be effective after synchronizing the contract cache with IB's contract definitions
    public void UpdateInstrumentGroups()
    {
      IProgressDialog progress = m_dialogService.ShowProgressDialog("Updating Instrument Groups", m_logger);
      progress.StatusMessage = "Updating Instrument Groups from Contract Cache";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_instrumentGroupService.Items.Count;
      progress.ShowAsync();

      //start updating the instrument groups
      foreach (var instrumentGroup in m_instrumentGroupService.Items)
      {
        int instrumentsUpdated = 0;
        foreach (var instrument in m_instrumentService.Items)
        {
          var contract = m_serviceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);
          if (contract is ContractStock contractStock)
          {
            if (instrumentGroup.Equals(contractStock.Industry) && !instrumentGroup.SearchTickers.Contains(instrument.Ticker))
            {
              m_database.CreateInstrumentGroupInstrument(instrumentGroup.Id, instrument.Ticker);
              instrumentsUpdated++;
            }
          }
          else
            if (contract != null) progress.LogError($"Contract {contract.Symbol}, {contract.SecType} is not supported.");

        }
        progress.LogInformation($"Updated {instrumentsUpdated} instruments for group {instrumentGroup.Name}");
        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
      }

      progress.Complete = true;
    }

    /// <summary>
    /// Checks the instrument groups against the IB contract definitions to ensure they are in sync.
    /// </summary>
    private class InstrumentGroupValidation
    {
      public InstrumentGroupValidation()
      {
        Industry = string.Empty;
        IndustryFound = false;
        Category = string.Empty;
        CategoryFound = false;
        Subcategory = string.Empty;
        SubcategoryFound = false;
      }

      public string Industry { get; set; }
      public bool IndustryFound { get; set; }
      public string Category { get; set; }
      public bool CategoryFound { get; set; }
      public string Subcategory { get; set; }
      public bool SubcategoryFound { get; set; }
    }

    public void ValidateInstrumentGroups()
    {
      List<InstrumentGroupValidation> definedContractGroups = new List<InstrumentGroupValidation>();

      IProgressDialog progress = m_dialogService.ShowProgressDialog("Validating Instrument Group Definitions", m_logger);
      progress.StatusMessage = "Accumulating industry class definitions from the InteractiveBrokers contract definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_instrumentService.Items.Count + m_instrumentGroupService.Items.Count;
      progress.ShowAsync();

      foreach (var instrument in m_instrumentService.Items)
      {
        var contract = m_serviceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

        if (contract == null)
          foreach (var altTicker in instrument.AlternateTickers)
          {
            contract = m_serviceHost.Cache.GetContract(altTicker, Constants.DefaultExchange);
            if (contract != null) break;
          }

        if (contract == null)
          progress.LogWarning($"Contract definition for instrument \"{instrument.Ticker}\" not found - skipping.");
        else
        {
          //check that instrument group would be correct
          if (contract is ContractStock contractStock)
          {
            var contractGroup = definedContractGroups.FirstOrDefault((g) => g.Industry == contractStock.Industry && g.Category == contractStock.Category && g.Subcategory == contractStock.Subcategory);
            if (contractGroup == null)
            {
              contractGroup = new InstrumentGroupValidation { Industry = contractStock.Industry, Category = contractStock.Category, Subcategory = contractStock.Subcategory };
              definedContractGroups.Add(contractGroup);
            }
          }
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      List<InstrumentGroup> missingInstrumentGroups = new List<InstrumentGroup>();
      progress.StatusMessage = "Analyzing instrument group definitions";
      foreach (var instrumentGroup in m_instrumentGroupService.Items)
      {
        var contractGroup = definedContractGroups.FirstOrDefault((g) => (!g.IndustryFound && instrumentGroup.Equals(g.Industry)) || (!g.CategoryFound && instrumentGroup.Equals(g.Category)) || (!g.SubcategoryFound && instrumentGroup.Equals(g.Subcategory)));

        if (contractGroup != null)
        {
          if (instrumentGroup.Equals(contractGroup.Industry))
            contractGroup.IndustryFound = true;
          if (instrumentGroup.Equals(contractGroup.Category))
            contractGroup.CategoryFound = true;
          if (instrumentGroup.Equals(contractGroup.Subcategory))
            contractGroup.SubcategoryFound = true;
        }
        else
          missingInstrumentGroups.Add(instrumentGroup);

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      progress.StatusMessage = $"Searching for potential matches on {missingInstrumentGroups.Count} instrument groups";
      progress.Maximum += missingInstrumentGroups.Count;
      foreach (var instrumentGroup in missingInstrumentGroups)
      {

        progress.LogError($"Could not find matching group for {instrumentGroup.Name}", HandleMissingInstrumentGroup, "Test string"); 
        

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      progress.Progress = progress.Maximum;
      progress.Complete = true;
    }

    public void HandleMissingInstrumentGroup(object? parameter)
    {
      m_logger.LogInformation($"HandleMissingInstrumentGroup called with parameter {parameter}");
    }

    /// <summary>
    /// Checks the instrument definitions against the IB contract definitions to ensure they are in sync.
    /// </summary>
    public void ValidateInstruments()
    {
      IProgressDialog progress = m_dialogService.ShowProgressDialog("Validating Instruments", m_logger);
      progress.StatusMessage = "Validating Instrument definitions against the Contract Cache definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_instrumentService.Items.Count;
      progress.ShowAsync();

      foreach (var instrument in m_instrumentService.Items)
      {
        using (progress.BeginScope($"Validating {instrument.Ticker}"))
        {
          var contract = m_serviceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

          if (contract == null)
            foreach (var altTicker in instrument.AlternateTickers)
            {
              contract = m_serviceHost.Cache.GetContract(altTicker, Constants.DefaultExchange);
              if (contract != null) progress.LogInformation($"Will not match on primary ticker but on alternate ticker {altTicker}.");
            }

          if (contract == null)
            progress.LogError($"Contract definition not found.");
          else
          {
            //check that instrument group would be correct
            if (contract is ContractStock contractStock)
            {
              if (contractStock.StockType == Constants.StockTypeCommon)
              {
                if (contractStock.Industry != string.Empty)
                {
                  var instrumentGroup = m_instrumentGroupService.Items.FirstOrDefault(g => g.Equals(contractStock.Subcategory));
                  if (instrumentGroup == null)
                    progress.LogError($"Instrument group for {contractStock.Industry}->{contractStock.Category}->{contractStock.Subcategory} not found.");
                }
                else
                  progress.LogWarning($"Stock contract {contractStock.Symbol} has no associated Industry set.");
              }
            }
            else
              progress.LogError($"Contract {contract.Symbol}, {contract.SecType} is not supported.");
          }
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
      }

      progress.Complete = true;
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

    public void RequestHistoricalData(Contract contract, DateTime startDateTime, DateTime endDateTime, Resolution resolution)
    {
      //duration string requires a valid date range
      if (startDateTime >= endDateTime)
      {
        m_logger.LogError($"Invalid date range (start: {startDateTime}, end: {endDateTime}) for historical data request on ticker {contract.Symbol}.");
        return;
      }

      string barSizeSetting = Constants.BarSize1Day;
      string durationString;
      switch (resolution)
      {
        case Resolution.Minute:
          barSizeSetting = Constants.BarSize1Min;
          TimeSpan duration = endDateTime - startDateTime;
          durationString = $"{(long)Math.Ceiling(duration.TotalDays)} {Constants.DurationDays}";
          break;
        case Resolution.Hour:
          barSizeSetting = Constants.BarSize1Hour;
          duration = endDateTime - startDateTime;
          durationString = $"{(long)Math.Ceiling(duration.TotalDays)} {Constants.DurationDays}";
          break;
        case Resolution.Day:
          barSizeSetting = Constants.BarSize1Day;
          duration = endDateTime - startDateTime;
          durationString = $"{(long)Math.Ceiling(duration.TotalDays)} {Constants.DurationDays}";
          break;
        case Resolution.Week:
          barSizeSetting = Constants.BarSize1Week;
          duration = endDateTime - startDateTime;          

          if (duration.TotalDays < 7)
          {
            m_logger.LogError($"Invalid date range (start: {startDateTime}, end: {endDateTime}) for weekly historical data request on ticker {contract.Symbol}.");
            return;
          }
          
          durationString = $"{(long)Math.Ceiling(duration.TotalDays / 7)} {Constants.DurationWeeks}";
          break;
        case Resolution.Month:
          barSizeSetting = Constants.BarSize1Month;
          duration = endDateTime - startDateTime;

          if (duration.TotalDays < 365)
            durationString = $"{(long)Math.Ceiling(duration.TotalDays)} {Constants.DurationMonths}";
          else
            durationString = $"{(long)Math.Ceiling(duration.TotalDays / 365)} {Constants.DurationYears}";
          break;
        default:
          m_logger.LogError($"Unsupported resolution requested {resolution.ToString()} on ticker {contract.Symbol} for historical data.");
          return;   //intentional exit on error state
      }

      int reqId = m_historicalRequestCounter + HistoricalIdBase;
      m_activeHistoricalRequests[reqId] = new HistoricalDataRequest(reqId, contract, resolution, startDateTime, endDateTime);
      m_serviceHost.Client.ClientSocket.reqHistoricalData(reqId, contract, endDateTime.ToString(), durationString, barSizeSetting, "TRADES" /*whatToShow*/ , 1 /*useRTH*/, 1 /*formatDate - https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#hist-format-date*/, false /*keepUpToDate*/, new List<TagValue>());
    }

    /// <summary>
    /// Request real-time data for the given instrument, IB looks like it will send info every 5-seconds (https://ibkrcampus.com/ibkr-api-page/twsapi-doc/#live-bars) 
    /// Returns the request Id used to cancal the data request.
    /// </summary>
    public int RequestRealTimeBars(Contract contract, int barSize, string whatToShow, bool useRTH)
    {
      int reqId = m_historicalRequestCounter++ + HistoricalIdBase;
      m_activeRealTimeRequests[reqId] = contract;
      //NOTE: barSize is currently ignored (14 April 2024)
      m_serviceHost.Client.ClientSocket.reqRealTimeBars(reqId, contract, barSize, whatToShow, useRTH, new List<TagValue>());
      return reqId;
    }

    public bool CancelRealTimeBars(int requestId)
    {
      bool result = m_activeRealTimeRequests.Remove(requestId);
      if (result)
        m_serviceHost.Client.ClientSocket.cancelRealTimeBars(requestId);
      else
        m_logger.LogError($"Failed to cancel real-time bars request with request Id {requestId}.");
      return result;
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
      if (m_activeHistoricalRequests.TryGetValue(historicalDataMessage.RequestId, out HistoricalDataRequest? request))
      {
        if (DateTime.TryParse(historicalDataMessage.Date, out DateTime dateTime))
        {
          //NOTE: Requests must be done as whole units of days, week, months or years so we need to make sure that the response date is within the specific requested date range.
          if (dateTime >= request.FromDateTime && dateTime <= request.ToDateTime)
            m_database.UpdateData(Constants.DefaultName, request.Contract.Symbol, request.Resolution, dateTime, historicalDataMessage.Open, historicalDataMessage.High, historicalDataMessage.Low, historicalDataMessage.Close, historicalDataMessage.Volume);
        }
        else
          m_logger.LogError($"Failed to parse date {historicalDataMessage.Date} for historical data request entry for reqId {historicalDataMessage.RequestId}");
      }
      else
        m_logger.LogError($"Failed to find historical data request entry for reqId {historicalDataMessage.RequestId}");
    }

    public void HandleHistoricalDataEnd(HistoricalDataEndMessage historicalDataEndMessage)
    {
      m_activeHistoricalRequests.Remove(historicalDataEndMessage.RequestId);
    }

    public void HandleUpdateMktDepth(DeepBookMessage updateMktDepthMessage)
    {

    }

    public void HandleRealTimeBar(RealTimeBarMessage realTimeBarsMessage)
    {

      //TODO:
      // - Update data for the real-time bar update in the database tables. How would you do this? Update all resolutions even if it means partial bars vs update only requested resolution update?
      // - Add some event that could be raised to fire the bar update.

    }

    public void HandleFundamentalsData(FundamentalsMessage fundamentalsMessage)
    {
      m_fundamentalsRequestActive = false;
    }

  }
}
