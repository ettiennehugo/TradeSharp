namespace TradeSharp.Common
{
  /// <summary>
  /// General utilities used throughout the application.
  /// </summary>
  public class Utilities
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties


    //methods
    public static string SafeFileName(string fileName)
    {
      return fileName.Replace(":", "_").Replace("/", "_").Replace("\\", "_").Replace(" ", "_");
    }
  }
}
