using System.ComponentModel;

namespace TradeSharp.Data
{
  /// <summary>
  /// Complex order type that can consist of multiple simple orders that can have specific relationships.
  /// </summary>
  public abstract class ComplexOrder : Order
  {
    //constants


    //enums
    /// <summary>
    /// Complex order types supported.
    /// </summary>summary>
    public enum OrderType
    {
      [Description("Order sends order")]
      OSO,
      [Description("Order cancels order")]
      OCO,
      Bracket,
      Fade,
    }

    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties


    //methods

  }
}
