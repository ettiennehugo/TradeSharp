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

    //constructors
    public InstrumentBarDataViewModel(INavigationService navigationService, IDialogService dialogService) : base(Ioc.Default.GetRequiredService<IInstrumentBarDataService>(), navigationService, dialogService) //need to get a transient instance of the service uniquely associated with this view model
    {
      Resolution = Resolution.Day;
      DataProvider = string.Empty;
      Instrument = null;
      AddCommand = new RelayCommand(OnAdd, () => DataProvider != string.Empty && Instrument != null); //view model must be keyed correctly before allowing the adding new items
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      ImportCommand = new RelayCommand(OnImport, () => DataProvider != string.Empty && Instrument != null);
      ExportCommand = new RelayCommand(OnExport, () => DataProvider != string.Empty && Instrument != null && Items.Count > 0);
      CopyCommand = new RelayCommand<object?>(OnCopy, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null);
      CopyToSyntheticCommand = new RelayCommand<object?>(OnCopyToSynthetic, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null);
      CopyToActualCommand = new RelayCommand<object?>(OnCopyToActual, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null);
      CopyToHourCommand = new RelayCommand<object?>(OnCopyToHour, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && Resolution == Resolution.Minute);
      CopyToDayCommand = new RelayCommand<object?>(OnCopyToDay, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && (Resolution == Resolution.Minute || Resolution == Resolution.Hour));
      CopyToWeekCommand = new RelayCommand<object?>(OnCopyToWeek, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && (Resolution == Resolution.Minute || Resolution == Resolution.Hour || Resolution == Resolution.Day));
      CopyToMonthCommand = new RelayCommand<object?>(OnCopyToMonth, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && (Resolution == Resolution.Minute || Resolution == Resolution.Hour || Resolution == Resolution.Day || Resolution == Resolution.Week));
      CopyToAllCommand = new RelayCommand<object?>(OnCopyToAll, (object? x) => DataProvider != string.Empty && Instrument != null && SelectedItem != null && Resolution != Resolution.Month);
  }

  //finalizers


  //interface implementations
  public async override void OnAdd()
    {
      IBarData? newBar = await m_dialogService.ShowCreateBarDataAsync(Resolution, DateTime.Now, PriceDataType.Merged);
      if (newBar != null)
      {
        newBar.Resolution = Resolution;
        await m_itemsService.AddAsync(newBar);
        SelectedItem = newBar;
      }
    }

    public async override void OnUpdate()
    {
      if (SelectedItem != null)
      {
        var updatedBar = await m_dialogService.ShowUpdateBarDataAsync(SelectedItem);
        if (updatedBar != null)
        {
          await m_itemsService.UpdateAsync(updatedBar);
          SelectedItem = updatedBar;
        }
      }
    }

    public override async void OnImport()
    {
      ImportSettings? importSettings = await m_dialogService.ShowImportBarDataAsync();

      if (importSettings != null)
      {
        ImportReplaceResult importResult = await m_itemsService.ImportAsync(importSettings);
        await m_dialogService.ShowStatusMessageAsync(importResult.Severity, "", $"Import result - Created({importResult.Created}), Skipped({importResult.Skipped}), Updated({importResult.Updated}), Replaced({importResult.Replaced})");
        await OnRefreshAsync();
      }
    }

    public override async void OnExport()
    {
      string? filename = await m_dialogService.ShowExportBarDataAsync();

      if (filename != null)
      {
        long exportCount = await m_itemsService.ExportAsync(filename);
        await m_dialogService.ShowStatusMessageAsync(exportCount == 0 ? IDialogService.StatusMessageSeverity.Warning : IDialogService.StatusMessageSeverity.Success, "", $"Exported {exportCount} instruments");
      }
    }

    public virtual void OnCopyToSynthetic(object? selection)
    {
      //TODO: Copy selected bar data as synthetic bars
      throw new NotImplementedException();
    }

    public virtual void OnCopyToActual(object? selection)
    {
      //TODO: Copy selected bar data as actual bars
      throw new NotImplementedException();
    }

    public virtual void OnCopyToHour(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual void OnCopyToDay(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual void OnCopyToWeek(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual void OnCopyToMonth(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    public virtual void OnCopyToAll(object? selection)
    {
      //TODO
      throw new NotImplementedException();
    }

    //properties
    public RelayCommand<object?> CopyToSyntheticCommand { get; internal set; }
    public RelayCommand<object?> CopyToActualCommand { get; internal set; }
    public RelayCommand<object?> CopyToHourCommand { get; internal set; }
    public RelayCommand<object?> CopyToDayCommand { get; internal set; }
    public RelayCommand<object?> CopyToWeekCommand { get; internal set; }
    public RelayCommand<object?> CopyToMonthCommand { get; internal set; }
    public RelayCommand<object?> CopyToAllCommand { get; internal set; }

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
