using System.Collections.ObjectModel;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Concrete interface for the view model for instruments.
  /// </summary>
  public interface IInstrumentViewModel: IListViewModel<Instrument> 
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    ObservableCollection<Instrument> SelectedItems { get; set; }

    //methods


  }
}