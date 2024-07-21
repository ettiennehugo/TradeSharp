namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Generic service interface to allow navigation between views on different platforms. Do use the provided constants in the implementing classes and raise and exception if some unknown
  /// page is passed into the NavigateToAsync method.
  /// </summary>


  //TBD: Remove this interface if it is not used.


  public interface INavigationService
  {
    //constants
    /// <summary>
    /// Pages to be supported by the DataManager UI's.
    /// </summary>
    public struct DataManager
    {
      public const string Brokers = "DataManager.Brokers";
      public const string DataProviders = "DataManager.DataProviders";
      public const string Extensions = "DataManager.Extensions";
      public const string Countries = "DataManager.Countries";
      public const string Exchanges = "DataManager.Exchanges";
      public const string Fundamentals = "DataManager.Fundamentals";
      public const string Instruments = "DataManager.Instruments";
      public const string InstrumentGroups = "DataManager.InstrumentGroups";
      public const string FundamentalData = "DataManager.FundamentalData";
      public const string InstrumentData = "DataManager.InstrumentData";
      public const string TaskScheduling = "DataManager.TaskScheduling";
      public const string Settings = "Settings";  //must be set to this value used by the NavigationView
    }

    //enums


    //types


    //attributes


    //properties
    string CurrentPage { get; }
    bool UseNavigation { get; set; }

    //methods
    Task NavigateToAsync(string pageName);
    Task GoBackAsync();
  }
}
