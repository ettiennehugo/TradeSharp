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
using TradeSharp.Data;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentGroupView : Page
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public InstrumentGroupView(Guid parentId)
    {
      InstrumentGroup = new InstrumentGroup(Guid.NewGuid(), Data.InstrumentGroup.DefaultAttributeSet, parentId, "", "", new List<Guid>());
      this.InitializeComponent();
      m_name.Text = "New instrument group";
      m_name.SelectAll();
    }

    public InstrumentGroupView(InstrumentGroup instrumentGroup)
    {
      InstrumentGroup = instrumentGroup;
      this.InitializeComponent();
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

    //methods


  }
}
