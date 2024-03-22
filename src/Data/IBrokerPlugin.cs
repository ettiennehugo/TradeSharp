using System.Runtime.InteropServices;

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
    IList<Account> Accounts { get; }

    //methods



  }
}
