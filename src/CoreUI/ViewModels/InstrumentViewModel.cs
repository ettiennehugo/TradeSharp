using TradeSharp.Data;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for list of instruments, it supports incremental loading of the objects from the service.
  /// </summary>
  public class InstrumentViewModel : ListViewModel<Instrument>, Common.IIncrementalSource<Instrument>
  {
    //constants
    /// <summary>
    /// Supported filter fields for the instrument service.
    /// </summary>
    public const string FilterTicker = "Ticker";
    public const string FilterName = "Name";
    public const string FilterDescription = "Description";
    public const int DefaultPageSize = 100;

    //enums


    //types


    //attributes
    private IInstrumentService m_instrumentService;
    private string m_tickerFilter;
    private string m_nameFilter;
    private string m_descriptionFilter;
    private Dictionary<string, object> m_filters;
    private int m_offsetIndex;
    private int m_offsetCount;

    //constructors
    public InstrumentViewModel(IInstrumentService itemsService, INavigationService navigationService, IDialogService dialogService): base(itemsService, navigationService, dialogService) 
    {
      m_instrumentService = itemsService;
      m_offsetIndex = 0;
      m_offsetCount = 0;
      m_filters = new Dictionary<string, object>();
    }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      Instrument? newInstrument = await m_dialogService.ShowCreateInstrumentAsync();
      if (newInstrument != null)
        m_itemsService.Add(newInstrument);
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedSession = await m_dialogService.ShowUpdateInstrumentAsync(SelectedItem);
        if (updatedSession != null)
          m_itemsService.Update(updatedSession);
      }
    }

    public override async Task OnImportAsync()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportInstrumentsAsync();

      if (importSettings != null)
      {
        ImportResult importResult = m_itemsService.Import(importSettings);
        await m_dialogService.ShowStatusMessageAsync(importResult.Severity, "", importResult.StatusMessage);
        m_itemsService.Refresh();
      }
    }

    public override async Task OnExportAsync()
    {
        string? filename = await m_dialogService.ShowExportInstrumentsAsync();

        if (filename != null)
        {
          ExportResult exportResult = m_itemsService.Export(filename);
          await m_dialogService.ShowStatusMessageAsync(exportResult.Severity, "", exportResult.StatusMessage);
        }
    }

    public override Task OnRefreshAsync()
    {      
      return LoadMoreItemsAsync(DefaultPageSize); //view model only supports incremental loading, so we just load the first page
    }

    public Task<IList<Instrument>> LoadMoreItemsAsync(int count)
    {
      updateFilters();  //ensure we sync any changes to the filters
      int index = m_offsetIndex;
      int totalCount = Count;
      m_offsetIndex+= m_offsetCount;
      OffsetCount = count;
      if (m_offsetIndex > totalCount) m_offsetIndex = totalCount; //clip offset index to the number of items in the database
      return Task.Run(() => m_instrumentService.GetItems(m_tickerFilter, m_nameFilter, m_descriptionFilter, index, m_offsetCount));
    }

    //properties
    public int Count { get { updateFilters(); return m_instrumentService.GetCount(m_tickerFilter, m_nameFilter, m_descriptionFilter); } }
    public int OffsetIndex { get => m_offsetIndex; set { if (value < 0) throw new ArgumentException("Offset index must be positive or zero."); m_offsetIndex = value; } }
    public int OffsetCount { get => m_offsetCount; set { if (value <= 0) throw new ArgumentException("Offset count must be positive."); m_offsetCount = value; } }
    public bool HasMoreItems { get => m_offsetIndex < Count; }
    public IDictionary<string, object> Filters { get => m_filters; set => m_filters = (Dictionary<string, object>)value; }

    //methods
    private void updateFilters()
    {
      //get user entered filters
      m_tickerFilter = m_filters.ContainsKey(FilterTicker) ? (string)m_filters[FilterTicker] : string.Empty;
      m_nameFilter = m_filters.ContainsKey(FilterName) ? (string)m_filters[FilterName] : string.Empty;
      m_descriptionFilter = m_filters.ContainsKey(FilterDescription) ? (string)m_filters[FilterDescription] : string.Empty;

      //always match using wildcards
      if (m_tickerFilter.Length > 0 && !m_tickerFilter.Contains("*") && !m_tickerFilter.Contains("?")) m_tickerFilter = $"*{m_tickerFilter}*";
      if (m_nameFilter.Length > 0 && !m_nameFilter.Contains("*") && !m_nameFilter.Contains("?")) m_nameFilter = $"*{m_nameFilter}*";
      if (m_descriptionFilter.Length > 0 && !m_descriptionFilter.Contains("*") && !m_descriptionFilter.Contains("?")) m_descriptionFilter = $"*{m_descriptionFilter}*";
    }
  }
}
