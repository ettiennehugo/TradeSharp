using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Common;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
    /// <summary>
    /// Interface for the platform independent dialog service.
    /// </summary>
    public interface IDialogService
  {
    //constants


    //enums
    /// <summary>
    /// Severity used for status messages.
    /// </summary>
    public enum StatusMessageSeverity 
    {
      Success,
      Information,
      Warning,
      Error
    }

    /// <summary>
    /// Behavior when importing instrument groups or instruments when an item already exists in the database.
    /// </summary>
    public enum ImportReplaceBehavior
    {
      Skip,
      Update,
      Replace,
    }

    //types
    /// <summary>
    /// Settings to use for importing instrument groups and instruments.
    /// </summary>
    public struct ImportSettings 
    {
      ImportReplaceBehavior importReplaceBehavior;
      string filePath;
    }

    //attributes


    //properties


    //methods
    Task ShowPopupMessageAsync(string message);
    Task ShowStatusMessageAsync(StatusMessageSeverity severity, string title, string message);

    Task<CountryInfo?> ShowSelectCountryAsync();

    Task<Holiday?> ShowCreateHolidayAsync(Guid parentId);
    Task<Holiday?> ShowUpdateHolidayAsync(Holiday holiday);

    Task<Exchange?> ShowCreateExchangeAsync();
    Task<Exchange?> ShowUpdateExchangeAsync(Exchange exchange);

    Task<Session?> ShowCreateSessionAsync(Guid parentId);
    Task<Session?> ShowUpdateSessionAsync(Session session);

    Task<Instrument?> ShowCreateInstrumentAsync();
    Task<Instrument?> ShowUpdateInstrumentAsync(Instrument instrument);
    Task<ImportSettings?> ShowImportInstrumentsAsync();
    Task ShowExportInstrumentsAsync();

    Task<InstrumentGroup?> ShowCreateInstrumentGroupAsync(Guid parentId);
    Task<InstrumentGroup?> ShowUpdateInstrumentGroupAsync(InstrumentGroup instrumentGroup);
    Task<ImportSettings?> ShowImportInstrumentGroupsAsync();
    Task ShowExportInstrumentGroupsAsync();
  }
}
