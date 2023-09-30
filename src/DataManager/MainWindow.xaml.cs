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
using TradeSharp.WinDataManager.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using TradeSharp.CoreUI.Events;
using TradeSharp.Common;

namespace TradeSharp.WinDataManager
{
  /// <summary>
  /// An empty window that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainWindow : Window
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public MainWindow()
    {
      this.InitializeComponent();
      ViewModel = Ioc.Default.GetRequiredService<MainWindowViewModel>();
      ViewModel.SetNavigationFrame(m_nvvMainContent);
    }

    //finalizers


    //interface implementations


    //properties
    public MainWindowViewModel ViewModel { get; }

    //methods
    // Send message to use navigation between views if the window size is not large enough to display a list view and the related detailed data.
    private void OnSizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
      double width = args.Size.Width;
      NavigationMessage navigation = new(new()
      {
        UseNavigation = width < 1024
      });
      WeakReferenceMessenger.Default.Send(navigation);
    }
  }
}
