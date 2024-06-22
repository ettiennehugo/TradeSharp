namespace TradeSharp.Data
{
  /// <summary>
  /// Arguments for when the accounts are updated by a broker.
  /// </summary>
  public class AccountsUpdatedArgs : EventArgs
  {
    public AccountsUpdatedArgs(Account account) => Account = account;
    public Account Account { get; }
  }
}
