using TradeSharp.CoreUI.Common;
using TradeSharp.Data;
using Microsoft.Extensions.Logging;
using System.Threading;
using IBApi;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Copy the Interactive Brokers contract definitions to the TradeSharp Instruments.
  /// </summary>
  public class CopyContractsToInstruments
  {
    //constants


    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;
    private IProgressDialog m_progress;

    //constructors
    public CopyContractsToInstruments(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public static bool DeepCompare(object? a, object? b)
    {
      Contract? contract = null;
      Instrument? instrument = null;

      if (a is Contract && b is Instrument)
      {
        contract = a as Contract;
        instrument = b as Instrument;
      }
      else if (a is Instrument && b is Contract)
      {
        contract = b as Contract;
        instrument = a as Instrument;
      }

      if (contract != null && instrument != null) return instrument.Equals(contract.Symbol);

      return false;
    }

    public void Run()
    {
      List<Contract> contracts = m_adapter.m_serviceHost.Cache.GetContracts();
      m_progress = m_adapter.m_dialogService.CreateProgressDialog("Synchronizing Contract Cache", m_adapter.m_logger);
      m_progress.StatusMessage = "Synchronizing Instrument definitions from Contracts";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_progress.Maximum = contracts.Count;
      m_progress.ShowAsync();

      bool refreshInstrumentGroups = false;
      foreach (var contract in contracts)
      {
        //try to find the associated instrument
        var instrument = m_adapter.m_instrumentService.Items.FirstOrDefault((i) => DeepCompare(contract, i));
        if (instrument == null)
        {
          m_progress.LogWarning($"No instrument definition for \"{contract.SecType}\" - \"{contract.Symbol}\"");
          m_progress.Progress++;
          if (m_progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelled
          continue;
        }

        //process the instrument according to contract type
        InstrumentType instrumentType = m_adapter.IBContractTypeToInstrumentType(contract.SecType);

        switch (instrumentType)
        {
          case InstrumentType.Stock:
          case InstrumentType.ETF:
            if (contract is ContractStock stock)
            {
              //check whether the associated instrument reflects the same inception date
              bool updated = false;
              if (DateOnly.TryParse(stock.IssueDate, out DateOnly issueDate) && issueDate != DateOnly.FromDateTime(instrument.InceptionDate))
              {
                instrument.InceptionDate = issueDate.ToDateTime(TimeOnly.MinValue);
                updated = true;
              }

              //check that the instrument type is set correctly
              if (instrument.Type != instrumentType)
              {
                instrument.Type = instrumentType;
                updated = true;
              }

              if (updated)
              {
                m_progress.LogInformation($"Updating instrument \"{instrument.Ticker}\" inception date\\instrument type.");  
                m_adapter.m_database.UpdateInstrument(instrument);
              }

              //check whether the instrument is associated with the specific instrument group
              if (instrumentType != InstrumentType.ETF && stock.Subcategory == string.Empty)
              {
                m_adapter.m_logger.LogWarning($"Stock {stock.Symbol} does not have a subcategory set.");
                break;
              }

              var instrumentGroup = m_adapter.m_instrumentGroupService.Items.FirstOrDefault((g) => g.Equals(stock.Subcategory));
              if (instrumentGroup == null)
              {
                m_adapter.m_logger.LogWarning($"Stock {stock.Symbol} subcategory {stock.Subcategory} not found.");
                break;
              }

              if (!instrumentGroup.Instruments.Contains(instrument.Ticker))
              {
                m_progress.LogInformation($"Adding stock \"{instrument.Ticker}\" to subcategory \"{stock.Subcategory}\".");
                instrumentGroup.Instruments.Add(instrument.Ticker);
                m_adapter.m_database.UpdateInstrumentGroup(instrumentGroup);
                refreshInstrumentGroups = true;
              }
            }
            else
              m_adapter.m_logger.LogWarning($"Contract type {contract.SecType} for symbol {contract.Symbol} did not return ContractStock type.");
            break;
          default:
            m_adapter.m_logger.LogWarning($"Unsupported contract type {contract.SecType} for symbol {contract.Symbol}");
            break;
        }

        m_progress.Progress++;
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelledigf
      }

      if (refreshInstrumentGroups) Task.Run(m_adapter.m_instrumentGroupService.Refresh);
      m_progress.Complete = true;
    }
  }
}
