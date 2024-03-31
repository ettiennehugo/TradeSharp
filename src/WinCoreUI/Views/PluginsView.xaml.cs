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
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Generic view to work with plugins.
  /// </summary>
  public sealed partial class PluginsView : Page
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public PluginsView()
    {
      ViewModel = (PluginsViewModel)IApplication.Current.Services.GetService(typeof(IPluginsViewModel));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public IPluginsViewModel ViewModel { get; internal set; }


    //methods



  }
}
