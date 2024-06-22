namespace TradeSharp.Data
{
  /// <summary>
  /// Arguments for when the accounts are updated by a broker.
  /// </summary>
  public class AccountUpdatedArgs : EventArgs
  {
    public AccountUpdatedArgs(Account account) => Account = account;
    public Account Account { get; }
  }
}
