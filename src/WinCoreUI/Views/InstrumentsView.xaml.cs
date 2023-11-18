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
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the list of instruments defined for trading.
  /// </summary>
  public sealed partial class InstrumentsView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private IDialogService m_dialogService;

    //constructors
    public InstrumentsView()
    {
      ViewModel = Ioc.Default.GetRequiredService<InstrumentViewModel>();
      m_dialogService = Ioc.Default.GetRequiredService<IDialogService>();


      //TODO: It seems like there is NO way to get this to show up before the window controls are instantiated.

      m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", "Loading instruments...");
      m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Indeterminate, 0, 0, 0);


      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentViewModel ViewModel { get; }

    //private async void Page_Loading(FrameworkElement sender, object args)
    //{
    //  await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", "Loading instruments...");
    //  await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Indeterminate, 0, 0, 0);
    //}

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      ViewModel.RefreshCommand.Execute(null);
//      await m_dialogService.ShowStatusMessageAsync(IDialogService.StatusMessageSeverity.Information, "", "");
//      await m_dialogService.ShowStatusProgressAsync(IDialogService.StatusProgressState.Reset, 0, 0, 0);
    }


    //methods


  }
}
