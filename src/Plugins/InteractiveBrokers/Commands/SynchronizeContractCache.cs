﻿using TradeSharp.CoreUI.Common;
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
    private ServiceHost m_serviceHost;
    private IProgressDialog m_progress;
    private int m_requestId;

    //constructors
    public SynchronizeContractCache(ServiceHost serviceHost) 
    {
      m_serviceHost = serviceHost;
      m_requestId = InstrumentAdapter.InstrumentIdBase;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      List<IBApi.Contract> contracts = m_serviceHost.Cache.GetContractHeaders();
      m_progress = m_serviceHost.DialogService.CreateProgressDialog("Synchronizing Contract Cache", m_serviceHost.Logger);
      m_progress.StatusMessage = "Synchronizing Contract Cache from Interactive Brokers";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_progress.Maximum = contracts.Count;
      m_progress.ShowAsync();

      m_serviceHost.Client.Error += HandleError;
      m_serviceHost.Instruments.m_contractRequestActive = true;

      int updated = 0;
      foreach (var contract in contracts)
      {
        //this can be a very long running process, check whether we have a connection and wait for the connection to come alive or exit
        //if the user cancels the operation
        bool disconnectDetected = false;
        while (!m_serviceHost.Client.ClientSocket.IsConnected() && !m_progress.CancellationTokenSource.IsCancellationRequested)
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
        m_serviceHost.Client.ClientSocket.reqContractDetails(m_requestId, contract);
        m_requestId++;
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
      }

      m_serviceHost.Client.Error -= HandleError;

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
          m_serviceHost.Logger.LogError($"Error {errorCode} - {errorMessage ?? ""}");
        else
          m_serviceHost.Logger.LogError($"Error - {e.Message}");
      }
    }
  }
}
