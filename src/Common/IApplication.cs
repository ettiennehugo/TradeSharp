namespace TradeSharp.Common
{
  /// <summary>
  /// Interface to support application wide behaviour, service discovery being one of the core functions.
  /// </summary>
  public interface IApplication
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    public static IApplication Current { get; set; }
    IServiceProvider Services { get; }

    //methods
    /// <summary>
    /// Shutdown the services used by the application.
    /// </summary>
    void Shutdown();
  }
}
