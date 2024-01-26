using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using TradeSharp.Common;
using CommunityToolkit.Mvvm.Input;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for instrument bar data.
  /// </summary>
  public partial class InstrumentBarDataViewModel : ListViewModel<IBarData>
  {
    //constants
    /// <summary>
    /// Supported filter fields for the instrument bar data service.
    /// </summary>
    public const string FilterFromDateTime = "FromDateTime";
    public const string FilterToDateTime = "ToDateTime";
    public const string DefaultPriceValueFormatMask = "0:0.00";

    //enums


    //types


    //attributes
    public static DateTime s_defaultStartDateTime = new DateTime(1900, 1, 1, 0, 0, 0);
    public static DateTime s_defaultEndDateTime = new DateTime(2100, 12, 30, 23, 59, 00);
    protected string m_dataProvider;
    protected Resolution m_resolution;
    protected Instrument? m_instrument;
    protected IInstrumentBarDataService m_barDataService;
    protected ILogger<InstrumentBarDataViewModel> m_logger;
    protected DateTime m_fromDateTime;
    protected DateTime m_toDateTime;
    protected Dictionary<string, object> m_filter;
    protected string m_priceValueFormatMask;

    //constructors
    public InstrumentBarDataViewModel(IInstrumentBarDataService itemService, INavigationService navigationService, IDialogService dialogService, ILogger<InstrumentBarDataViewModel> logger) : base(itemService, navigationService, dialogService) //need to get a transient instance of the service uniquely associated with this view model
    {
      m_barDataService = (IInstrumentBarDataService)m_itemsService;
      m_logger = logger;
      m_barDataService.Resolution = Resolution; //need to always keep the service resolution the same as the view model resolution
      m_barDataService.RefreshEvent += onServiceRefresh;
      m_dataProvider = string.Empty;
      m_filter = new Dictionary<string, object>();
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
      return Task.Run(() => OnRefresh());
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
        if (DataProvider != string.Empty && Instrument != null && Count > 0) RefreshCommandAsync.ExecuteAsync(null);
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
        if (DataProvider != string.Empty && Instrument != null && Count > 0) RefreshCommandAsync.ExecuteAsync(null);
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
        if (DataProvider != string.Empty && Instrument != null && Count > 0) RefreshCommandAsync.ExecuteAsync(null);
        NotifyCanExecuteChanged();
      }
    }

    public int Count
    {
      get
      {
        updateFilters();
        return isKeyed() ? m_barDataService.GetCount(m_fromDateTime, m_toDateTime) : 0;
      }
    }

    public IDictionary<string, object> Filter { get => m_filter; set => m_filter = (Dictionary<string, object>)value; }
    public string PriceValueFormatMask { get => m_priceValueFormatMask; } //string.Format value format mask for the price values based on Instrument

    //methods
    protected virtual bool isKeyed()
    {
      return DataProvider != string.Empty && Instrument != null;
    }

    protected virtual void updateFilters()
    {
      m_fromDateTime = m_filter.ContainsKey(FilterFromDateTime) ? (DateTime)m_filter[FilterFromDateTime] : DateTime.MinValue;
      m_toDateTime = m_filter.ContainsKey(FilterToDateTime) ? (DateTime)m_filter[FilterToDateTime] : DateTime.MinValue;
    }

    protected void updatePriceValueFormatMask()
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
