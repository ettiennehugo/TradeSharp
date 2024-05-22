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
      m_adapter.m_serviceHost.Cache.Clear();    //ensure cache starts fresh after the update
      m_progress = m_adapter.m_dialogService.CreateProgressDialog("Synchronizing Contract Cache", m_adapter.m_logger);
      m_progress.StatusMessage = "Synchronizing Contract Cache from Instrument Definitions";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_progress.Maximum = m_adapter.m_instrumentService.Items.Count;
      m_progress.ShowAsync();
      m_adapter.m_serviceHost.Client.Error += HandleError;
      m_adapter.m_contractRequestActive = true;

      foreach (var instrument in m_adapter.m_instrumentService.Items)
      {
        var currency = "USD";   //currently hardcoded to support Stock contract retrieval
        var exchange = m_adapter.m_exchangeService.Items.FirstOrDefault(e => e.Id == instrument.PrimaryExchangeId);
        var country = m_adapter.m_countryService.Items.FirstOrDefault(c => c.Id == exchange?.CountryId);
        if (country != null) currency = country?.CountryInfo?.RegionInfo.ISOCurrencySymbol;
        //NOTE: This is very much coded to work with only Stock cotnracts, other contract types will need to be handled differently.
        var contract = new IBApi.Contract { Symbol = instrument.Ticker, SecType = m_adapter.InstrumentTypeToIBContractType(instrument.Type), Exchange = Constants.DefaultExchange, Currency = currency };
        m_adapter.m_serviceHost.Client.ClientSocket.reqContractDetails(InstrumentAdapter.InstrumentIdBase, contract);
    
        m_progress.Progress++;
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
        Thread.Sleep(InstrumentAdapter.IntraRequestSleep);    //throttle requests to avoid exceeding the hard limit imposed by IB
      }

      m_adapter.m_serviceHost.Client.Error -= HandleError;
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
