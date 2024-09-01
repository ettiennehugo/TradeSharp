using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Data;
using TradeSharp.Common;
using TradeSharp.CoreUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Session definition view used to create/vuew/update a session.
  /// </summary>
  public sealed partial class SessionView : Page, IWindowedView
  {
    //constants
    public int Width = 725;
    public int Height = 405;

    //enums


    //types


    //attributes
    private ISessionService m_sessionService;

    //constructors
    public SessionView(Guid parentId, ViewWindow parent)
    {
      ParentWindow = parent;
      m_sessionService = (ISessionService)IApplication.Current.Services.GetService(typeof(ISessionService));
      InitializeComponent();
      setParentProperties();
      m_name.Text = "New Session";
      Session = new Session(Guid.NewGuid(), Session.DefaultAttributes, string.Empty, m_name.Text, parentId, DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(23, 59));
    }

    public SessionView(Session session, ViewWindow parent)
    {
      ParentWindow = parent;
      m_sessionService = (ISessionService)IApplication.Current.Services.GetService(typeof(ISessionService));
      InitializeComponent();
      setParentProperties();
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

    public ViewWindow ParentWindow { get; private set; }
    public UIElement UIElement => this;

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      WinCoreUI.Common.Utilities.populateComboBoxFromEnum(ref m_dayOfWeek, typeof(DayOfWeek));
    }

    private void setParentProperties()
    {
      ParentWindow.View = this;   //need to set this only once the view screen elements are created
      ParentWindow.ResetSizeable();
      ParentWindow.HideMinimizeAndMaximizeButtons();
      ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(Width, Height));
      ParentWindow.CenterWindow();
    }

    private void m_okButton_Click(object sender, RoutedEventArgs e)
    {
      m_sessionService.Update(Session);
      ParentWindow.Close();
    }

    private void m_cancelButton_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
