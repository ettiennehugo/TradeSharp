using System.Runtime.InteropServices;
using TradeSharp.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Data
{
  /// <summary>
  /// Base class for broker plugins.
  /// </summary>
  [ComVisible(true)]
  [Guid("E7E2167A-8EDA-46C4-B610-F989A1DCB840")]
  public abstract class BrokerPlugin : Plugin, IBrokerPlugin
  {
    //constants


    //enums


    //types


    //attributes
    protected List<Account> m_accounts;

    //constructors
    public BrokerPlugin(string name): base(name) 
    {
      m_accounts = new List<Account>();    
    }

    //finalizers


    //interface implementations


    //properties
    public IList<Account> Accounts { get => m_accounts; }

    //methods


  }
}
