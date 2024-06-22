namespace TradeSharp.Data
{
  /// <summary>
  /// Delegate when an account is updated by the broker. NOTE: This does not include positions and orders in the account, separate handlers are defined for these.
  /// </summary>
  public delegate void AccountUpdatedHandler(object sender, AccountUpdatedArgs args);
}
