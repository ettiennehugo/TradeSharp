namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Represents the state of a service/view model that is being loaded from the underlying repository.
  /// </summary>
  public enum LoadedState
  {
    NotLoaded,    //initial state - object refresh has not been run
    Loading,      //refresh is in progress
    Loaded,       //refresh is complete
    Error         //error occurred during refresh - a retry might resolve it
  }
}
