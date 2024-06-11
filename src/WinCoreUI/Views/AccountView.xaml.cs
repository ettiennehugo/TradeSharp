using Microsoft.UI.Xaml;
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
      ParentWindow = null;
      BrokerPlugin = null;    //TBD: This will lead to a crash if not set.
      Account = new EmptyAccount { Name = "No account selected" };
      this.InitializeComponent();
    }

    public AccountView(IBrokerPlugin broker, Account account)
    {
      ParentWindow = null;
      BrokerPlugin = broker;
      Account = account;    // TBD - this needs to be some deep copy or something else.
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Window ParentWindow { get; set; }
    public IBrokerPlugin? BrokerPlugin { get; set; } = null;
    public Account Account { get; set; } = null;

    //methods
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {

      //subscribe to account updates

    }
  }
}
