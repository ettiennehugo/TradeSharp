using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
      ParentWindow = null;
      Account = new EmptyAccount { Name = "No account selected" };
      this.InitializeComponent();
    }

    public AccountView(Account account)
    {
      ParentWindow = null;
      Account = account;
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Account Account { get; set; } = null;
    public Window ParentWindow { get; set; }

    //methods


  }
}
