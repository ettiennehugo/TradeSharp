namespace TradeSharp.Common
{
  /// <summary>
  /// Delegate for plugins that support remote connections when the connection status changes.
  /// </summary>
  public delegate void ConnectionStatusHandler(object sender, ConnectionStatusArgs e);
}
