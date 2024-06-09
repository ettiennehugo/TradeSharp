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
    private Window m_parentWindow;

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
    public Window ParentWindow { get => m_parentWindow; set { ShowCloseBar = value != null ? Visibility.Visible : Visibility.Collapsed; m_parentWindow = value; } }
    public static readonly DependencyProperty s_showCloseBar = DependencyProperty.Register("ShowCloseBar", typeof(Visibility), typeof(AccountView), new PropertyMetadata(null));
    public Visibility ShowCloseBar { get => (Visibility)GetValue(s_showCloseBar); set => SetValue(s_showCloseBar, value); }

    //methods
    private void m_closeBtn_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
