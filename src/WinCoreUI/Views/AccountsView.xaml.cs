using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the set of brokers with their associated accounts if possible (brokers can require connections to be established).
  /// </summary>
  public sealed partial class AccountsView : UserControl
  {
    //constants


    //enums


    //types


    //attributes
    private AccountView m_accountView = null;

    //constructors
    public AccountsView()
    {
      ViewModel = (IBrokerAccountsViewModel)IApplication.Current.Services.GetService(typeof(IBrokerAccountsViewModel));
      ViewModel.BrokerFilter = null;    //make sure we reset the broker account filter
      this.InitializeComponent();
    }

    public AccountsView(IBrokerPlugin brokerPlugin)
    {
      ViewModel = (IBrokerAccountsViewModel)IApplication.Current.Services.GetService(typeof(IBrokerAccountsViewModel));
      BrokerPlugin = brokerPlugin;
      ViewModel.BrokerFilter = brokerPlugin;
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Window ParentWindow { get; set; } = null;
    public IBrokerAccountsViewModel ViewModel { get; set; } = null;
    public static readonly DependencyProperty AccountProperty = DependencyProperty.Register(name: "Account", propertyType: typeof(Account), ownerType: typeof(AccountsView), typeMetadata: null);
    public IBrokerPlugin BrokerPlugin { get; set; } = null;
    public Account Account
    {
      get => (Account)GetValue(AccountProperty);
      set => SetValue(AccountProperty, value);
    }

    //methods
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Nodes.Count == 0)
        ViewModel.RefreshCommand.Execute(null);

      //if we filter accounts by broker, set the selected item delegate
      //NOTE: This selection change does not fire in time when brokers are shown so for that case we purely use the ViewModel.SelectedNode property
      if (BrokerPlugin != null)
        m_brokerAccountsTree.SelectionChanged += m_brokerAccountsTree_SelectionChanged;
      else
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
      if (BrokerPlugin == null) ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == PropertyName.SelectedNode)
      {
        if (ViewModel.SelectedNode?.Item is Account account)
          selectAccount(account);
        else
          selectAccount(null);
      }
    }

    private void selectAccount(Account? account)
    {
      if (m_accountView == null && account == null)    //no account to show
        return;

      if (m_accountView == null)
      {
        m_accountView = new AccountView(null, account);
        Grid.SetColumn(m_accountView, 1);
        m_main.Children.Add(m_accountView);
      }
      else if (m_accountView.Account != account)
      {
        m_main.Children.Remove(m_accountView);

        if (account != null)
        {
          m_accountView = new AccountView(null, account);
          Grid.SetColumn(m_accountView, 1);
          m_main.Children.Add(m_accountView);
        }
        else
        {
          m_main.Children.Remove(m_accountView);
          m_accountView = null;
        }
      }
    }

    private void m_brokerAccountsTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
      BrokerAccountsNodeType? accountNode = (BrokerAccountsNodeType?)sender.SelectedItem;
      selectAccount((Account)accountNode.Item);
    }
  }
}
