using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model for instrument bar data.
  /// </summary>
  public partial class InstrumentBarDataViewModel : ListViewModel<IBarData>
  {
    //constants


    //enums


    //types


    //attributes
    private string m_dataProvider;
    private Resolution m_resolution;
    private Instrument? m_instrument;
    private IInstrumentBarDataService m_barDataService;

    //constructors
    public InstrumentBarDataViewModel(INavigationService navigationService, IDialogService dialogService) : base(Ioc.Default.GetRequiredService<IInstrumentBarDataService>(), navigationService, dialogService) //need to get a transient instance of the service uniquely associated with this view model
    {
      m_barDataService = (IInstrumentBarDataService)m_itemsService;
      Resolution = Resolution.Day;
      DataProvider = string.Empty;
      Instrument = null;
      AddCommand = new RelayCommand(OnAdd, () => DataProvider != string.Empty && Instrument != null); //view model must be keyed correctly before allowing the adding new items
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      DeleteCommandAsync = new AsyncRelayCommand<object?>(OnDeleteAsync, (object? x) => SelectedItem != null);
      ImportCommandAsync = new AsyncRelayCommand(OnImportAsync, () => DataProvider != string.Empty && Instrument != null);
      ExportCommandAsync = new AsyncRelayCommand(OnExportAsync, () => DataProvider != string.Empty && Instrument != null && Items.Count > 0);
      CopyCommandAsync = new AsyncRelayCommand<object?>(OnCopyAsync, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null);
      CopyToHourCommandAsync = new AsyncRelayCommand<object?>(OnCopyToHourAsync, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && Resolution == Resolution.Minute);
      CopyToDayCommandAsync = new AsyncRelayCommand<object?>(OnCopyToDayAsync, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && (Resolution == Resolution.Minute || Resolution == Resolution.Hour));
      CopyToWeekCommandAsync = new AsyncRelayCommand<object?>(OnCopyToWeekAsync, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && (Resolution == Resolution.Minute || Resolution == Resolution.Hour || Resolution == Resolution.Day));
      CopyToMonthCommandAsync = new AsyncRelayCommand<object?>(OnCopyToMonthAsync, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && (Resolution == Resolution.Minute || Resolution == Resolution.Hour || Resolution == Resolution.Day || Resolution == Resolution.Week));
      CopyToAllCommandAsync = new AsyncRelayCommand<object?>(OnCopyToAllAsync, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && Resolution != Resolution.Month);
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

    public override Task OnImportAsync()
    {
      return Task.Run(async () =>
      {
        ImportSettings? importSettings = await m_dialogService.ShowImportBarDataAsync();

        if (importSettings != null)
        {
          ImportResult importResult = m_itemsService.Import(importSettings);
          await m_dialogService.ShowStatusMessageAsync(importResult.Severity, "", importResult.StatusMessage);
        }
      });
    }

    public override Task OnExportAsync()
    {
      return Task.Run(async () =>
      {
        string? filename = await m_dialogService.ShowExportBarDataAsync();

        if (filename != null)
        {
          ExportResult exportResult = m_itemsService.Export(filename);
          await m_dialogService.ShowStatusMessageAsync(exportResult.Severity, "", exportResult.StatusMessage);
        }
      });
    }

    public Task<IList<IBarData>> GetItems(DateTime from, DateTime to)
    {
      return Task.Run(() => { return m_barDataService.GetItems(from, to); });
    }

    public Task<IList<IBarData>> GetItems(int index, int count)
    {
      return Task.Run(() => { return m_barDataService.GetItems(index, count); });
    }

    public Task<IList<IBarData>> GetItems(DateTime from, DateTime to, int index, int count)
    {
      return Task.Run(() => { return m_barDataService.GetItems(from, to, index, count); });
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
        ((IInstrumentBarDataService)m_itemsService).DataProvider = value;
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
        ((IInstrumentBarDataService)m_itemsService).Resolution = value;
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
        ((IInstrumentBarDataService)m_itemsService).Instrument = value;
        if (DataProvider != string.Empty && Instrument != null) RefreshCommandAsync.ExecuteAsync(null);
        NotifyCanExecuteChanged();
      }
    }

    //methods


  }
}
