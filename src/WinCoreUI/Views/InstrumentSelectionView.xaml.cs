using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;
using TradeSharp.CoreUI.ViewModels;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Displays the list of instruments defined for trading and allows selection, display, editing of the elements based on the mode in which the control is run.
  /// </summary>
  public sealed partial class InstrumentSelectionView : UserControl, INotifyPropertyChanged
  {
    //constants
    public InstrumentSelectionViewMode DefaultInstrumentSelectionViewMode = InstrumentSelectionViewMode.SelectSingle | InstrumentSelectionViewMode.Add | InstrumentSelectionViewMode.Edit | InstrumentSelectionViewMode.Delete | InstrumentSelectionViewMode.Import | InstrumentSelectionViewMode.Export;

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
    InstrumentSelectionViewMode m_instrumentSelectionViewMode;
    ListViewSelectionMode m_selectionMode = ListViewSelectionMode.Single;
    Visibility m_addVisible = Visibility.Visible;
    Visibility m_editVisible = Visibility.Visible;
    Visibility m_deleteVisible = Visibility.Visible;
    Visibility m_addEditDeleteSeparatorVisible = Visibility.Visible;
    Visibility m_importVisible = Visibility.Visible;
    Visibility m_exportVisible = Visibility.Visible;
    Visibility m_importExportSeparatorVisible = Visibility.Visible;

    //constructors
    public InstrumentSelectionView()
    {
      ViewModel = (IInstrumentViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentViewModel));
      Instruments = new ObservableCollection<Instrument>(ViewModel.Items);
      m_instrumentSelectionViewMode = DefaultInstrumentSelectionViewMode;
      updateScreenControlFlags(false);
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public InstrumentSelectionViewMode InstrumentSelectionViewMode { get => m_instrumentSelectionViewMode; set { m_instrumentSelectionViewMode = value; updateScreenControlFlags(true); } }
    public IInstrumentViewModel ViewModel { get; }
    public ObservableCollection<Instrument> Instruments;
    public ListViewSelectionMode SelectionMode { get => m_selectionMode; }
    public Visibility AddVisible { get => m_addVisible; }
    public Visibility EditVisible { get => m_editVisible;}
    public Visibility DeleteVisible { get => m_deleteVisible;}
    public Visibility AddEditDeleteSeparatorVisible { get => m_addEditDeleteSeparatorVisible; }
    public Visibility ImportVisible { get => m_importVisible; }
    public Visibility ExportVisible { get => m_exportVisible; }
    public Visibility ImportExportSeparatorVisible { get => m_importExportSeparatorVisible; }

    //events
    public event PropertyChangedEventHandler PropertyChanged;

    //methods
    private void notifyPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void updateVisible(ref Visibility visibility, string propertyName, InstrumentSelectionViewMode modeFlag, bool raiseChangeNotification)
    {
      Visibility oldValue = visibility;
      visibility = (InstrumentSelectionViewMode & modeFlag) == modeFlag ? Visibility.Visible : Visibility.Collapsed;
      if (raiseChangeNotification && oldValue != visibility) notifyPropertyChanged(propertyName);
    }

    private void updateVisible(ref Visibility visibility, string propertyName, bool visible, bool raiseChangeNotification)
    {
      Visibility oldValue = visibility;
      visibility = visible ? Visibility.Visible : Visibility.Collapsed;
      if (raiseChangeNotification && oldValue != visibility) notifyPropertyChanged(propertyName);
    }

    private void updateScreenControlFlags(bool raiseChangeNotification)
    {
      ListViewSelectionMode oldSelectionMode = m_selectionMode;
      if ((InstrumentSelectionViewMode & InstrumentSelectionViewMode.SelectSingle) == InstrumentSelectionViewMode.SelectSingle)
        m_selectionMode = ListViewSelectionMode.Single;
      else if ((InstrumentSelectionViewMode & InstrumentSelectionViewMode.SelectMulti) == InstrumentSelectionViewMode.SelectMulti)
        m_selectionMode = ListViewSelectionMode.Multiple;
      else
        m_selectionMode = ListViewSelectionMode.Single;
      m_selectionMode = (InstrumentSelectionViewMode & InstrumentSelectionViewMode.SelectSingle) == InstrumentSelectionViewMode.SelectSingle ? ListViewSelectionMode.Single : ListViewSelectionMode.None;
      if (raiseChangeNotification && oldSelectionMode != m_selectionMode) notifyPropertyChanged("SelectionMode");

      updateVisible(ref m_addVisible, "AddVisible", InstrumentSelectionViewMode.Add, raiseChangeNotification);
      updateVisible(ref m_editVisible, "EditVisible", InstrumentSelectionViewMode.Edit, raiseChangeNotification);
      updateVisible(ref m_deleteVisible, "DeleteVisible", InstrumentSelectionViewMode.Delete, raiseChangeNotification);
      updateVisible(ref m_addEditDeleteSeparatorVisible, "AddEditDeleteSeparatorVisible", m_addVisible == Visibility.Visible | m_editVisible == Visibility.Visible | m_deleteVisible == Visibility.Visible, raiseChangeNotification);
      updateVisible(ref m_importVisible, "ImportVisible", InstrumentSelectionViewMode.Import, raiseChangeNotification);
      updateVisible(ref m_exportVisible, "ExportVisible", InstrumentSelectionViewMode.Export, raiseChangeNotification);
      updateVisible(ref m_importExportSeparatorVisible, "ImportExportSeparatorVisible", m_importVisible == Visibility.Visible | m_exportVisible == Visibility.Visible, raiseChangeNotification);
    }
    
    private bool filterInstrument(Instrument instrument)
    {
      if (m_instrumentFilter.Text.Length == 0) return true;

      switch ((FilterField)m_filterMatchFields.SelectedIndex)
      {
        case FilterField.Ticker:
          return instrument.Ticker.Contains(m_instrumentFilter.Text);
        case FilterField.Name:
          return instrument.Name.Contains(m_instrumentFilter.Text);
        case FilterField.Description:
          return instrument.Description.Contains(m_instrumentFilter.Text);
        case FilterField.Any:
          return instrument.Ticker.Contains(m_instrumentFilter.Text) || instrument.Name.Contains(m_instrumentFilter.Text) || instrument.Description.Contains(m_instrumentFilter.Text);
        default:
          return false;
      }
    }

    private void refreshFilter()
    {
      if (m_instrumentFilter == null) return;
      var filteredResult = from instrument in ViewModel.Items where filterInstrument(instrument) select instrument;
      Instruments.Clear();
      foreach (var instrument in filteredResult) Instruments.Add(instrument);
      ViewModel.SelectedItem = Instruments.FirstOrDefault();
    }

    private void resetFilter()
    {
      m_instrumentFilter.ClearValue(TextBox.TextProperty);
      m_filterMatchFields.SelectedIndex = (int)FilterField.Any;
      Instruments.Clear();
      foreach (var instrument in ViewModel.Items) Instruments.Add(instrument);
      ViewModel.SelectedItem = Instruments.FirstOrDefault();
    }

    private void m_instrumentFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
      refreshFilter();
    }

    private void m_filterMatchFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      refreshFilter();
    }

    private void DeleteBarButton_Click(object sender, RoutedEventArgs e)
    {
      //need to explicitly remove the item collection since this is the filtered list
      ViewModel.DeleteCommand.Execute(ViewModel.SelectedItem);
      Instruments.Remove(ViewModel.SelectedItem);
      ViewModel.SelectedItem = ViewModel.Items.FirstOrDefault();
    }
  }
}
