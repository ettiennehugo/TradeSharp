using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.Common;
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
      ViewModel = (ICountryViewModel)IApplication.Current.Services.GetService(typeof(ICountryViewModel));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public ICountryViewModel ViewModel { get; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommand.Execute(null);
    }

    //methods


  }
}
