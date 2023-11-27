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
using TradeSharp.CoreUI.ViewModels;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the list of the defined countries and allow adding new countries.
  /// </summary>
  public sealed partial class CountriesView : Page
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public CountriesView()
    {
      this.InitializeComponent();
      ViewModel = Ioc.Default.GetRequiredService<CountryViewModel>();
    }

    //finalizers


    //interface implementations


    //properties
    public CountryViewModel ViewModel { get; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommand.Execute(null);
    }

    //methods


  }
}
