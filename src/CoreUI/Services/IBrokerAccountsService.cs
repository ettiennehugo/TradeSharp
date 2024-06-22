using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Concrete interface for the service for brokers and accounts associated with them.
  /// </summary>
  public interface IBrokerAccountsService: ITreeItemsService<string, object> 
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    bool KeepAccountsOnDisconnect { get; set; } //keep accounts on disconnect, will refresh them when a connection is established again
    IBrokerPlugin? BrokerFilter { get; set; }   //filter the accounts down to the specific broker, null for no filtering

    //methods
    void Refresh(IBrokerPlugin broker);   //refresh a specific broker's accounts
  }
}
