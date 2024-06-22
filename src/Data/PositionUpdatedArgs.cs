namespace TradeSharp.Data
{
  /// <summary>
  /// Arguments for when an account position is updated.
  /// </summary>
  public class PositionUpdatedArgs: EventArgs
  {
    public PositionUpdatedArgs(Position position) => Position = position;
    public Position Position { get; }
  }
}
