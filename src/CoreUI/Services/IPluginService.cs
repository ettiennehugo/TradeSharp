using Microsoft.Extensions.Hosting;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Service to manage plugins used to extend TradeSharp.
  /// </summary>
  public interface IPluginService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IList<IPlugin> Plugins { get; }

    //methods
    void LoadPlugins(IHost host);
  }
}
