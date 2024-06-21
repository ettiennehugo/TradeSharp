using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Common;
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
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public Window ParentWindow { get; set; } = null;
    public IBrokerAccountsViewModel ViewModel { get; set; } = null;

    public static readonly DependencyProperty AccountProperty = DependencyProperty.Register(name: "Account", propertyType: typeof(Account), ownerType: typeof(AccountsView), typeMetadata: null);
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
      ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
      ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == PropertyName.SelectedNode)
      {
        if (ViewModel.SelectedNode?.Item is Account account)
        {    
          //change the account view based on the new account
          if (m_accountView?.Account != account)
            m_main.Children.Remove(m_accountView);

          m_accountView = new AccountView(null, account);
          Grid.SetColumn(m_accountView, 1);
          m_main.Children.Add(m_accountView);
        }
        else if (m_accountView != null)
          {
            m_main.Children.Remove(m_accountView);
            m_accountView = null;
          }
      }
    }
  }
}
