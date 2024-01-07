using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Implements the IAsyncOperation<LoadMoreItemsResult> interface to support incremental loading of instruments.
  /// </summary>
  class IncrementalInstrumentLoadResult : IAsyncOperation<LoadMoreItemsResult>
  {
    //constants


    //enums


    //types


    //attributes
    private Task<Instrument> m_task;

    //constructors
    public IncrementalInstrumentLoadResult(Task<Instrument> task)
    {
      m_task = task;
    }

    //finalizers


    //interface implementations



    //Example of how to implement the IAsyncOperation 
    //https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.data.isupportincrementalloading?view=winrt-22621



    public AsyncOperationCompletedHandler<LoadMoreItemsResult> Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


    public void Cancel()
    {
      //m_task.Cancel = true;
      throw new NotImplementedException();
    }

    public void Close()
    {
      //m_task.Close();
      throw new NotImplementedException();
    }

    public LoadMoreItemsResult GetResults()
    {
      throw new NotImplementedException();
    }


    //properties
    public Exception ErrorCode => m_task.Exception;
    public uint Id => (uint)m_task.Id;
    public AsyncStatus Status => throw new NotImplementedException();


    //methods




  }

  /// <summary>
  /// Displays the list of instruments defined for trading.
  /// </summary>
  public sealed partial class InstrumentsView : Page, ISupportIncrementalLoading
  {
    //constants


    //enums
    private enum FilterField
    {
      Ticker = 0,
      Name,
      Description,
      Any
    }

    //types


    //attributes


    //constructors
    public InstrumentsView()
    {
      ViewModel = Ioc.Default.GetRequiredService<InstrumentViewModel>();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
      throw new NotImplementedException();



      //TODO: implement paged loading


    }

    //properties
    public InstrumentViewModel ViewModel { get; }
    public bool HasMoreItems { get { return ViewModel.HasMoreItems; } }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      if (ViewModel.Items.Count == 0) ViewModel.RefreshCommandAsync.ExecuteAsync(null);
    }

    public bool filter(Instrument instrument)
    {
      if (m_instrumentFilter == null || m_instrumentFilter.Text.Length == 0) return true; //no filter specified - m_instrumentFilter is null on screen init

      switch ((FilterField)m_filterMatchFields.SelectedIndex)
      {
        case FilterField.Ticker:
          return instrument.Ticker.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
        case FilterField.Name:
          return instrument.Name.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
        case FilterField.Description:
          return instrument.Description.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
        case FilterField.Any:
          return instrument.Ticker.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase) || instrument.Name.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase) || instrument.Description.Contains(m_instrumentFilter.Text, StringComparison.InvariantCultureIgnoreCase);
      }

      return false;   //in general should not happen if match field is mandatory selection
    }

    private void refreshFilter()
    {
      if (m_instrumentFilter == null) return;

      //show all items on clear filter text
      if (m_instrumentFilter.Text.Length == 0)
      {
        if (m_instruments.ItemsSource != ViewModel.Items) m_instruments.ItemsSource = ViewModel.Items;
        return;
      }

      m_instruments.ItemsSource = new ObservableCollection<Instrument>(from instrument in ViewModel.Items where filter(instrument) select instrument);
    }

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      refreshFilter();
    }
  }
}
