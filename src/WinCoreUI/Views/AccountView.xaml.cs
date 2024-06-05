using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// User control to show account details with it's associated positions and orders.
  /// </summary>
  public sealed partial class AccountView : UserControl
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public AccountView()
    {
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Account Account { get; set; }

    //methods



  }
}
