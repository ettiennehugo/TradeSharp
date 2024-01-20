using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.Input;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for instrument bar data.
  /// </summary>
  public partial class InstrumentBarDataViewModel : ListViewModel<IBarData>, Common.IIncrementalSource<IBarData>
  {
    //constants
    /// <summary>
    /// Supported filter fields for the instrument bar data service.
    /// </summary>
    public const string FilterFromDateTime = "FromDateTime";
    public const string FilterToDateTime = "ToDateTime";
    public const int DefaultPageSize = 500;
    public const string DefaultPriceValueFormatMask = "0:0.00";

    //enums


    //types


    //attributes
    private string m_dataProvider;
    private Resolution m_resolution;
    private Instrument? m_instrument;
    private IInstrumentBarDataService m_barDataService;
    private DateTime m_oldFromDateTime;
    private DateTime m_oldToDateTime;
    private DateTime m_fromDateTime;
    private DateTime m_toDateTime;
    private Dictionary<string, object> m_filter;
    private readonly object m_offsetIndexLock = new object();   //incremental loading is not thread safe, so we need to lock the offset index
    private int m_offsetIndex;
    private int m_offsetCount;
    private string m_priceValueFormatMask;

    //constructors
    public InstrumentBarDataViewModel(IInstrumentBarDataService itemService, INavigationService navigationService, IDialogService dialogService) : base(itemService, navigationService, dialogService) //need to get a transient instance of the service uniquely associated with this view model
    {
      m_barDataService = (IInstrumentBarDataService)m_itemsService;
      m_barDataService.Resolution = Resolution; //need to always keep the service resolution the same as the view model resolution
      m_barDataService.RefreshEvent += onServiceRefresh;
      m_oldFromDateTime = m_fromDateTime = DateTime.MinValue;
      m_oldToDateTime = m_toDateTime = DateTime.MaxValue;
      m_filter = new Dictionary<string, object>();
      m_offsetIndex = 0;
      m_offsetCount = DefaultPageSize;
      m_priceValueFormatMask = DefaultPriceValueFormatMask;
      Resolution = Resolution.Day;
      DataProvider = string.Empty;
      Instrument = null;
      AddCommand = new RelayCommand(OnAdd, () => DataProvider != string.Empty && Instrument != null); //view model must be keyed correctly before allowing the adding new items
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null);
      ImportCommandAsync = new AsyncRelayCommand(OnImportAsync, () => DataProvider != string.Empty && Instrument != null);
      ExportCommandAsync = new AsyncRelayCommand(OnExportAsync, () => DataProvider != string.Empty && Instrument != null && Items.Count > 0);
      CopyCommandAsync = new AsyncRelayCommand<object?>(OnCopyAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0);
      CopyToHourCommandAsync = new AsyncRelayCommand<object?>(OnCopyToHourAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0 && Resolution == Resolution.Minute);
      CopyToDayCommandAsync = new AsyncRelayCommand<object?>(OnCopyToDayAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0 && (Resolution == Resolution.Minute || Resolution == Resolution.Hour));
      CopyToWeekCommandAsync = new AsyncRelayCommand<object?>(OnCopyToWeekAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0 && (Resolution == Resolution.Minute || Resolution == Resolution.Hour || Resolution == Resolution.Day));
      CopyToMonthCommandAsync = new AsyncRelayCommand<object?>(OnCopyToMonthAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0 && (Resolution == Resolution.Minute || Resolution == Resolution.Hour || Resolution == Resolution.Day || Resolution == Resolution.Week));
      CopyToAllCommandAsync = new AsyncRelayCommand<object?>(OnCopyToAllAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0 && Resolution != Resolution.Month);
    }

    //finalizers


    //interface implementations
    public override async void OnAdd()
    {
      IBarData? newBar = await m_dialogService.ShowCreateBarDataAsync(Resolution, DateTime.Now);
      if (newBar != null)
      {
        newBar.Resolution = Resolution;
        m_itemsService.Add(newBar);
        SelectedItem = newBar;
      }
    }

    public override async void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedBar = await m_dialogService.ShowUpdateBarDataAsync(SelectedItem);
        if (updatedBar != null)
        {
          m_itemsService.Update(updatedBar);
          SelectedItem = updatedBar;
        }
      }
    }

    public override Task OnRefreshAsync()
    {
      OffsetIndex = 0;
      return LoadMoreItemsAsync(DefaultPageSize); //view model only supports incremental loading, so we just load the first page
    }

    public override async Task OnImportAsync()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportBarDataAsync();
      if (importSettings != null) _ = Task.Run(() => m_itemsService.Import(importSettings));
    }

    public override async Task OnExportAsync()
    {
      string? filename = await m_dialogService.ShowExportBarDataAsync();
      if (filename != null) _ = Task.Run(() => m_itemsService.Export(filename));
    }

    public Task<IList<IBarData>> GetItems(DateTime from, DateTime to)
    {
      return Task.Run(() => m_barDataService.GetItems(from, to));
    }

    public Task<IList<IBarData>> GetItems(int index, int count)
    {
      return Task.Run(() => m_barDataService.GetItems(index, count));
    }

    public Task<IList<IBarData>> GetItems(DateTime from, DateTime to, int index, int count)
    {
      return Task.Run(() => m_barDataService.GetItems(from, to, index, count));
    }

    public virtual Task OnCopyToHourAsync(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual Task OnCopyToDayAsync(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual Task OnCopyToWeekAsync(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual Task OnCopyToMonthAsync(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual Task OnCopyToAllAsync(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public Task<IList<IBarData>> LoadMoreItemsAsync(int count)
    {
      updateFilters();
      OffsetCount = count;
      int index = OffsetIndex;
      int totalCount = Count;
      OffsetIndex += m_offsetCount;
      if (m_offsetIndex > totalCount) OffsetIndex = totalCount; //clip offset index to the number of items in the database
      return Task.Run(() => m_barDataService.GetItems(m_fromDateTime, m_toDateTime, index, m_offsetCount));
    }

    //properties
    public AsyncRelayCommand<object?> CopyToHourCommandAsync { get; internal set; }
    public AsyncRelayCommand<object?> CopyToDayCommandAsync { get; internal set; }
    public AsyncRelayCommand<object?> CopyToWeekCommandAsync { get; internal set; }
    public AsyncRelayCommand<object?> CopyToMonthCommandAsync { get; internal set; }
    public AsyncRelayCommand<object?> CopyToAllCommandAsync { get; internal set; }

    public string DataProvider
    {
      get => m_dataProvider;
      set
      {
        SetProperty(ref m_dataProvider, value);
        m_barDataService.DataProvider = value;
        if (DataProvider != string.Empty && Instrument != null) RefreshCommandAsync.ExecuteAsync(null);
        NotifyCanExecuteChanged();
      }
    }

    public Resolution Resolution
    {
      get => m_resolution;
      set
      {
        SetProperty(ref m_resolution, value);
        m_barDataService.Resolution = value;
        if (DataProvider != string.Empty && Instrument != null) RefreshCommandAsync.ExecuteAsync(null);
        NotifyCanExecuteChanged();
      }
    }

    public Instrument? Instrument
    {
      get => m_instrument;
      set
      {
        SetProperty(ref m_instrument, value);
        m_barDataService.Instrument = value;
        updatePriceValueFormatMask();
        if (DataProvider != string.Empty && Instrument != null) RefreshCommandAsync.ExecuteAsync(null);
        NotifyCanExecuteChanged();
      }
    }

    public int Count { get { updateFilters(); return isKeyed() ? m_barDataService.GetCount(m_fromDateTime, m_toDateTime) : 0; } }
    public int OffsetIndex
    {
      get => m_offsetIndex;
      set
      {
        if (value < 0) throw new ArgumentException("Offset index must be positive or zero.");
        lock (m_offsetIndexLock)
        {
          m_offsetIndex = value;
        }
      }
    }
    public int OffsetCount { get => m_offsetCount; set { if (value <= 0) throw new ArgumentException("Offset count must be positive."); m_offsetCount = value; } }
    public bool HasMoreItems { get => m_offsetIndex < Count; }
    public IDictionary<string, object> Filter { get => m_filter; set => m_filter = (Dictionary<string, object>)value; }
    public string PriceValueFormatMask { get => m_priceValueFormatMask; } //string.Format value format mask for the price values based on Instrument

    //methods
    private bool isKeyed()
    {
      return DataProvider != string.Empty && Instrument != null;
    }

    private void updateFilters()
    {
      m_fromDateTime = m_filter.ContainsKey(FilterFromDateTime) ? (DateTime)m_filter[FilterFromDateTime] : DateTime.MinValue;
      m_toDateTime = m_filter.ContainsKey(FilterToDateTime) ? (DateTime)m_filter[FilterToDateTime] : DateTime.MinValue;
      if (!m_oldFromDateTime.Equals(m_fromDateTime) || !m_oldToDateTime.Equals(m_toDateTime)) m_offsetIndex = 0; //reset the offset index if the filter has changed
      m_oldFromDateTime = m_fromDateTime;
      m_oldToDateTime = m_toDateTime;
    }

    private void updatePriceValueFormatMask()
    {
      m_priceValueFormatMask = DefaultPriceValueFormatMask;
      if (Instrument != null)
      {
        m_priceValueFormatMask = "0:0"; //need to at least have a value with zero decimals
        
        if (Instrument.PriceDecimals > 0)
        {
          m_priceValueFormatMask += ".";
          for (int i = 0; i < Instrument.PriceDecimals; i++) m_priceValueFormatMask += "0";
        }
      }
    }
  }
}
