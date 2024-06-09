using Microsoft.Extensions.Logging;
using System.Linq;
using TradeSharp.CoreUI.Common;
using TradeSharp.InteractiveBrokers.Messages;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Scans the Interactive Brokers for contracts that are not defined in the local cache, Interactive Brokers does not provide and API
  /// to just ask for the set of contracts that are available.
  /// NOTE: 
  ///  - This operation will run for a LONG time if made to long to scan for all possible contracts so we keep it fairly short. Using
  ///    a 25-millisecond wait between requests would take about 3.5 hours to process all possible combinations of 4-letter tickers.
  ///  - The scanner only returns minimal data even in the contracts, the full contract details will need to be requested separately
  ///    to fill in the fill contract and contract details tables.
  /// </summary>
  public class ContractScanner
  {
    //constants
    private const int MaxLength = 4;                    //Maximum length of the tickers generated to scan, this increases EXPONENTIALLY so lengths beyond 4 is probably not feasible within a reasonable time to download
    private const int PersistBlockInterval = 100;       //interval at which contracts would be persisted to the local cache
    private const int RefreshScanIntervalDays = 183;    //recheck an instrument every 6-months of so for existance
    public const int ScannerIdBase = 100000000;
    
    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;
    private IProgressDialog m_progress;
    private HashSet<string> m_contractsToScan;
    private HashSet<string> m_contractsNotProcessed;
    private HashSet<string> m_contractsDefined;
    private List<IBApi.Contract> m_contractsFound;
    private HashSet<string> m_contractsFoundSymbols;
    private HashSet<string> m_basicList;
    private List<ContractScannerMetaData> m_contractScannerMetaData;
    private int m_requestId;
    private int m_persistBlockIndex;

    //constructors
    public ContractScanner(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
      m_contractsToScan = new HashSet<string>();
      m_contractsNotProcessed = new HashSet<string>();
      m_contractsDefined = m_adapter.m_serviceHost.Cache.GetDefinedSymbols();
      m_contractScannerMetaData = m_adapter.m_serviceHost.Cache.GetContractScannerMetaData();
      m_contractsFound = new List<IBApi.Contract>();
      m_contractsFoundSymbols = new HashSet<string>();
      m_basicList = new HashSet<string>();
      for (char ticker = 'A'; ticker <= 'Z'; ticker++) m_basicList.Add(ticker.ToString());
      m_requestId = -1;
      m_persistBlockIndex = 0;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      m_progress = m_adapter.m_dialogService.CreateProgressDialog("Scanning for Contracts", m_adapter.m_logger);
      m_progress.StatusMessage = "Generating tickers to scan, please wait.";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_contractsToScan = generateTickersToScan(MaxLength);
      m_progress.ShowAsync();

      m_adapter.m_serviceHost.Client.Error += HandleError;

      m_progress.LogInformation($"Generated {m_contractsToScan.Count} potential tickers of up to length {MaxLength}...removing tickers already cached");

      //remove contracts not required for scanning based on the meta data
      m_progress.LogInformation($"{m_contractScannerMetaData.Count} tickers previously scanned in the meta-data cache, removing tickers already scanned within the last {RefreshScanIntervalDays} days");
      var now = DateTime.Now;
      foreach (var metaData in m_contractScannerMetaData)
        if ((now - metaData.LastScanDateTime).Days < RefreshScanIntervalDays) m_contractsToScan.Remove(metaData.Ticker);

      //remove contracts already defined in the cache
      foreach (string ticker in m_contractsDefined)
        m_contractsToScan.Remove(ticker);
      m_progress.LogInformation($"{m_contractsToScan.Count} contracts after removing {m_contractsDefined.Count} contracts already defined");

      //scan for contracts from Interactive Brokers
      m_progress.Maximum = m_contractsToScan.Count;   //update Maximum after removing contracts already defined and not required for scan
      int scannedCount = 0;
      if (!m_progress.CancellationTokenSource.IsCancellationRequested)
      {
        m_progress.LogInformation($"{m_contractsToScan.Count} contracts after removing contracts not within the refresh interval of {RefreshScanIntervalDays} days");
        m_progress.LogInformation($"Scanning remaining {m_contractsToScan.Count} tickers");

        m_adapter.m_serviceHost.Client.SymbolSamples += HandleSymbolSamples;

        foreach (string ticker in m_contractsToScan) m_contractsNotProcessed.Add(ticker);
        foreach (string ticker in m_contractsToScan)
        {
          //this can be a very long running process, check whether we have a connection and wait for the connection to come alive or exit
          //if the user cancels the operation
          bool disconnectDetected = false;
          while (!m_adapter.m_serviceHost.Client.ClientSocket.IsConnected() && !m_progress.CancellationTokenSource.IsCancellationRequested)
          {
            if (!disconnectDetected)             {
              m_progress.LogWarning($"Connection to Interactive Brokers lost at {DateTime.Now}, waiting for connection to be re-established");
              disconnectDetected = true;
            }
            Thread.Sleep(Constants.DisconnectedSleepInterval);
          }
          if (m_progress.CancellationTokenSource.IsCancellationRequested) break;

          //update progress
          m_progress.Progress++;

          //check whether we need to still process this ticker
          //NOTE: Two lists are kept of the tickers to process to avoid issues with the list being modified while being processed in HandleSymbolSamples.
          if (!m_contractsNotProcessed.Contains(ticker)) continue;

          //request the new symbols that might match the ticker
          m_requestId = ScannerIdBase + scannedCount;
          m_adapter.m_serviceHost.Client.ClientSocket.reqMatchingSymbols(m_requestId, ticker);
          if (m_progress.CancellationTokenSource.IsCancellationRequested) break;

          //persist specific generated ticker that it was scanned - we commit every ticker as this stuff takes ages to run
          var metaData = new ContractScannerMetaData { Ticker = ticker };
          metaData.LastScanDateTime = DateTime.Now;
          m_adapter.m_serviceHost.Cache.UpdateContractScannerMetaData(metaData);

          //wait for the response to be handled
          var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
          while (m_requestId != -1 && !cancellationTokenSource.Token.IsCancellationRequested)
            Thread.Sleep(Constants.IntraRequestSleep);   //wait till request has been handled

          //persist found contracts at specific intervals so ensure we don't lose them if the program crashes
          //NOTE: This can have a race condition on m_contractsFound.Count with the receiving thread and handler method (HandleSymbolSamples)
          //      so we rather capture the persistence in blocks using a block index than directly trying to base this persistence on the
          //      m_contarctsFound.Count variable.
          int blockIndex = m_contractsFound.Count / PersistBlockInterval;
          if (m_contractsFound.Count > 0 && blockIndex != m_persistBlockIndex)
          {
            m_adapter.m_serviceHost.Cache.BeginTransaction();
            int startIndex = m_persistBlockIndex * PersistBlockInterval;
            int endIndex = blockIndex * PersistBlockInterval - 1; //-1 since it will be included in the next block persistenace, also makes sure the index stays in bounds of the m_contractsFound list
            m_progress.LogInformation($"{m_contractsFound.Count} new contracts found, flushing contracts to the local cache from index {startIndex} to {endIndex}");
            for (int i = startIndex; i <= endIndex; i++)
            {
              m_adapter.m_serviceHost.Cache.UpdateContract(m_contractsFound[i]);
              metaData = new ContractScannerMetaData { Ticker = m_contractsFound[i].Symbol };
              metaData.LastScanDateTime = DateTime.Now;
              m_adapter.m_serviceHost.Cache.UpdateContractScannerMetaData(metaData);
            }
            m_adapter.m_serviceHost.Cache.CommitTransaction();
            m_persistBlockIndex++;
          }

          scannedCount++;
        }

        m_adapter.m_serviceHost.Client.Error -= HandleError;
        m_adapter.m_serviceHost.Client.SymbolSamples -= HandleSymbolSamples;
      }

      //store left over contracts to the cache
      //NOTE: We always update the cache even if the operation is cancelled so we don't have to rescan the same tickers again.
      int lastPersistedIndex = m_persistBlockIndex * PersistBlockInterval;
      if (m_contractsFound.Count > lastPersistedIndex)
      {
        m_progress.LogInformation($"{m_contractsFound.Count} new contracts found, flushing contracts to the local cache from index {lastPersistedIndex} to {m_contractsFound.Count}");
        m_adapter.m_serviceHost.Cache.BeginTransaction();
        for (int i = lastPersistedIndex; i < m_contractsFound.Count; i++)
        {
          m_adapter.m_serviceHost.Cache.UpdateContract(m_contractsFound[i]);
          var metaData = new ContractScannerMetaData { Ticker = m_contractsFound[i].Symbol };
          metaData.LastScanDateTime = DateTime.Now;
          m_adapter.m_serviceHost.Cache.UpdateContractScannerMetaData(metaData);
        }
        m_adapter.m_serviceHost.Cache.CommitTransaction();
      }
      m_progress.LogInformation($"Found {m_contractsFound.Count} new contracts after scanning {scannedCount} generated tickers (this number can be smaller, IB can return multiple contracts per ticker scanned), refreshing meta-data cache for scanner");

      if (!m_progress.CancellationTokenSource.IsCancellationRequested)
        m_progress.StatusMessage = $"Scan completed - found {m_contractsFound.Count} new contracts";
      else
        m_progress.StatusMessage = $"Scan cancelled - found {m_contractsFound.Count} new contracts";
      m_progress.Complete = true;
    }
    
    /// <summary>
    /// Recursively computes the combinations of tickers to scan for. 
    /// </summary>
    private HashSet<string> generateTickersToScan(int length)
    {
      if (length == 1) return m_basicList;

      HashSet<string> tickers = new HashSet<string>();
      HashSet<string> subTickers = generateTickersToScan(length - 1);
      foreach (var subTicker in subTickers) tickers.Add(subTicker);

      foreach (var basic in m_basicList)
        foreach (var sub in subTickers)
          tickers.Add(basic + sub);

      return tickers;
    }

    public void HandleSymbolSamples(SymbolSamplesMessage symbols)
    {
      //NOTE: If the contract data returned here has exchanges missing then the call above to reqMatchingSymbols is being done too
      //      quickly so the IB server does not fill these fields for whatever reason.
      foreach (var symbol in symbols.ContractDescriptions)
        if (symbol.Contract != null && 
           (symbol.Contract.SecType == Constants.ContractTypeStock ||
            symbol.Contract.SecType == Constants.ContractTypeETF ||
            symbol.Contract.SecType == Constants.ContractTypeIndex ||
            symbol.Contract.SecType == Constants.ContractTypeForex ||
            symbol.Contract.SecType == Constants.ContractTypeMutualFund ||
            symbol.Contract.SecType == Constants.ContractTypeCommodity)) //only keep the security types we're interested in (NOTE might have to change this later on)
        {
          if (!m_contractsDefined.Contains(symbol.Contract.Symbol) &&     //symbol not yet defined in the persistent store
              !m_contractsFoundSymbols.Contains(symbol.Contract.Symbol))  //contract not yet captured by this scan
          {
            m_contractsFound.Add(symbol.Contract);
            m_contractsFoundSymbols.Add(symbol.Contract.Symbol);
          }
          m_contractsNotProcessed.Remove(symbol.Contract.Symbol); //make sure we do not proress this ticker again
        }

      //signal to request code that response was handled
      m_requestId = -1;
    }

    public void HandleError(int id, int errorCode, string errorMessage, string unknown, Exception e)
    {
      if (m_progress != null)
      {
        if (e == null)
          m_progress.LogError($"Error {errorCode} - {errorMessage ?? ""}");
        else
          m_progress.LogError($"Error - {e.Message}");
      }
      else
      {
        if (e == null)
          m_adapter.m_logger.LogError($"Error {errorCode} - {errorMessage ?? ""}");
        else
          m_adapter.m_logger.LogError($"Error - {e.Message}");
      }
    }
  }
}
