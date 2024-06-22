using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  /// <summary>
  /// Interface for broker plugins to implement.
  /// </summary>
  [ComVisible(true)]
  [Guid("C20C3B05-70B6-4766-BC3C-A19B6DA07174")]
  public interface IBrokerPlugin : IPlugin
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    ObservableCollection<Account> Accounts { get; }

    //events
    event AccountsUpdatedHandler? AccountsUpdated;    //accounts added/removed or account values were changed (this does NOT include position or order updates)
    event PositionUpdatedHandler? PositionUpdated;    //position values were changed
    event OrderUpdatedHandler? OrderUpdated;          //order values were changed

    //methods


  }
}