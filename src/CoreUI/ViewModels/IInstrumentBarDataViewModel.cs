using CommunityToolkit.Mvvm.Input;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Concrete interface for instrument bar data vewi model.
  /// </summary>
  public interface IInstrumentBarDataViewModel: IListViewModel<IBarData>
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    string DataProvider { get; set; }
    Instrument? Instrument { get; set; }
    Resolution Resolution { get; set; }
    int Count { get; }
    DateTime FromDateTime { get; set; }
    DateTime ToDateTime { get; set; }
    string PriceValueFormatMask { get; }

    AsyncRelayCommand CopyToAllCommandAsync { get; }
    AsyncRelayCommand CopyToDayCommandAsync { get; }
    AsyncRelayCommand CopyToHourCommandAsync { get; }
    AsyncRelayCommand CopyToMonthCommandAsync { get; }
    AsyncRelayCommand CopyToWeekCommandAsync { get; }

    //methods
    Task OnCopyToAllAsync();
    Task OnCopyToDayAsync();
    Task OnCopyToHourAsync();
    Task OnCopyToMonthAsync();
    Task OnCopyToWeekAsync();
    Task<IList<IBarData>> GetItems(DateTime from, DateTime to);
    Task<IList<IBarData>> GetItems(DateTime from, DateTime to, int index, int count);
    Task<IList<IBarData>> GetItems(int index, int count);
  }
}