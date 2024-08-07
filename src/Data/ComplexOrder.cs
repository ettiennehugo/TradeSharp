using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TradeSharp.Data
{
  /// <summary>
  /// Complex order type that can consist of multiple simple orders that can have specific relationships.
  /// </summary>
  [ComVisible(true)]
  [Guid("70A8C5EE-0A95-4193-BA07-D3F3B84BF3DB")]
  public abstract partial class ComplexOrder : Order
  {
    //constants


    //enums
    /// <summary>
    /// Complex order types supported.
    /// </summary>summary>
    public enum OrderType
    {
      None,
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
    public ComplexOrder(Account account, Instrument instrument) : base(account, instrument) 
    {
      Type = OrderType.None;
    }

    //finalizers


    //interface implementations


    //properties
    [ObservableProperty] OrderType m_type;

    //methods

  }
}
