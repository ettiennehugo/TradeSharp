using System.Runtime.InteropServices;

namespace TradeSharp.Common
{

  //Exposing components to COM
  //https://learn.microsoft.com/en-us/dotnet/framework/interop/exposing-dotnet-components-to-com
  //https://learn.microsoft.com/en-us/dotnet/core/native-interop/expose-components-to-com
  //https://learn.microsoft.com/en-us/dotnet/standard/native-interop/


  /// <summary>
  /// Interface to be implemented by data provider plugin's to extend TradeSharp integration into specific data providers.
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
    string Name { get; set; }
    IPluginConfigurationProfile ConfigurationProfile { get; set; }


    //TODO:
    // * Add get-property to return the maximum number of connections allowed, this will be used in the download dialog to control the maximum number of threads to use for the download.


    //methods
    


  }
}
