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
      IProgressDialog progress = m_dialogService.CreateProgressDialog("Synchronizing Contract Cache", m_logger);
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
      IProgressDialog progress = m_dialogService.CreateProgressDialog("Updating Instrument Groups", m_logger);
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
      //Word separators used to split the contract group names into words for matching
      public static string[] WordSeparators = new string[] { " ", "\t", ",", "-", "_", ".", "/", "\\" };

      public enum MatchesOn
      {
        None,
        Industry,
        Category,
        Subcategory
      }

      public InstrumentGroupValidation()
      {
        InstrumentGroup = null;
        Industry = string.Empty;
        IndustryFound = false;
        Category = string.Empty;
        CategoryFound = false;
        Subcategory = string.Empty;
        SubcategoryFound = false;
        IndustryWords = Array.Empty<string>();
        CategoryWords = Array.Empty<string>();
        SubcategoryWords = Array.Empty<string>();
      }

      public Contract Contract { get; set; }
      public InstrumentGroup? InstrumentGroup { get; set; }
      public string Industry { get; set; }
      public bool IndustryFound { get; set; }
      public string Category { get; set; }
      public bool CategoryFound { get; set; }
      public string Subcategory { get; set; }
      public bool SubcategoryFound { get; set; }
      public string[] IndustryWords { get; set; }
      public string[] CategoryWords { get; set; }
      public string[] SubcategoryWords { get; set; }
      private bool m_initWordLists = false;

      private string[] splitWords(string text)
      {
        return text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
      }

      /// <summary>
      /// Performs a match on the given instrument group and returns a weighted score based on the number of words that match
      /// between the contracts' industry, category and sub-category and the given IndustryGroup.
      /// </summary>
      public Tuple<double, MatchesOn, string> Match(InstrumentGroup instrumentGroup)
      {
        Tuple<double, MatchesOn, string> result = new Tuple<double, MatchesOn, string>(0.0, MatchesOn.None, string.Empty);
        double highestScore = 0.0;
        if (!m_initWordLists)
        {
          IndustryWords = splitWords(Industry);
          CategoryWords = splitWords(Category);
          SubcategoryWords = splitWords(Subcategory);
          m_initWordLists = true;
        }

        string[] nameWords = splitWords(instrumentGroup.Name);
        double score = matchWords(IndustryWords, nameWords);
        if (score > highestScore)
        {
          result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Industry, $"Matched name \"{instrumentGroup.Name}\" with industry \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
          highestScore = score;
        }

        score = matchWords(CategoryWords, nameWords);
        if (score > highestScore)
        {
          result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Category, $"Matched name \"{instrumentGroup.Name}\" with category \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
          highestScore = score;
        }

        score = matchWords(SubcategoryWords, nameWords);
        if (score > highestScore)
        {
          result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Subcategory, $"Matched name \"{instrumentGroup.Name}\" with sub-category \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
          highestScore = score;
        }

        foreach (var alternateName in instrumentGroup.AlternateNames)
        {
          nameWords = splitWords(alternateName);
          score = matchWords(IndustryWords, nameWords);
          if (score > highestScore)
          {
            result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Industry, $"Matched alternate name \"{alternateName}\" with industry \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
            highestScore = score;
          }

          score = matchWords(CategoryWords, nameWords);
          if (score > highestScore)
          {
            result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Category, $"Matched alternate name \"{alternateName}\" with category \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
            highestScore = score;
          }

          score = matchWords(SubcategoryWords, nameWords);
          if (score > highestScore)
          {
            result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Subcategory, $"Matched alternate name \"{alternateName}\" with sub-category \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
            highestScore = score;
          }
        }

        return result;
      }

      private double matchWords(string[] words1, string[] words2)
      {
        double score = 0.0;
        int count = words1.Length + words2.Length;

        foreach (var word1 in words1)
          foreach (var word2 in words2)
          {
            if (word1.Equals(word2, StringComparison.OrdinalIgnoreCase))
            {
              score += 1.0;
              break;
            }
          }

        return count > 0 ? score / count : 0.0;
      }
    }

    /// <summary>
    /// Strategy method to handle the fixing of missing instrument groups.
    /// </summary>
    public void ValidateInstrumentGroups()
    {
      List<InstrumentGroupValidation> definedContractGroups = new List<InstrumentGroupValidation>();

      IProgressDialog progress = m_dialogService.CreateProgressDialog("Validating Instrument Group Definitions", m_logger);
      progress.StatusMessage = "Accumulating industry class definitions from the InteractiveBrokers contract definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_instrumentService.Items.Count + m_instrumentGroupService.Items.Count;
      progress.ShowAsync();

      List<string> missingInstruments = new List<string>();
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
          missingInstruments.Add($"{instrument.Ticker}");
        else
        {
          //check that instrument group would be correct
          if (contract is ContractStock contractStock)
          {
            var contractGroup = definedContractGroups.FirstOrDefault((g) => g.Industry == contractStock.Industry && g.Category == contractStock.Category && g.Subcategory == contractStock.Subcategory);
            if (contractGroup == null)
            {
              contractGroup = new InstrumentGroupValidation { Contract = contract, Industry = contractStock.Industry, Category = contractStock.Category, Subcategory = contractStock.Subcategory };
              definedContractGroups.Add(contractGroup);
            }
          }
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      if (missingInstruments.Count > 0)
        using (progress.BeginScope($"Missing {missingInstruments.Count} contracts - run instrument analysis to correct this"))
          foreach (var missingInstrument in missingInstruments)
          {
            progress.LogWarning($"{missingInstrument}");
            if (progress.CancellationTokenSource.IsCancellationRequested) return;
          }
          
      List<Tuple<InstrumentGroup, string>> matchedInstrumentGroups = new List<Tuple<InstrumentGroup, string>>();
      List<InstrumentGroup> missingInstrumentGroups = new List<InstrumentGroup>();
      
      if (!progress.CancellationTokenSource.IsCancellationRequested)
      {
        progress.StatusMessage = "Analyzing instrument group definitions";
        foreach (var instrumentGroup in m_instrumentGroupService.Items)
        {
          var contractGroup = definedContractGroups.FirstOrDefault((g) => (!g.IndustryFound && instrumentGroup.Equals(g.Industry)) || (!g.CategoryFound && instrumentGroup.Equals(g.Category)) || (!g.SubcategoryFound && instrumentGroup.Equals(g.Subcategory)));

          if (contractGroup != null)
          {
            if (instrumentGroup.Equals(contractGroup.Industry))
            {
              contractGroup.IndustryFound = true;
              matchedInstrumentGroups.Add(new (instrumentGroup, $"\"{instrumentGroup.Name}\" matched with industry \"{contractGroup.Industry}\""));
            }

            if (instrumentGroup.Equals(contractGroup.Category))
            {
              contractGroup.CategoryFound = true;
              matchedInstrumentGroups.Add(new (instrumentGroup, $"\"{instrumentGroup.Name}\" matched with category \"{contractGroup.Category}\""));
            }

            if (instrumentGroup.Equals(contractGroup.Subcategory))
            {
              contractGroup.SubcategoryFound = true;
              matchedInstrumentGroups.Add(new (instrumentGroup, $"\"{instrumentGroup.Name}\" matched with sub-category \"{contractGroup.Subcategory}\""));
            }
          }
          else
            missingInstrumentGroups.Add(instrumentGroup);

          progress.Progress++;
          if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
        }
      }

      if (!progress.CancellationTokenSource.IsCancellationRequested)
      {
        using (progress.BeginScope($"Matched {matchedInstrumentGroups.Count} instrument groups"))
          foreach (var matchedInstrumentGroup in matchedInstrumentGroups)
          {
            progress.LogInformation(matchedInstrumentGroup.Item2);
            if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
          }
      }

      if (!progress.CancellationTokenSource.IsCancellationRequested && missingInstrumentGroups.Count > 0)
      {
        progress.StatusMessage = $"Searching for potential matches on {missingInstrumentGroups.Count} instrument groups";
        progress.Maximum += missingInstrumentGroups.Count * definedContractGroups.Count;

        foreach (var instrumentGroup in missingInstrumentGroups)
        {
          List<Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string>> matchScores = new List<Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string>>();
          foreach (var contractGroup in definedContractGroups)
          {
            Tuple<double, InstrumentGroupValidation.MatchesOn, string> result = contractGroup.Match(instrumentGroup);
            if (result.Item1 > 0.0) matchScores.Add(new Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string>(result.Item1, result.Item2, contractGroup, instrumentGroup, result.Item3));
            progress.Progress++;
            if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
          }

          if (matchScores.Count > 0)
          {
            Common.Utilities.Sort(matchScores, x => x.Item1);
            ILogCorrections corrections = progress.LogInformation($"{matchScores.Count} matches found for group \"{instrumentGroup.Name}\"");
            foreach (var match in matchScores)
              corrections.Add($"{match.Item5} - score {match.Item1:F3}", HandleFixMissingInstrumentGroup, match);
          }
          else
            progress.LogWarning($"No matches found for group \"{instrumentGroup.Name}\".");

          progress.Progress++;
          if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
        }
      }

      progress.Progress = progress.Maximum;
      progress.Complete = true;
    }

    public void HandleFixMissingInstrumentGroup(object? parameter)
    {
      if (parameter == null)
      {
        m_logger.LogError($"HandleMissingInstrumentGroup encountered null parameter.");
        return;
      }

      if (parameter is Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string> match)
      {
        string valueToAdd;
        switch (match.Item2)
        {
          case InstrumentGroupValidation.MatchesOn.Industry:
            valueToAdd = match.Item3.Industry;
            break;
          case InstrumentGroupValidation.MatchesOn.Category:
            valueToAdd = match.Item3.Category;
            break;
          case InstrumentGroupValidation.MatchesOn.Subcategory:
            valueToAdd = match.Item3.Subcategory;
            break;
          default:
            m_logger.LogError($"HandleMissingInstrumentGroup encountered incorrect match state.");
            return;
        }

        match.Item4.AlternateNames.Add(valueToAdd);
        m_database.UpdateInstrumentGroup(match.Item4);
        m_logger.LogInformation($"Added alternate name \"{valueToAdd}\" to instrument group \"{match.Item4.Name}\"");
      }
    }

    /// <summary>
    /// Checks the instrument definitions against the IB contract definitions to ensure they are in sync.
    /// </summary>
    public void ValidateInstruments()
    {
      IProgressDialog progress = m_dialogService.CreateProgressDialog("Validating Instruments", m_logger);
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
