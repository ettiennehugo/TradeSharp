using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the list of sessions associated with a parent exhange.
  /// </summary>
  public sealed partial class SessionsView : UserControl
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public SessionsView()
    {
      ViewModel = (SessionViewModel)IApplication.Current.Services.GetService(typeof(SessionViewModel));
      ExchangeViewModel = (ExchangeViewModel)IApplication.Current.Services.GetService(typeof(ExchangeViewModel));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_parentIdProperty = DependencyProperty.Register("ParentId", typeof(Guid), typeof(SessionsView), new PropertyMetadata(null));
    public Guid? ParentId
    {
      get => (Guid?)GetValue(s_parentIdProperty);
      set
      {
        SetValue(s_parentIdProperty, value);
        ViewModel.ParentId = (System.Guid)value;
        ViewModel.RefreshCommand.Execute(null);
      }
    }

    public SessionViewModel ViewModel { get; }
    public ExchangeViewModel ExchangeViewModel { get; }

    //methods
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      m_copyToDayFlyout.Items.Clear();

      foreach (DayOfWeek dayOfWeek in typeof(DayOfWeek).GetEnumValues())
      {
        MenuFlyoutItem menuItem = new MenuFlyoutItem
        {
          Text = dayOfWeek.ToString(),
          Command = ViewModel.CopyCommandAsync,
          CommandParameter = new KeyValuePair<DayOfWeek, IList>(dayOfWeek, m_sessions.SelectedItems)
        };

        m_copyToDayFlyout.Items.Add(menuItem);
      }

      m_copyToExchangeFlyout.Items.Clear();

      foreach (Data.Exchange exchange in ExchangeViewModel.Items)
      {
        MenuFlyoutItem menuItem = new MenuFlyoutItem
        {
          Text = exchange.Name,
          Command = ViewModel.CopyCommandAsync,
          CommandParameter = new KeyValuePair<Guid, IList>(exchange.Id, m_sessions.SelectedItems)
        };

        m_copyToExchangeFlyout.Items.Add(menuItem);
      }
    }
  }
}
