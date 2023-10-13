using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TradeSharp.CoreUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.Services;

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
      this.InitializeComponent();
      ViewModel = Ioc.Default.GetRequiredService<SessionViewModel>();
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
      }
    }

    public SessionViewModel ViewModel { get; }



    //TODO: Implement the logic to copy sessions between days and optionally between Exchanges.


    //methods
    private void copySessionClick(object sender, RoutedEventArgs e)
    {
      if ((MenuFlyoutItem)sender == m_menuItemMonday)
        copySessions(DayOfWeek.Monday);
      else if ((MenuFlyoutItem)sender == m_menuItemTuesday)
        copySessions(DayOfWeek.Tuesday);
      else if ((MenuFlyoutItem)sender == m_menuItemWednesday)
        copySessions(DayOfWeek.Wednesday);
      else if ((MenuFlyoutItem)sender == m_menuItemThursday)
        copySessions(DayOfWeek.Thursday);
      else if ((MenuFlyoutItem)sender == m_menuItemFriday)
        copySessions(DayOfWeek.Friday);
      else if ((MenuFlyoutItem)sender == m_menuItemSaturday)
        copySessions(DayOfWeek.Saturday);
      else if ((MenuFlyoutItem)sender == m_menuItemSunday)
        copySessions(DayOfWeek.Sunday);
    }

    private void copySessions(DayOfWeek dayOfWeek)
    {
      
      foreach (var session in m_sessions.SelectedItems)
      {
                  
      }
    }
  }
}
