using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the list of the holidays associated with a specific parent country or exchange.
  /// </summary>
  public sealed partial class HolidaysView : UserControl
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public HolidaysView()
    {
      ViewModel = (HolidayViewModel)((IApplication)Application.Current).Services.GetService(typeof(HolidayViewModel));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public static readonly DependencyProperty s_parentIdProperty = DependencyProperty.Register("ParentId", typeof(Guid), typeof(HolidaysView), new PropertyMetadata(null));
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

    public HolidayViewModel ViewModel { get; }

    //methods


  }
}
