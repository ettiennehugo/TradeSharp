using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class InstrumentGroupsView : Page
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public InstrumentGroupsView()
    {
      ViewModel = Ioc.Default.GetRequiredService<InstrumentGroupViewModel>();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentGroupViewModel ViewModel { get; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Nodes.Count == 0) ViewModel.RefreshCommand.Execute(null);
    }

    //methods



  }
}
