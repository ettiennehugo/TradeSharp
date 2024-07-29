using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentGroupView : Page, IWindowedView
  {
    //constants
    public const int Width = 922;
    public const int Height = 350;

    //enums


    //types


    //attributes
    private IInstrumentGroupService m_instrumentGroupService;

    //constructors
    public InstrumentGroupView(Guid parentId, ViewWindow parent)
    {
      ParentWindow = parent;
      m_instrumentGroupService = (IInstrumentGroupService)IApplication.Current.Services.GetService(typeof(IInstrumentGroupService));
      InstrumentGroup = new InstrumentGroup(Guid.NewGuid(), Data.InstrumentGroup.DefaultAttributes, "", parentId, "", Array.Empty<string>(), "", "", new List<string>());
      this.InitializeComponent();
      setParentProperties();
    }

    public InstrumentGroupView(InstrumentGroup instrumentGroup, ViewWindow parent)
    {
      ParentWindow = parent;
      InstrumentGroup = instrumentGroup;
      this.InitializeComponent();
      setParentProperties();
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_instrumentGroupProperty = DependencyProperty.Register("InstrumentGroup", typeof(InstrumentGroup), typeof(InstrumentGroupView), new PropertyMetadata(null));
    public InstrumentGroup? InstrumentGroup
    {
      get => (InstrumentGroup?)GetValue(s_instrumentGroupProperty);
      set => SetValue(s_instrumentGroupProperty, value);
    }

    public ViewWindow ParentWindow { get; private set; }
    public UIElement UIElement => this;

    //methods
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
      m_instrumentGroupService.Update(InstrumentGroup!);
      ParentWindow.Close();
    }

    private void m_cancelButton_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }
  }
}
