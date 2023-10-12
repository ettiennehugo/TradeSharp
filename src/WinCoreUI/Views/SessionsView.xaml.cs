using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TradeSharp.CoreUI.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;

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
    public bool Editable { get; set; }

    private void m_sessions_AutoGeneratingColumn(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridAutoGeneratingColumnEventArgs e)
    {
      if (e.Column.Header.ToString() == "Id")
        e.Cancel = true;
      else if (e.Column.Header.ToString() == "ExchangeId")
        e.Cancel = true;
      else if (e.Column.Header.ToString() == "DayOfWeek")
        e.Column.Header = "Day of Week";
    }

    //methods



  }
}
