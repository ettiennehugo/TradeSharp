using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for the country service.
  /// </summary>
  public interface ICountryService : IListService<Country> 
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    /// <summary>
    /// Finds the associated country for the given exchange.
    /// </summary>
    Country? Find(Exchange exchange);
  }
}
