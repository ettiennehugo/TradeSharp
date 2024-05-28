using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
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
    public InstrumentSelectionViewMode DefaultInstrumentSelectionViewMode = InstrumentSelectionViewMode.SelectSingle | InstrumentSelectionViewMode.Refresh | InstrumentSelectionViewMode.Add | InstrumentSelectionViewMode.Edit | InstrumentSelectionViewMode.Delete | InstrumentSelectionViewMode.Import | InstrumentSelectionViewMode.Export;

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
    Visibility m_refreshVisible = Visibility.Visible;
    Visibility m_addVisible = Visibility.Visible;
    Visibility m_editVisible = Visibility.Visible;
    Visibility m_deleteVisible = Visibility.Visible;
    Visibility m_addEditDeleteSeparatorVisible = Visibility.Visible;
    Visibility m_importVisible = Visibility.Visible;
    Visibility m_exportVisible = Visibility.Visible;
    Visibility m_importExportSeparatorVisible = Visibility.Visible;
    Visibility m_multiSelectVisible = Visibility.Visible;
    List<Instrument> m_selectedItems;

    //constructors
    public InstrumentSelectionView()
    {
      ViewModel = (IInstrumentViewModel)IApplication.Current.Services.GetService(typeof(IInstrumentViewModel));
      Instruments = new ObservableCollection<Instrument>(ViewModel.Items);
      m_selectedItems = new List<Instrument>();
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
    public IList<Instrument> SelectedItems { get => m_selectedItems; }
    public Visibility RefreshVisible { get => m_refreshVisible; }
    public Visibility AddVisible { get => m_addVisible; }
    public Visibility EditVisible { get => m_editVisible;}
    public Visibility DeleteVisible { get => m_deleteVisible;}
    public Visibility AddEditDeleteSeparatorVisible { get => m_addEditDeleteSeparatorVisible; }
    public Visibility ImportVisible { get => m_importVisible; }
    public Visibility ExportVisible { get => m_exportVisible; }
    public Visibility ImportExportSeparatorVisible { get => m_importExportSeparatorVisible; }
    public Visibility MultiSelectVisible { get => m_multiSelectVisible; }

    //events
    public event PropertyChangedEventHandler PropertyChanged;
    public event SelectionChangedEventHandler SelectionChanged;

    //methods
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      updateScreenControlFlags(true, true);
    }

    private void notifyPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void updateVisible(ref Visibility visibility, string propertyName, InstrumentSelectionViewMode modeFlag, bool raiseChangeNotification, bool forceRefresh)
    {
      Visibility oldValue = visibility;
      visibility = (InstrumentSelectionViewMode & modeFlag) == modeFlag ? Visibility.Visible : Visibility.Collapsed;
      if (raiseChangeNotification && (oldValue != visibility || forceRefresh)) notifyPropertyChanged(propertyName);
    }

    private void updateVisible(ref Visibility visibility, string propertyName, bool visible, bool raiseChangeNotification, bool forceRefresh)
    {
      Visibility oldValue = visibility;
      visibility = visible ? Visibility.Visible : Visibility.Collapsed;
      if (raiseChangeNotification && (oldValue != visibility || forceRefresh)) notifyPropertyChanged(propertyName);
    }

    private void updateScreenControlFlags(bool raiseChangeNotification, bool forceRefresh = false)
    {
      ListViewSelectionMode oldSelectionMode = m_selectionMode;
      if ((InstrumentSelectionViewMode & InstrumentSelectionViewMode.SelectSingle) == InstrumentSelectionViewMode.SelectSingle)
        m_selectionMode = ListViewSelectionMode.Single;
      else if ((InstrumentSelectionViewMode & InstrumentSelectionViewMode.SelectMulti) == InstrumentSelectionViewMode.SelectMulti)
        m_selectionMode = ListViewSelectionMode.Extended; //extended allows non-contigious multiseletion, multiselect only allows selecting contigious elements from the list
      else
        m_selectionMode = ListViewSelectionMode.Single;
      if (raiseChangeNotification && (oldSelectionMode != m_selectionMode || forceRefresh)) notifyPropertyChanged("SelectionMode");

      updateVisible(ref m_multiSelectVisible, "MultiSelectVisible", InstrumentSelectionViewMode.SelectMulti, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_refreshVisible, "RefreshVisible", InstrumentSelectionViewMode.Refresh, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_addVisible, "AddVisible", InstrumentSelectionViewMode.Add, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_editVisible, "EditVisible", InstrumentSelectionViewMode.Edit, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_deleteVisible, "DeleteVisible", InstrumentSelectionViewMode.Delete, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_addEditDeleteSeparatorVisible, "AddEditDeleteSeparatorVisible", m_addVisible == Visibility.Visible | m_editVisible == Visibility.Visible | m_deleteVisible == Visibility.Visible, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_importVisible, "ImportVisible", InstrumentSelectionViewMode.Import, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_exportVisible, "ExportVisible", InstrumentSelectionViewMode.Export, raiseChangeNotification, forceRefresh);
      updateVisible(ref m_importExportSeparatorVisible, "ImportExportSeparatorVisible", m_importVisible == Visibility.Visible | m_exportVisible == Visibility.Visible, raiseChangeNotification, forceRefresh);
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

    private void m_selectAll_Click(object sender, RoutedEventArgs e)
    {
      m_instruments.SelectAll();
      m_selectedItems = new List<Instrument>(ViewModel.Items);
      IList<object> itemsAdded = new List<object>(m_selectedItems);
      SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Array.Empty<object>().ToList(), itemsAdded));
    }

    private void m_deselectAll_Click(object sender, RoutedEventArgs e)
    {
      m_instruments.DeselectRange(new ItemIndexRange(0, (uint)Instruments.Count));
      IList<object> itemsRemoved = new List<object>(m_selectedItems);
      m_selectedItems.Clear();
      SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(itemsRemoved, Array.Empty<object>().ToList()));
    }

    private void m_instruments_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      IList<object> itemsRemoved = new List<object>(m_selectedItems);
      m_selectedItems.Clear();
      foreach (var item in m_instruments.SelectedItems) m_selectedItems.Add(item as Instrument);
      foreach (var item in itemsRemoved) if (m_selectedItems.Contains(item as Instrument)) itemsRemoved.Remove(item as Instrument); //need to compute the correct delta for removed items if item is still selected
      IList<object> itemsAdded = new List<object>(m_selectedItems);
      SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(itemsRemoved, itemsAdded));
    }
  }
}
