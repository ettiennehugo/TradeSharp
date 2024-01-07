using TradeSharp.Common;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TradeSharp.CoreUI.Services
{
  //types
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
  /// Timezone of the date/time values specified in a instrument data file being imported.
  /// </summary>
  public enum ImportDataDateTimeTimeZone
  {
    UTC,
    Exchange,
    Local,    //local machine timezone
  }

  /// <summary>
  /// Results from import operation with the specific outcome. Success state is set per default and other states must be explicitly set.
  /// </summary>
  public class ImportResult
  {
    public ImportResult()
    {
      Severity = IDialogService.StatusMessageSeverity.Success;
      StatusMessage = string.Empty;
    }

    public IDialogService.StatusMessageSeverity Severity;
    public string StatusMessage;
  }

  /// <summary>
  /// Results from export operation with specific outcome. Success state is set per default and other states must be explicitly set.
  /// </summary>
  public class ExportResult
  {
    public ExportResult()
    {
      Severity = IDialogService.StatusMessageSeverity.Success;
      StatusMessage = string.Empty;
    }

    public IDialogService.StatusMessageSeverity Severity;
    public string StatusMessage;
  }


  /// <summary>
  /// Settings to use for importing instrument groups, instruments and instrument data.
  /// </summary>
  public partial class ImportSettings: ObservableObject
  {
    public ImportSettings()
    {
      ReplaceBehavior = ImportReplaceBehavior.Skip;
      DateTimeTimeZone = ImportDataDateTimeTimeZone.Exchange;
      Filename = "";
    }

    [ObservableProperty] ImportReplaceBehavior m_replaceBehavior;
    [ObservableProperty] ImportDataDateTimeTimeZone m_dateTimeTimeZone;
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

    Task<IBarData?> ShowCreateBarDataAsync(Resolution resolution, DateTime dateTime);
    Task<IBarData?> ShowUpdateBarDataAsync(IBarData barData);
    Task<ImportSettings?> ShowImportBarDataAsync();
    Task<string?> ShowExportBarDataAsync();
  }
}
