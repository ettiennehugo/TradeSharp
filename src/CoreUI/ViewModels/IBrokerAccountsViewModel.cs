using TradeSharp.Data;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Concrete interface for the view model for brokers and accounts associated with them.
  /// </summary>
  public interface IBrokerAccountsViewModel: ITreeViewModel<string, object> 
  {
    IBrokerPlugin? BrokerFilter { get; set; }  //filter the accounts down to the specific broker, no filtering if null
  }
}
