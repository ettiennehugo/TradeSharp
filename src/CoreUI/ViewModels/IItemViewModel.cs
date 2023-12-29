namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// Detail item view model interface.
  /// </summary>
  public interface IItemViewModel<out T>
    {
        T? Item { get; }
    }
}
