using TradeSharp.Common;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace TradeSharp.CoreUI.Services
{
  //types
  /// <summary>
  /// Supported import/export file types.
  /// </summary>
  public enum ImportExportFileTypes
  {
    CSV,
    JSON,
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
  /// Settings to use for importing instrument groups, instruments and instrument data.
  /// </summary>
  public partial class ImportSettings: ObservableObject
  {
    public ImportSettings()
    {
      FromDateTime = Constants.DefaultMinimumDateTime;
      ToDateTime = Constants.DefaultMaximumDateTime;
      ReplaceBehavior = ImportReplaceBehavior.Skip;
      DateTimeTimeZone = ImportDataDateTimeTimeZone.Exchange;
      Filename = "";
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportReplaceBehavior m_replaceBehavior;
    [ObservableProperty] ImportDataDateTimeTimeZone m_dateTimeTimeZone;
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
      FromDateTime = DateTime.Now;
      ToDateTime = DateTime.Now;
      ReplaceBehavior = ImportReplaceBehavior.Update;
      DateTimeTimeZone = ImportDataDateTimeTimeZone.UTC;
      Directory = "";
      ImportStructure = MassImportExportStructure.DiretoriesAndFiles;
      FileType = ImportExportFileTypes.CSV;
      ResolutionMinute = true;
      ResolutionHour = true;
      ResolutionDay = true;
      ResolutionWeek = true;
      ResolutionMonth = true;
      ThreadCount = Environment.ProcessorCount;    //clip this the Environment.ProcessorCount as max since it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportReplaceBehavior m_replaceBehavior;
    [ObservableProperty] ImportDataDateTimeTimeZone m_dateTimeTimeZone;
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
      FromDateTime = DateTime.Now;
      ToDateTime = DateTime.Now;
      DateTimeTimeZone = ImportDataDateTimeTimeZone.UTC;
      Directory = "";
      ExportStructure = MassImportExportStructure.DiretoriesAndFiles;
      FileType = ImportExportFileTypes.CSV;
      ResolutionMinute = true;
      ResolutionHour = true;
      ResolutionDay = true;
      ResolutionWeek = true;
      ResolutionMonth = true;
      ThreadCount = Environment.ProcessorCount;    //clip this the Environment.ProcessorCount as max since it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportDataDateTimeTimeZone m_dateTimeTimeZone;
    [ObservableProperty] string m_directory;
    [ObservableProperty] MassImportExportStructure m_exportStructure;
    [ObservableProperty] ImportExportFileTypes m_fileType;
    [ObservableProperty] bool m_resolutionMinute;
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
      FromDateTime = DateTime.Now;
      ToDateTime = DateTime.Now;
      DateTimeTimeZone = ImportDataDateTimeTimeZone.UTC;
      ResolutionMinute = true;
      ResolutionHour = true;
      ResolutionDay = true;
      ResolutionWeek = true;
      ResolutionMonth = true;
      ThreadCount = 1;    //clip this the Environment.ProcessorCount as max and the data provider max connection count, it would most likely not be useful to have more threads than processors
    }

    [ObservableProperty] DateTime m_fromDateTime;
    [ObservableProperty] DateTime m_toDateTime;
    [ObservableProperty] ImportDataDateTimeTimeZone m_dateTimeTimeZone;
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

    Task ShowMassDataImportAsync(string dataProvider);
    Task ShowMassDataExportAsync(string dataProvider);
    Task ShowMassDataDownloadAsync(string dataProvider);
  }
}
