using TradeSharp.CoreUI.Common;

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
    private ServiceHost m_serviceHost;

    //constructors
    public ValidateInstruments(ServiceHost serviceHost)
    {
      m_serviceHost = serviceHost;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      IProgressDialog progress = m_serviceHost.DialogService.CreateProgressDialog("Validating Instruments", m_serviceHost.Logger);
      progress.StatusMessage = "Validating Instrument definitions against the Contract Cache definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_serviceHost.InstrumentService.Items.Count;
      progress.ShowAsync();

      foreach (var instrument in m_serviceHost.InstrumentService.Items)
      {
        var contract = m_serviceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

        if (contract == null)
          foreach (var altTicker in instrument.AlternateTickers)
          {
            contract = m_serviceHost.Cache.GetContract(altTicker, Constants.DefaultExchange);
            if (contract != null) progress.LogInformation($"Will not match on primary ticker but on alternate ticker {altTicker}.");
          }

        if (contract == null)
          progress.LogError($"\"{instrument.Ticker}\" - No contract definition found for ticker.");
        else
        {
          //check that instrument group would be correct
          if (contract is ContractStock contractStock)
          {
            if (contractStock.StockType == Constants.StockTypeCommon)
            {
              if (contractStock.Subcategory != string.Empty)
              {
                var instrumentGroup = m_serviceHost.InstrumentGroupService.Items.FirstOrDefault(g => g.Equals(contractStock.Subcategory));
                if (instrumentGroup == null)
                  progress.LogError($"\"{instrument.Ticker}\" - Instrument group for {contractStock.Industry}->{contractStock.Category}->{contractStock.Subcategory} not found.");
              }
              else
              {
                if (contractStock.Industry != string.Empty) progress.LogWarning($"\"{instrument.Ticker}\" - Stock contract {contractStock.Symbol} has no associated Industry set.");
                if (contractStock.Category != string.Empty) progress.LogWarning($"\"{instrument.Ticker}\" - Stock contract {contractStock.Symbol} has no associated Category set.");
              }
            }
          }
          else
            progress.LogError($"Contract {contract.Symbol}, {contract.SecType} is not supported.");
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
      }

      progress.Complete = true;
    }
  }
}
