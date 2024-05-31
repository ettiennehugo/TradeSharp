using TradeSharp.CoreUI.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Synchronize the local contract cache with the defined contracts of Interactive Brokers.
  /// </summary>
  public class SynchronizeContractCache
  {
    //constants


    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;
    private IProgressDialog m_progress;

    //constructors
    public SynchronizeContractCache(InstrumentAdapter adapter) 
    {
      m_adapter = adapter;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      List<IBApi.Contract> contracts = m_adapter.m_serviceHost.Cache.GetContractHeaders();
      m_progress = m_adapter.m_dialogService.CreateProgressDialog("Synchronizing Contract Cache", m_adapter.m_logger);
      m_progress.StatusMessage = "Synchronizing Contract Cache from Interactive Brokers";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_progress.Maximum = contracts.Count;
      m_progress.ShowAsync();

      m_adapter.m_serviceHost.Client.Error += HandleError;
      m_adapter.m_contractRequestActive = true;

      int updated = 0;
      foreach (var contract in contracts)
      {
        //this can be a very long running process, check whether we have a connection and wait for the connection to come alive or exit
        //if the user cancels the operation
        bool disconnectDetected = false;
        while (!m_adapter.m_serviceHost.Client.ClientSocket.IsConnected() && !m_progress.CancellationTokenSource.IsCancellationRequested)
        {
          if (!disconnectDetected)
          {
            m_progress.LogWarning($"Connection to Interactive Brokers lost at {DateTime.Now}, waiting for connection to be re-established");
            disconnectDetected = true;
          }
          Thread.Sleep(Constants.DisconnectedSleepInterval);
        }
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;

        //update progress
        m_progress.Progress++;
        updated++;

        //NOTE: InstrumentAdapter.HandleContractDetails is called when the contract details are received.
        m_adapter.m_serviceHost.Client.ClientSocket.reqContractDetails(InstrumentAdapter.InstrumentIdBase, contract);
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
        Thread.Sleep(InstrumentAdapter.IntraRequestSleep);    //throttle requests to avoid exceeding the hard limit imposed by IB
      }

      m_adapter.m_serviceHost.Client.Error -= HandleError;

      m_progress.StatusMessage = $"Synchronized {updated} contracts of {contracts.Count}";
      m_progress.Complete = true;
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
