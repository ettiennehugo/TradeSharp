using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class AccountsView : Page
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public AccountsView()
    {
      Account = new EmptyAccount { Name = "No account selected" };
      this.InitializeComponent();
    }

    public AccountsView(Account account)
    {
      Account = account;
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Window ParentWindow { get; set; } = null;
    public Account Account { get; set; } = null;

    //methods


  }
}
