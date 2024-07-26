using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Concrete interface for the view model for instruments.
  /// </summary>
  public interface IInstrumentViewModel: IListViewModel<Instrument> 
  {
    public RelayCommand AddStockCommand { get; set; }
    void OnAddStock();
  }
}