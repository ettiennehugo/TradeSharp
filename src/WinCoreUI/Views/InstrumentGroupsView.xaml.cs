using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Events;

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
      ViewModel = (IInstrumentGroupViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentGroupViewModel));
      this.InitializeComponent();
      ViewModel.RefreshEvent += onViewModelRefresh;
    }

    //finalizers


    //interface implementations


    //properties
    public IInstrumentGroupViewModel ViewModel { get; }

    //methods
    private void onViewModelRefresh(object? sender, RefreshEventArgs e)
    {
      //NOTE: Event to refresh will most likely come from a background thread, so we need to marshal the call to the UI thread.
      m_instrumentGroups.DispatcherQueue.TryEnqueue(new Microsoft.UI.Dispatching.DispatcherQueueHandler(() => ViewModel.RefreshCommand.Execute(null)));
    }

    private void m_findInstrumentGroupFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      ViewModel.FindText = m_findInstrumentGroupFilter.Text;
      if (ViewModel.FindText == "") ViewModel.ClearFilterCommand.Execute(null);
    }

    private void m_findInstrumentGroupFilter_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
      if (e.Key == Windows.System.VirtualKey.Enter && ViewModel.FindFirstCommand.CanExecute(null)) ViewModel.FindFirstCommand.Execute(null);
    }

    private void m_instrumentGroups_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
      ViewModel.ExpandNodeCommand.Execute(args.Item);
    }

    private void m_instrumentGroups_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
    {
      ViewModel.CollapseNodeCommand.Execute(args.Item);
    }
  }
}
