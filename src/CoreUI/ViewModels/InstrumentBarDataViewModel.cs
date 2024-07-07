using Microsoft.Extensions.Logging;
using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.Input;
using TradeSharp.Common;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for instrument bar data.
  /// </summary>
  public partial class InstrumentBarDataViewModel : ListViewModel<IBarData>, IInstrumentBarDataViewModel
  {
    //constants


    //enums


    //types


    //attributes
    protected string m_dataProvider;
    protected Resolution m_resolution;
    protected Instrument? m_instrument;
    protected IInstrumentBarDataService m_barDataService;
    protected ILogger<InstrumentBarDataViewModel> m_logger;
    protected DateTime m_fromDateTime;
    protected DateTime m_toDateTime;
    protected Dictionary<string, object> m_filter;

    //constructors
    public InstrumentBarDataViewModel(IInstrumentBarDataService itemService, INavigationService navigationService, IDialogService dialogService, ILogger<InstrumentBarDataViewModel> logger) : base(itemService, navigationService, dialogService) //need to get a transient instance of the service uniquely associated with this view model
    {
      m_barDataService = (IInstrumentBarDataService)m_itemsService;
      m_logger = logger;
      m_barDataService.Resolution = Resolution; //need to always keep the service resolution the same as the view model resolution
      m_barDataService.RefreshEvent += onServiceRefresh;
      m_dataProvider = string.Empty;
      m_filter = new Dictionary<string, object>();
      Resolution = Resolution.Days;
      DataProvider = string.Empty;
      Instrument = null;
      AddCommand = new RelayCommand(OnAdd, () => DataProvider != string.Empty && Instrument != null); //view model must be keyed correctly before allowing the adding new items
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null);
      ImportCommandAsync = new AsyncRelayCommand(OnImportAsync, () => DataProvider != string.Empty && Instrument != null);
      ExportCommandAsync = new AsyncRelayCommand(OnExportAsync, () => DataProvider != string.Empty && Instrument != null);
      CopyCommandAsync = new AsyncRelayCommand<object?>(OnCopyAsync, (object? x) => DataProvider != string.Empty && Instrument != null && Count > 0);
      CopyToHourCommandAsync = new AsyncRelayCommand(OnCopyToHourAsync, () => DataProvider != string.Empty && Instrument != null && Count > 0 && Resolution == Resolution.Minutes);
      CopyToDayCommandAsync = new AsyncRelayCommand(OnCopyToDayAsync, () => DataProvider != string.Empty && Instrument != null && Count > 0 && (Resolution == Resolution.Minutes || Resolution == Resolution.Hours));
      CopyToWeekCommandAsync = new AsyncRelayCommand(OnCopyToWeekAsync, () => DataProvider != string.Empty && Instrument != null && Count > 0 && (Resolution == Resolution.Minutes || Resolution == Resolution.Hours || Resolution == Resolution.Days));
      CopyToMonthCommandAsync = new AsyncRelayCommand(OnCopyToMonthAsync, () => DataProvider != string.Empty && Instrument != null && Count > 0 && (Resolution == Resolution.Minutes || Resolution == Resolution.Hours || Resolution == Resolution.Days || Resolution == Resolution.Weeks));
      CopyToAllCommandAsync = new AsyncRelayCommand(OnCopyToAllAsync, () => DataProvider != string.Empty && Instrument != null && Count > 0 && Resolution != Resolution.Months);
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
      ExportSettings? exportSettings = await m_dialogService.ShowExportBarDataAsync();
      if (exportSettings != null) _ = Task.Run(() => m_itemsService.Export(exportSettings));
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

    /// <summary>
    /// The copy functions will fill in the intermediate timeframes if the selected resolution is not
    /// incrementally higher than the current resolution.
    /// </summary>
    public virtual Task OnCopyToHourAsync()
    {
      return Task.Run(() => m_barDataService.Copy(Resolution.Minutes));
    }

    public virtual Task OnCopyToDayAsync()
    {
      return Task.Run(() =>
      {
        if (Resolution == Resolution.Minutes) m_barDataService.Copy(Resolution.Minutes);
        m_barDataService.Copy(Resolution.Hours);
      });
    }

    public virtual Task OnCopyToWeekAsync()
    {
      return Task.Run(() =>
      {
        if (Resolution == Resolution.Minutes) m_barDataService.Copy(Resolution.Minutes);
        if (Resolution == Resolution.Hours) m_barDataService.Copy(Resolution.Hours);
        m_barDataService.Copy(Resolution.Days);
      });
    }

    public virtual Task OnCopyToMonthAsync()
    {
      return Task.Run(() =>
      {
        if (Resolution == Resolution.Minutes) m_barDataService.Copy(Resolution.Minutes);
        if (Resolution == Resolution.Hours) m_barDataService.Copy(Resolution.Hours);
        if (Resolution == Resolution.Days) m_barDataService.Copy(Resolution.Days);
        m_barDataService.Copy(Resolution.Weeks);
      });
    }

    public virtual Task OnCopyToAllAsync()
    {
      return Task.Run(() =>
      {
        switch (Resolution)
        {
          case Resolution.Minutes:
            m_barDataService.Copy(Resolution.Minutes);
            m_barDataService.Copy(Resolution.Hours);
            m_barDataService.Copy(Resolution.Days);
            m_barDataService.Copy(Resolution.Weeks);
            break;
          case Resolution.Hours:
            m_barDataService.Copy(Resolution.Hours);
            m_barDataService.Copy(Resolution.Days);
            m_barDataService.Copy(Resolution.Weeks);
            break;
          case Resolution.Days:
            m_barDataService.Copy(Resolution.Days);
            m_barDataService.Copy(Resolution.Weeks);
            break;
          case Resolution.Weeks:
            m_barDataService.Copy(Resolution.Weeks);
            break;
        }
      });
    }

    //properties
    public AsyncRelayCommand CopyToHourCommandAsync { get; internal set; }
    public AsyncRelayCommand CopyToDayCommandAsync { get; internal set; }
    public AsyncRelayCommand CopyToWeekCommandAsync { get; internal set; }
    public AsyncRelayCommand CopyToMonthCommandAsync { get; internal set; }
    public AsyncRelayCommand CopyToAllCommandAsync { get; internal set; }

    public string DataProvider
    {
      get => m_dataProvider;
      set
      {
        SetProperty(ref m_dataProvider, value);
        m_barDataService.DataProvider = value;
        RefreshCommandAsync.ExecuteAsync(null);
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
        RefreshCommandAsync.ExecuteAsync(null);
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
        RefreshCommandAsync.ExecuteAsync(null);
        NotifyCanExecuteChanged();
      }
    }

    public int Count
    {
      get
      {
        int result = isKeyed() ? m_barDataService.GetCount(m_fromDateTime, m_toDateTime) : 0;
        if (Debugging.InstrumentBarDataLoadAsync) m_logger.LogInformation($"Returned bar data count: {result}");
        return result;
      }
    }

    public DateTime FromDateTime
    {
      get => m_fromDateTime;
      set => m_fromDateTime = value;
    }

    public DateTime ToDateTime
    {
      get => m_toDateTime;
      set => m_toDateTime = value;
    }

    public string PriceFormatMask { get => m_barDataService.PriceFormatMask; }

    //methods
    protected virtual bool isKeyed()
    {
      return DataProvider != string.Empty && Instrument != null;
    }
  }
}
