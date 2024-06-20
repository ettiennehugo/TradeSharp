using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using TradeSharp.CoreUI.Common;
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
    public Account Account { get; set; } = null;

    //methods
    private void m_brokerAccountsTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
      if (ViewModel.SelectedNode.Item is Account account)
        Account = account;
      else
        Account = null;
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Nodes.Count == 0)
        ViewModel.RefreshCommand.Execute(null);
      ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == ITreeViewModel<string,object>.PropertySelectedNode)
      {
        if (ViewModel.SelectedNode.Item is Account account)
          Account = account;
        else
          Account = null;
      }
    }
  }
}
