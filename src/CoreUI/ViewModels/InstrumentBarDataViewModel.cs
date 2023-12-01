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
    private DateTime m_start;
    private DateTime m_end;
    private PriceDataType m_priceDataType;

    //constructors
    public InstrumentBarDataViewModel(IInstrumentBarDataService service, INavigationService navigationService, IDialogService dialogService) : base(service, navigationService, dialogService)
    {
      Resolution = Resolution.Day;
      DataProvider = string.Empty;
      Instrument = null;
      Start = DateTime.MinValue;
      End = DateTime.MaxValue;
      PriceDataType = PriceDataType.Both;
      AddCommand = new RelayCommand(OnAdd, () => DataProvider != string.Empty && Instrument != null); //view model must be keyed correctly before allowing the adding new items
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      CopyCommand = new RelayCommand<object?>(OnCopy, (object? x) => SelectedItem != null);
    }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      IBarData? barData = await m_dialogService.ShowCreateBarDataAsync(Resolution, DateTime.Now, PriceDataType == PriceDataType.Synthetic);
      if (barData != null)
      {
        barData.Resolution = Resolution;
        await m_itemsService.AddAsync(barData);
        SelectedItem = barData;
        Items.Add(barData);
        await OnRefreshAsync();
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
          await OnRefreshAsync();
        }
      }
    }

    //properties
    public string DataProvider
    {
      get => m_dataProvider;
      set
      {
        SetProperty(ref m_dataProvider, value);
        ((IInstrumentBarDataService)m_itemsService).DataProvider = value;
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
        NotifyCanExecuteChanged();
      }
    }

    public DateTime Start
    {
      get => m_start;
      set
      {
        SetProperty(ref m_start, value);
        ((IInstrumentBarDataService)m_itemsService).Start = value;
        NotifyCanExecuteChanged();
      }
    }

    public DateTime End
    {
      get => m_end;
      set
      {
        SetProperty(ref m_end, value);
        ((IInstrumentBarDataService)m_itemsService).End = value;
        NotifyCanExecuteChanged();
      }
    }

    public PriceDataType PriceDataType
    {
      get => m_priceDataType;
      set
      {
        SetProperty(ref m_priceDataType, value);
        ((IInstrumentBarDataService)m_itemsService).PriceDataType = value;
        NotifyCanExecuteChanged();
      }
    }

    //methods


  }
}
