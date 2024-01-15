namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Event arguments for the RefreshEvent, curretly do not require anything special.
  /// </summary>
  public class RefreshEventArgs : EventArgs 
  {
    //constants
    public static readonly new RefreshEventArgs Empty = new RefreshEventArgs();

    //enums


    //types


    //attributes


    //constructors
    public RefreshEventArgs() : base() { }

    //finalizers


    //interface implementations


    //properties


    //methods


  }

  /// <summary>
  /// Interface to be implemented by services/view models that support refreshing of data.
  /// </summary>
  public interface IRefreshable
  {
    //constants


    //enums


    //types


    //attributes


    //properties


    //events
    public delegate void RefreshEventHandler(object? sender, RefreshEventArgs e);

    /// <summary>
    /// Object was changed, any external views/services/view models need to refresh their data.
    /// </summary>
    public event RefreshEventHandler? RefreshEvent;

    //methods



  }
}
