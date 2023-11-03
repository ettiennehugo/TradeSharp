using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.WinCoreUI.Common;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Session definition view used to create/vuew/update a session.
  /// </summary>
  public sealed partial class SessionView : Page
    {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public SessionView(Guid parentId)
    {
      InitializeComponent();
      m_name.Text = "New Session";
      Session = new Session(Guid.NewGuid(), Session.DefaultAttributeSet, "TagValue", m_name.Text, parentId, DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(23, 59));
    }

    public SessionView(Session session)
    {
      InitializeComponent();
      Session = session;
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_sessionProperty = DependencyProperty.Register("Session", typeof(Session), typeof(SessionView), new PropertyMetadata(null));
    public Session? Session
    {
      get => (Session?)GetValue(s_sessionProperty);
      set => SetValue(s_sessionProperty, value);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Utilities.populateComboBoxFromEnum(ref m_dayOfWeek, typeof(DayOfWeek));
    }

    //methods



  }
}
