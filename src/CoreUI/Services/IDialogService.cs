using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Views;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace TradeSharp.CoreUI.Services
{
  //types
  /// <summary>
  /// Supported import/export file types.
  /// </summary>
  public enum ImportExportFileTypes
  {
    [Description("Comma Separated Values (CSV)")]
    CSV,
    [Description("JavaScript Object Notation (JSON)")]
    JSON,
  }  
  
  /// <summary>
  /// Behavior when importing instrument groups, instruments or instrument bar data when an item already exists in the database.
  /// </summary>
  public enum ImportReplaceBehavior
  {
    Skip,
    Update,
    Replace,
  }

  /// <summary>
  /// Behavior when exporting instrument groups, instruments or instrument bar data when an export file aready exists.
  /// </summary>
  public enum ExportReplaceBehavior
  {
    Skip,
    Replace,
  }

  /// <summary>
  /// Instrument selection view controls visible encodings.
  /// </summary>
  [Flags]
  public enum InstrumentSelectionViewMode
  {
    SelectSingle = 1,
    SelectMulti = 2,
    Refresh = 4, 
    Add = 8,
    Edit = 16,
    Delete = 32,
    Import = 64,
    Export = 128,
  }

  /// <summary>
  /// Timezone of the date/time values specified in a instrument data file being imported.
  /// </summary>
  public enum ImportExportDataDateTimeTimeZone
  {
    [Description("Coordinated Universal Time (UTC)")]
    UTC,
    Exchange,
    Local,    //local machine timezone
  }

  /// <summary>
  /// Settings to use for importing instrument groups, instruments and instrument data.
  /// </summary>
  public partial class ImportSettings: ObservableObject
  {
    public ImportSettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      ReplaceBehavior = ImportReplaceBehavior.Skip;
      DateTimeTimeZone = ImportExportDataDateTimeTimeZone.Exchange;
      Filename = "";
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportReplaceBehavior m_replaceBehavior;
    [ObservableProperty] ImportExportDataDateTimeTimeZone m_dateTimeTimeZone;
    [ObservableProperty] string m_filename;
  }

  /// <summary>
  /// Settings to use for exporting instrument groups, instruments and instrument data.
  /// </summary>
  public partial class ExportSettings: ObservableObject
  {
    public ExportSettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      ReplaceBehavior = ExportReplaceBehavior.Skip;
      DateTimeTimeZone = ImportExportDataDateTimeTimeZone.Exchange;
      Filename = "";
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ExportReplaceBehavior m_replaceBehavior;
    [ObservableProperty] ImportExportDataDateTimeTimeZone m_dateTimeTimeZone;
    [ObservableProperty] string m_filename;
  }

  /// <summary>
  /// Structure to use for mass import/export of instrument data.
  /// </summary>
  public enum MassImportExportStructure
  {
    [Description("Directories and files")]
    DiretoriesAndFiles,   //export data into directories according to timeframe and then individual files for the stock data
    [Description("Files only")]
    FilesOnly,            //export data into individual files for the data with the format <instrument>.<resolution>.csv/json
  }

  /// <summary>
  /// Settings used for mass import of instrument data.
  /// </summary>
  public partial class MassImportSettings: ObservableObject
  {
    public MassImportSettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      ReplaceBehavior = ImportReplaceBehavior.Update;
      DateTimeTimeZone = ImportExportDataDateTimeTimeZone.Exchange; //most data are captured in exchange time zone
      Directory = "";
      ImportStructure = MassImportExportStructure.DiretoriesAndFiles;
      FileType = ImportExportFileTypes.CSV;
      ResolutionMinute = false;
      ResolutionHour = false;
      ResolutionDay = true;
      ResolutionWeek = false;
      ResolutionMonth = false;
      ThreadCount = Environment.ProcessorCount;    //clip this the Environment.ProcessorCount as max since it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportReplaceBehavior m_replaceBehavior;
    [ObservableProperty] ImportExportDataDateTimeTimeZone m_dateTimeTimeZone;
    [ObservableProperty] string m_directory;
    [ObservableProperty] MassImportExportStructure m_importStructure;
    [ObservableProperty] ImportExportFileTypes m_fileType;
    [ObservableProperty] bool m_resolutionMinute;
    [ObservableProperty] bool m_resolutionHour;
    [ObservableProperty] bool m_resolutionDay;
    [ObservableProperty] bool m_resolutionWeek;
    [ObservableProperty] bool m_resolutionMonth;
    [ObservableProperty] int m_threadCount;
  }

  /// <summary>
  /// Settings used for mass export of instrument data.
  /// </summary>
  public partial class MassExportSettings: ObservableObject
  {
    public MassExportSettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      DateTimeTimeZone = ImportExportDataDateTimeTimeZone.Exchange;
      Directory = "";
      ExportStructure = MassImportExportStructure.DiretoriesAndFiles;
      FileType = ImportExportFileTypes.CSV;
      CreateEmptyFiles = false;
      ResolutionMinute = false;
      ResolutionHour = false;
      ResolutionDay = true;
      ResolutionWeek = true;
      ResolutionMonth = true;
      ThreadCount = Environment.ProcessorCount;    //clip this the Environment.ProcessorCount as max since it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportExportDataDateTimeTimeZone m_dateTimeTimeZone;
    [ObservableProperty] string m_directory;
    [ObservableProperty] MassImportExportStructure m_exportStructure;
    [ObservableProperty] ImportExportFileTypes m_fileType;
    [ObservableProperty] bool m_createEmptyFiles;
    [ObservableProperty] bool m_resolutionMinute;
    [ObservableProperty] bool m_resolutionHour;
    [ObservableProperty] bool m_resolutionDay;
    [ObservableProperty] bool m_resolutionWeek;
    [ObservableProperty] bool m_resolutionMonth;
    [ObservableProperty] int m_threadCount;
  }

  /// <summary>
  /// Settings used for mass copy of instrument data.
  /// </summary>
  public partial class MassCopySettings : ObservableObject
  {
    public MassCopySettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      ResolutionHour = false;
      ResolutionDay = false;
      ResolutionWeek = false;
      ResolutionMonth = false;
      ThreadCount = Environment.ProcessorCount;    //clip this the Environment.ProcessorCount as max since it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] bool m_resolutionHour;
    [ObservableProperty] bool m_resolutionDay;
    [ObservableProperty] bool m_resolutionWeek;
    [ObservableProperty] bool m_resolutionMonth;
    [ObservableProperty] int m_threadCount;
  }

  /// <summary>
  /// Settings used for mass download of instrument data.
  /// </summary>
  public partial class MassDownloadSettings: ObservableObject
  {
    public MassDownloadSettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      DateTimeTimeZone = ImportExportDataDateTimeTimeZone.UTC;
      ResolutionMinute = true;
      ResolutionHour = true;
      ResolutionDay = true;
      ResolutionWeek = true;
      ResolutionMonth = true;
      ThreadCount = 1;    //clip this the Environment.ProcessorCount as max and the data provider max connection count, it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportExportDataDateTimeTimeZone m_dateTimeTimeZone;
    [ObservableProperty] bool m_resolutionMinute;
    [ObservableProperty] bool m_resolutionHour;
    [ObservableProperty] bool m_resolutionDay;
    [ObservableProperty] bool m_resolutionWeek;
    [ObservableProperty] bool m_resolutionMonth;
    [ObservableProperty] int m_threadCount;
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
    void PostUIUpdate(Action updateAction);     //post void return action to the UI thread
    Task<T> PostUIUpdateAsync<T>(Func<T> updateAction);   //post return type T action to the UI thread
    Task ShowPopupMessageAsync(string message);
    Task ShowStatusMessageAsync(StatusMessageSeverity severity, string title, string message);
    IProgressDialog CreateProgressDialog(string title, ILogger? logger);
    ICorrectiveLoggerDialog CreateCorrectiveLoggerDialog(string title, LogEntry? entry = null); //adds given log entry to display it in the dialog (typically a collapsible log entry

    Task<CountryInfo?> ShowSelectCountryAsync();

    Task<Holiday?> ShowCreateHolidayAsync(Guid parentId);
    Task<Holiday?> ShowUpdateHolidayAsync(Holiday holiday);

    Task<Exchange?> ShowCreateExchangeAsync();
    Task<Exchange?> ShowUpdateExchangeAsync(Exchange exchange);

    Task<Session?> ShowCreateSessionAsync(Guid parentId);
    Task<Session?> ShowUpdateSessionAsync(Session session);

    Task<Instrument?> ShowCreateInstrumentAsync(InstrumentType instrumentType);
    Task ShowUpdateInstrumentAsync(Instrument instrument);
    Task<ImportSettings?> ShowImportInstrumentsAsync();
    Task<ExportSettings?> ShowExportInstrumentsAsync();

    Task<InstrumentGroup?> ShowCreateInstrumentGroupAsync(Guid parentId);
    Task<InstrumentGroup?> ShowUpdateInstrumentGroupAsync(InstrumentGroup instrumentGroup);
    Task<ImportSettings?> ShowImportInstrumentGroupsAsync();
    Task<ExportSettings?> ShowExportInstrumentGroupsAsync();

    Task<IBarData?> ShowCreateBarDataAsync(Resolution resolution, DateTime dateTime);
    Task<IBarData?> ShowUpdateBarDataAsync(IBarData barData);
    Task<ImportSettings?> ShowImportBarDataAsync();
    Task<ExportSettings?> ShowExportBarDataAsync();

    Task ShowMassDataImportAsync(string dataProvider);
    Task ShowMassDataExportAsync(string dataProvider);
    Task ShowMassDataCopyAsync(string dataProvider);
    Task ShowMassDataDownloadAsync(string dataProvider);

    Task ShowAccountDialogAsync();                                        //show accounts for all defined broker plugins
    Task ShowAccountDialogAsync(IBrokerPlugin broker);                    //show accounts for a specific broker plugin
    Task ShowAccountDialogAsync(IBrokerPlugin broker, Account account);   //show account for specific broker plugin and account
  }
}
