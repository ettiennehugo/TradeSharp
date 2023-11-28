using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private IInstrumentBarDataService m_service;

    //constructors
    public InstrumentBarDataViewModel(IInstrumentBarDataService service, INavigationService navigationService, IDialogService dialogService) : base(service, navigationService, dialogService)
    {
      m_service = service;
      Resolution = m_service.Resolution;
      AddCommand = new RelayCommand(OnAdd, () => DataProvider != "" && Instrument != null && Ticker != ""); //view model must be keyed correctly before adding new items
      UpdateCommand = new RelayCommand(OnUpdate, () => SelectedItem != null);
      DeleteCommand = new RelayCommand<object?>(OnDelete, (object? x) => SelectedItem != null);
      CopyCommand = new RelayCommand<object?>(OnCopy, (object? x) => SelectedItem != null);
    }

    //finalizers


    //interface implementations
    public async override void OnAdd()
    {
      IBarData? barData = await m_dialogService.ShowCreateBarDataAsync(Resolution);
      if (barData != null)
      {
        barData.Resolution = Resolution;
        await m_service.AddAsync(barData);
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
          await m_service.UpdateAsync(updatedBar);
          await OnRefreshAsync();
        }
      }
    }

    //properties
    public string DataProvider { get => m_service.DataProvider; set => m_service.DataProvider = value; }
    public Resolution Resolution { get => m_service.Resolution; set => m_service.Resolution = value; }
    public Instrument? Instrument { get => m_service.Instrument; set => m_service.Instrument = value; }
    public string Ticker { get => m_service.Ticker; set => m_service.Ticker= value; }
    public DateTime Start{ get => m_service.Start; set => m_service.Start= value; }
    public DateTime End { get => m_service.End ; set => m_service.End = value; }
    public PriceDataType PriceDataType { get => m_service.PriceDataType; set => m_service.PriceDataType = value; }

    //methods


  }
}
