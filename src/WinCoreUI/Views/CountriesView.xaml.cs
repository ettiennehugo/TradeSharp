using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
      ViewModel = Ioc.Default.GetRequiredService<CountryViewModel>();
      this.InitializeComponent();
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
