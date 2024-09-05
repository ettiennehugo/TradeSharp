using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for exchange service.
  /// </summary>
  public interface IExchangeService : IListService<Exchange> {
    //constants


    //enums


    //types


    //attributes


    //properties


    //methods
    /// <summary>
    /// Find the exchange associated with a given instrument, per default include search for the secondary exchanges.
    /// </summary>
    Exchange? Find(Instrument instrument, bool includeSecondaryExchange = true);
  }
}
