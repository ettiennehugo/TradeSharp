using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for the service for brokers and accounts associated with them.
  /// </summary>
  public interface IBrokerAccountsService: ITreeItemsService<string, object> 
  {
    IBrokerPlugin? BrokerFilter { get; set; }  //filter the accounts down to the specific broker, null for no filtering
  }
}
