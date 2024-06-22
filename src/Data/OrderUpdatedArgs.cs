namespace TradeSharp.Data
{
  /// <summary>
  /// Order updated event handler arguments.
  /// </summary>
  public class OrderUpdatedArgs : EventArgs
  {
    public OrderUpdatedArgs(Order order) => Order = order;
    public Order Order { get; }
  }
}
