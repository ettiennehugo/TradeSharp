using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class ExchangesView : Page
  {

    //constants


    //enums


    //types


    //attributes


    //constructors
    public ExchangesView()
    {
      ViewModel = (ExchangeViewModel)((IApplication)Application.Current).Services.GetService(typeof(ExchangeViewModel));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public ExchangeViewModel ViewModel { get; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommand.Execute(null);
    }

    //methods


  }
}
