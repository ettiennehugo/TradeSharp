using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Concrete interface for the view model for exchanges.
  /// </summary>
  public interface IExchangeViewModel: IListViewModel<Exchange> 
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Exchange GlobalExchange { get; }

    //methods
    Exchange? GetItem(Guid id);
  }
}