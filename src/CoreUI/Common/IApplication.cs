namespace TradeSharp.CoreUI.Common
{
  /// <summary>
  /// Interface to support application wide behaviour.
  /// </summary>
  public interface IApplication
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    IServiceProvider Services { get; }

    //methods
    public static IApplication Current { get; set; }

  }
}
