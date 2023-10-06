using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TradeSharp.Data;
using System.Xml.Linq;
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
      Session = new Session(Guid.NewGuid(), m_name.Text, parentId, DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(23, 59));
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
