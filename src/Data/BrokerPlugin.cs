using System.Runtime.InteropServices;
using TradeSharp.Common;
using Microsoft.Extensions.Hosting;

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


    //constructors
    public BrokerPlugin(string name): base(name)  { }

    //finalizers


    //interface implementations


    //properties
    public abstract IList<Account> Accounts { get; }

    //methods


  }
}
