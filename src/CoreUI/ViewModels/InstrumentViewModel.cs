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
    public const int DefaultPageSize = 500;

    //enums


    //types


    //attributes
    private IInstrumentService m_instrumentService;
    private string m_tickerFilter;
    private string m_nameFilter;
    private string m_descriptionFilter;
    private Dictionary<string, object> m_filters;
    private int m_offsetIndex;
    private readonly object m_offsetIndexLock = new object();
    private readonly object m_asyncBusyLock = new object();
    private bool m_asyncBusy;
    private int m_offsetCount;

    //constructors
    public InstrumentViewModel(IInstrumentService itemsService, INavigationService navigationService, IDialogService dialogService) : base(itemsService, navigationService, dialogService)
    {
      m_instrumentService = itemsService;
      m_instrumentService.RefreshEvent += onServiceRefresh;
      m_offsetIndex = 0;
      m_offsetCount = 0;
      m_asyncBusy = false;
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
      if (importSettings != null) _ = Task.Run(() => m_itemsService.Import(importSettings));
    }

    public override async Task OnExportAsync()
    {
      string? filename = await m_dialogService.ShowExportInstrumentsAsync();
      if (filename != null) _ = Task.Run(() => m_itemsService.Export(filename));
    }

    public override Task OnRefreshAsync()
    {
      OffsetIndex = 0;
      return LoadMoreItemsAsync(DefaultPageSize); //view model only supports incremental loading, so we just load the first page
    }

    public Task<IList<Instrument>> LoadMoreItemsAsync(int count)
    {
      if (m_asyncBusy) throw new Exception("Already busy");

      lock (m_asyncBusyLock) m_asyncBusy = true; //set the busy flag to prevent re-entrancy

      return Task.Run(() => {
        try
        {
          updateFilters();  //ensure we sync any changes to the filters
          int index = OffsetIndex;
          int totalCount = Count;
          OffsetCount = count;
          var items = m_instrumentService.GetItems(m_tickerFilter, m_nameFilter, m_descriptionFilter, m_offsetIndex, count);
          OffsetIndex += items.Count;
          return items;
        }
        finally
        {
          lock (m_asyncBusyLock) m_asyncBusy = false; //clear the busy flag
        }
      });
    }

    //properties
    public int Count { get { updateFilters(); return m_instrumentService.GetCount(m_tickerFilter, m_nameFilter, m_descriptionFilter); } }
    public int OffsetIndex 
    { 
      get => m_offsetIndex; 
      set { 
        if (value < 0) throw new ArgumentException("Offset index must be positive or zero.");
        lock (m_offsetIndexLock) 
        {
          m_offsetIndex = value;
        }
      } 
    }
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
