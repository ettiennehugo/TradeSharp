using System.Globalization;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.Data;
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
    private ServiceHost m_serviceHost;
    private IProgressDialog m_progress;

    //constructors
    public CopyContractsToInstruments(ServiceHost serviceHost)
    {
      m_serviceHost = serviceHost;
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
      List<Contract> contracts = m_serviceHost.Cache.GetContracts();
      m_progress = m_serviceHost.DialogService.CreateProgressDialog("Synchronizing Contract Cache", m_serviceHost.Logger);
      m_progress.StatusMessage = "Synchronizing Instrument definitions from Contracts";
      m_progress.Progress = 0;
      m_progress.Minimum = 0;
      m_progress.Maximum = contracts.Count;
      m_progress.ShowAsync();

      bool refreshInstrumentGroups = false;
      int created = 0;
      int updated = 0;

      foreach (var contract in contracts)
      {
        var instrument = m_serviceHost.InstrumentService.Items.FirstOrDefault((i) => DeepCompare(contract, i));
        if (instrument == null)
        {
          instrument = createInstrument(contract);
          if (instrument != null)
          {
            created++;
            refreshInstrumentGroups |= updateInstrumentGroup(instrument!, contract);
          }
        }
        else
        {
          bool instrumentUpdated = updateInstrument(instrument!, contract);
          if (updateInstrumentGroup(instrument!, contract))
          {
            refreshInstrumentGroups = true;
            instrumentUpdated = true;
          }
          if (instrumentUpdated) updated++;
        }

        m_progress.Progress++;
        if (m_progress.CancellationTokenSource.IsCancellationRequested) break;  //exit thread when operation is cancelledigf
      }

      if (refreshInstrumentGroups) Task.Run(m_serviceHost.InstrumentGroupService.Refresh);

      m_progress.StatusMessage = $"Synchronizing instruments complete - created {created} and updated {updated} instruments";
      m_progress.Complete = true;
    }

    private Instrument? createInstrument(Contract contract)
    {
      Instrument? instrument = null; 
      if (contract is ContractStock stock)
      {
        InstrumentType instrumentType = m_serviceHost.Instruments.IBContractTypeToInstrumentType(stock.SecType);
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        string name = textInfo.ToTitleCase(stock.LongName);   //NOTE We use only the long name as the name contains trash at times.
        DateTime inceptionDate = Common.Constants.DefaultMinimumDateTime;

        string primaryExchangeName = stock.PrimaryExch.ToUpper();
        Data.Exchange? exchange = m_serviceHost.ExchangeService.Items.FirstOrDefault((e) => e.Name.ToUpper() == primaryExchangeName);

        //fallback strategies for exchanges
        //Smart exchange - this is the default exchange for IB so we first try it
        if (exchange == null && stock.ValidExchanges.Contains(Constants.DefaultExchange))
          exchange = m_serviceHost.ExchangeService.Items.FirstOrDefault((e) => e.Name.ToUpper() == Constants.DefaultExchange);

        //Global exchange - this should always exist for TradeSharp
        if (exchange == null)
          exchange = m_serviceHost.ExchangeService.Items.FirstOrDefault((e) => e.Id == Data.Exchange.InternationalId);

        //build list of secondary exchanges
        List<Guid> secondaryExchangeIds = new List<Guid>();
        foreach (var exchangeName in stock.ValidExchanges.Split())
        {
          Data.Exchange? secondaryExchange = m_serviceHost.ExchangeService.Items.FirstOrDefault((e) => e.Name.ToUpper() == exchangeName.ToUpper());
          if (secondaryExchange != null)
          {
            if (exchange != null) exchange = secondaryExchange;
            secondaryExchangeIds.Add(secondaryExchange.Id);
          }
        }

        if (exchange == null)
          m_progress.LogError($"Exchange {stock.PrimaryExch} not found for stock {stock.Symbol} and no valid alternatives from \"{stock.ValidExchanges}\"");
        else
        {
          instrument = new Instrument(contract.Symbol, Instrument.DefaultAttributes, contract.Symbol, instrumentType, Array.Empty<string>(), name, name /*this is correct, see note above*/, inceptionDate, Instrument.DefaultPriceDecimals, Instrument.DefaultMinimumMovement, Instrument.DefaultBigPointValue, exchange.Id, secondaryExchangeIds, "");
          m_serviceHost.InstrumentService.Add(instrument);
        }
      }
      else
        m_progress.LogError($"Contract type {contract.SecType} for symbol {contract.Symbol} did not return ContractStock type.");

      return instrument;
    }

    private bool updateInstrument(Instrument instrument, Contract contract)
    {
      bool updated = false;

      //process the instrument according to contract type
      InstrumentType instrumentType = m_serviceHost.Instruments.IBContractTypeToInstrumentType(contract.SecType);
      switch (instrumentType)
      {
        case InstrumentType.Stock:
        case InstrumentType.ETF:
          if (contract is ContractStock stock)
          {
            //check whether the associated instrument reflects the same inception date
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
              m_serviceHost.Database.UpdateInstrument(instrument);
            }

            //check whether the instrument is associated with the specific instrument group
            if (instrumentType != InstrumentType.ETF && stock.Subcategory.Trim() == string.Empty)
            {
              m_progress.LogWarning($"Stock {stock.Symbol} does not have a subcategory set.");
              break;
            }
          }
          else
            m_progress.LogError($"Contract type {contract.SecType} for symbol {contract.Symbol} did not return ContractStock type.");
          break;
        default:
          m_progress.LogError($"Unsupported contract type {contract.SecType} for symbol {contract.Symbol}");
          break;
      }

      return updated;
    }

    /// <summary>
    /// Try to find the instrument group associated with the instrument and update it if required.
    /// </summary>
    private bool updateInstrumentGroup(Instrument instrument, Contract contract)
    {
      bool updated = false;

      if (contract is ContractStock stock)
      {
        //NOTE: There can potentially be instrument groups where the category has the same as the stock sub-category,
        //      in this case we need to find the associated sub-category and ignore the category. The sub-category
        //      will not contain any child groups.
        InstrumentGroup? instrumentGroup = null;
        foreach (var group in m_serviceHost.InstrumentGroupService.Items)
          if (group.Equals(stock.Subcategory) && m_serviceHost.InstrumentGroupService.Items.FirstOrDefault((g) => g.ParentId == group.Id) == null)
          {
            instrumentGroup = group;
            break;
          }

        if (instrumentGroup == null)
        {
          m_progress.LogWarning($"Stock {stock.Symbol} subcategory \"{stock.Subcategory}\" not found.");
        }
        else if (!instrumentGroup.Instruments.Contains(instrument.Ticker))
        {
          m_progress.LogInformation($"Adding stock \"{instrument.Ticker}\" to subcategory \"{stock.Subcategory}\".");
          instrumentGroup.Instruments.Add(instrument.Ticker);
          m_serviceHost.Database.UpdateInstrumentGroup(instrumentGroup);
          updated = true;
        }
      }

      return updated;
    }

  }
}
