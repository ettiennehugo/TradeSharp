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
    private const int MaxLength = 4;
    private const int PersistInterval = 100;
    public const int ScannerIdBase = 100000000;

    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;
    private IProgressDialog m_progress;
    private List<string> m_contractsToScan;
    private List<string> m_contractsNotProcessed;
    private List<string> m_contractsDefined;
    private List<IBApi.Contract> m_contractsFound;
    private List<string> m_basicList;
    private int m_requestId;

    //constructors
    public ContractScanner(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
      m_contractsToScan = new List<string>();
      m_contractsNotProcessed = new List<string>();
      m_contractsDefined = m_adapter.m_serviceHost.Cache.GetDefinedSymbols();
      m_contractsFound = new List<IBApi.Contract>();
      m_basicList = new List<string>();
      for (char ticker = 'A'; ticker <= 'Z'; ticker++) m_basicList.Add(ticker.ToString());
      m_requestId = -1;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      m_progress = m_adapter.m_dialogService.CreateProgressDialog("Scanning for Contracts", m_adapter.m_logger);
      m_progress.StatusMessage = "Scanning for Contracts";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_contractsToScan = generateTickersToScan(MaxLength);
      m_progress.Maximum = m_contractsToScan.Count;
      m_progress.ShowAsync();

      m_progress.LogInformation($"Generated {m_contractsToScan.Count} potential ticker combinations up to length {MaxLength}");

      foreach (string ticker in m_contractsDefined)
      {
        m_progress.Progress++;
        m_contractsToScan.Remove(ticker);
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;
      }

      int foundPersistedIndex = 0;
      if (!m_progress.CancellationTokenSource.IsCancellationRequested)
      {
        m_progress.LogInformation($"{m_contractsToScan.Count} tickers left to scan after removing contracts already defined");

        m_adapter.m_serviceHost.Client.SymbolSamples += HandleSymbolSamples;

        int count = 0;
        m_contractsNotProcessed.AddRange(m_contractsToScan);
        foreach (string ticker in m_contractsToScan)
        {
          m_progress.Progress++;

          //check whether we need to still process this ticker
          //NOTE: Two lists are kept of the tickers to process to avoid issues with the list being modified while being processed in HandleSymbolSamples.
          if (!m_contractsNotProcessed.Contains(ticker)) continue;

          //request the new symbols that might match the ticker
          m_requestId = ScannerIdBase + count;
          m_adapter.m_serviceHost.Client.ClientSocket.reqMatchingSymbols(m_requestId, ticker);
          if (m_progress.CancellationTokenSource.IsCancellationRequested) break;

          //wait for the response to be handled
          var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
          while (m_requestId != -1 && !cancellationTokenSource.Token.IsCancellationRequested)
            Thread.Sleep(InstrumentAdapter.IntraRequestSleep);   //throttle requests to avoid exceeding the hard limit imposed by IB

          //persist found contracts at specific intervals so ensure we don't lose them if the program crashes
          if (m_contractsFound.Count > 0 && m_contractsFound.Count % PersistInterval == 0)
          {
            m_progress.LogInformation($"{m_contractsFound.Count} new contracts found, flushing contracts to the local cache");
            for (int i = foundPersistedIndex; i < m_contractsFound.Count; i++)
              m_adapter.m_serviceHost.Cache.UpdateContract(m_contractsFound[i]);
            foundPersistedIndex = m_contractsFound.Count;
          }

          count++;
        }

        m_adapter.m_serviceHost.Client.SymbolSamples -= HandleSymbolSamples;
      }

      //store left over contracts to the cache
      if (m_contractsFound.Count != foundPersistedIndex && !m_progress.CancellationTokenSource.IsCancellationRequested)
      {
        m_progress.LogInformation($"{m_contractsFound.Count} new contracts found, flushing contracts to the local cache");
        for (int i = foundPersistedIndex; i < m_contractsFound.Count; i++)
          m_adapter.m_serviceHost.Cache.UpdateContract(m_contractsFound[i]);
      }

      if (m_contractsFound.Count == 0)
        m_progress.LogInformation("No new contracts were found");

      m_progress.Complete = true;
    }
    
    /// <summary>
    /// Recursively computes the combinations of tickers to scan for. 
    /// </summary>
    private List<string> generateTickersToScan(int length)
    {
      if (length == 1) return m_basicList;

      List<string> tickers = new List<string>();
      List<string> subTickers = generateTickersToScan(length - 1);

      tickers.AddRange(subTickers);

      foreach (var basic in m_basicList)
        foreach (var sub in subTickers)
          tickers.Add(basic + sub);

      return tickers;
    }

    public void HandleSymbolSamples(SymbolSamplesMessage symbols)
    {
      //NOTE: The contract data returned here contains only the basic information in order to retrieve the rest of the
      //      contract data and contract details.
      foreach (var symbol in symbols.ContractDescriptions)
        if (symbol.Contract != null && m_contractsNotProcessed.Remove(symbol.Contract.Symbol))
          m_contractsFound.Add(symbol.Contract);

      //signal to request code that response was handled
      m_requestId = -1;
    }
  }
}
