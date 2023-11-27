using TradeSharp.CoreUI.Services;
using TradeSharp.Data;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public override void OnUpdate()
    {
      throw new NotImplementedException();
    }

    //properties
    [ObservableProperty] private Resolution m_resolution;

    //methods



  }
}
