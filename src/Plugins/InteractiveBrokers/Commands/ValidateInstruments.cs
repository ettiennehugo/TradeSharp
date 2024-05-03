using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Validate the defined set of instruments against the Interactive Brokers defined contracts.
  /// </summary>
  public class ValidateInstruments
  {
    //constants


    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;

    //constructors
    public ValidateInstruments(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      IProgressDialog progress = m_adapter.m_dialogService.CreateProgressDialog("Validating Instruments", m_adapter.m_logger);
      progress.StatusMessage = "Validating Instrument definitions against the Contract Cache definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_adapter.m_instrumentService.Items.Count;
      progress.ShowAsync();

      foreach (var instrument in m_adapter.m_instrumentService.Items)
      {
        using (progress.BeginScope($"Validating {instrument.Ticker}"))
        {
          var contract = m_adapter.m_serviceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

          if (contract == null)
            foreach (var altTicker in instrument.AlternateTickers)
            {
              contract = m_adapter.m_serviceHost.Cache.GetContract(altTicker, Constants.DefaultExchange);
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
                  var instrumentGroup = m_adapter.m_instrumentGroupService.Items.FirstOrDefault(g => g.Equals(contractStock.Subcategory));
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
  }
}
