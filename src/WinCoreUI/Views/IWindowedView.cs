using Microsoft.UI.Xaml;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// View hosted by the a window.
  /// </summary>
  public interface IWindowedView
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    ViewWindow ParentWindow { get; }
    UIElement UIElement { get; }

    //methods


  }
}
