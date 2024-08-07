using Microsoft.UI.Xaml;
using System;
using TradeSharp.WinCoreUI.Common;

namespace TradeSharp.WinTraderWorkbench
{
  /// <summary>
  /// Provides application-specific behavior to supplement the default Application class.
  /// </summary>
  public partial class App : ApplicationBase
  {

    //constants


    //enums


    //types


    //attributes
    private Window m_window;

    //properties


    //constructors
    public App()
    {
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
      base.OnLaunched(args);
      m_window = new MainWindow();
      m_window.Activate();
    }

    //methods



  }
}
