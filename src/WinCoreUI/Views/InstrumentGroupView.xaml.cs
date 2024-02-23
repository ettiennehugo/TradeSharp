using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TradeSharp.Data;

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
      InstrumentGroup = new InstrumentGroup(Guid.NewGuid(), Data.InstrumentGroup.DefaultAttributeSet, "", parentId, "", Array.Empty<string>(), "", "", new List<Guid>());
      this.InitializeComponent();
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
