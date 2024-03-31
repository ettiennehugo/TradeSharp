using Microsoft.Extensions.Hosting;
using TradeSharp.Data;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Service to manage plugins used to extend TradeSharp.
  /// </summary>
  public interface IPluginsService: IListService<IPlugin>
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IHost Host { get; set; }

    //methods

  }
}
