using TradeSharp.Data;

namespace TradeSharp.InteractiveBrokers
{
  /// <summary>
  /// 
  /// </summary>
  public interface IInteractiveBrokersPlugin: IPlugin
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    bool IsConnected { get; }                           //is the plugin connected to the remote service (returns true if no connection is used)

    //deletagates
    event EventHandler? Connected;                      //event raised when the plugin connects to the remote service
    event EventHandler? Disconnected;                   //event raised when the plugin disconnects from the remote service

    //methods
    Task OnConnectAsync();
    Task OnDisconnectAsync();
  }
}
