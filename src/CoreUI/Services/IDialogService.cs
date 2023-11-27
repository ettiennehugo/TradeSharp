using TradeSharp.Common;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.CoreUI.Services
{

  /// <summary>
  /// Behavior when importing instrument groups or instruments when an item already exists in the database.
  /// </summary>
  public enum ImportReplaceBehavior
  {
    Skip,
    Update,
    Replace,
  }

  /// <summary>
  /// Results from import operation in terms of entries skipped, updated or replaced in the data store.
  /// </summary>
  public class ImportReplaceResult
  {
    public ImportReplaceResult()
    {
      Severity = IDialogService.StatusMessageSeverity.Error;
      Created = 0;
      Skipped = 0;
      Updated = 0;
      Replaced = 0;
    }

    public IDialogService.StatusMessageSeverity Severity;
    public int Created;
    public int Skipped;
    public int Updated;
    public int Replaced;
  }

  //types
  /// <summary>
  /// Settings to use for importing instrument groups and instruments.
  /// </summary>
  public partial class ImportSettings: ObservableObject
  {
    public ImportSettings()
    {
      m_importReplaceBehavior = ImportReplaceBehavior.Skip;
      m_filename = "";
    }

    [ObservableProperty] ImportReplaceBehavior m_importReplaceBehavior;
    [ObservableProperty] string m_filename;
  }

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
    /// State of the status progress bar.
    /// </summary>
    public enum StatusProgressState
    {
      Reset,          //reset the progress bar to indicate no operation in progress
      Normal,         //normal operation value between the given minimum and maximum
      Indeterminate,  //indeterminate state - typically waiting for something to happen before progress can be determined
      Paused,         //paused state - the operation has entered a paused state
      Error,          //error state - operation has reached an error state
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
    Task<string?> ShowExportInstrumentsAsync();

    Task<InstrumentGroup?> ShowCreateInstrumentGroupAsync(Guid parentId);
    Task<InstrumentGroup?> ShowUpdateInstrumentGroupAsync(InstrumentGroup instrumentGroup);
    Task<ImportSettings?> ShowImportInstrumentGroupsAsync();
    Task<string?> ShowExportInstrumentGroupsAsync();

    Task<IBarData?> ShowCreateBarDataAsync(Resolution resolution);
    Task<IBarData?> ShowUpdateBarDataAsync(IBarData barData);

  }
}
