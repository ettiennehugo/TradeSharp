using System.Runtime.InteropServices;
using TradeSharp.Common;

namespace TradeSharp.Data
{
  //Exposing components to COM
  //https://learn.microsoft.com/en-us/dotnet/framework/interop/exposing-dotnet-components-to-com
  //https://learn.microsoft.com/en-us/dotnet/core/native-interop/expose-components-to-com
  //https://learn.microsoft.com/en-us/dotnet/standard/native-interop/

  /// <summary>
  /// Interface to be supported by data provider plugins.
  /// </summary>
  [ComVisible(true)]
  [Guid("A7396674-D60B-489C-83C2-39BD6466C0FC")]
  public interface IDataProvider
  {

    //constants


    //enums


    //types


    //attributes


    //properties
    string Name { get; }    //name of the data provider
    IList<string> Tickers { get; }  //set of tickers supported by the data provider
    int ConnectionCountMax { get; } //maximum number of concurrent connections allowed to the data provider
    IPluginConfigurationProfile ConfigurationProfile { get; set; }

    //methods
    void Connect();
    void Disconnect();
    void Create(string config);
    void Destroy();
    void Request(string ticker, DateTime start, DateTime end);

  }
}
