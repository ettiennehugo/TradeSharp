using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Concrete interface for the view model for brokers and accounts associated with them.
  /// </summary>
  public interface IBrokerAccountsViewModel: ITreeViewModel<string, object> 
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    bool KeepAccountsOnDisconnect { get; set; } //keep accounts on disconnect, will refresh them when a connection is established again
    IBrokerPlugin? BrokerFilter { get; set; }   //filter the accounts down to the specific broker, null for no filtering
  }
}
