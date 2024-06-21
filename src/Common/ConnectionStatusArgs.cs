namespace TradeSharp.Common
{
  /// <summary>
  /// Plugin connection status change arguments.
  /// </summary>
  public class ConnectionStatusArgs
  {
    public ConnectionStatusArgs(bool isConnected)
    {
      IsConnected = isConnected;
    }

    public bool IsConnected { get; internal set;}
  }
}
