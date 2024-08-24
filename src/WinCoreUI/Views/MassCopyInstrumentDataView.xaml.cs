using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Commands;
using TradeSharp.CoreUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MassCopyInstrumentDataView : Page
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public MassCopyInstrumentDataView()
    {
      Settings = new MassCopySettings();
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public string DefaultStartDateTime { get => Constants.DefaultMinimumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public string DefaultEndDateTime { get => Constants.DefaultMaximumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public MassCopySettings Settings { get; internal set; }
    public int ThreadCountMax { get => Environment.ProcessorCount; }
    public Window ParentWindow { get; set; }
    public string DataProvider { get; set; }

    //methods
    private bool enableCopyButton()
    {
      return m_startDateTime != null && 
        DateTime.TryParse(m_startDateTime.Text, out DateTime startDateTime) && 
        DateTime.TryParse(m_endDateTime.Text, out DateTime endDateTime) &&
        startDateTime < endDateTime &&
        m_instrumentSelectionView.SelectedItems.Count > 0 &&
        ((bool)m_resolutionMinute.IsChecked || (bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_copyBtn_Click(object sender, RoutedEventArgs e)
    {
      MassCopyInstrumentData massCopyInstrumentData = new MassCopyInstrumentData();
      Settings.DataProvider = DataProvider;
      IMassCopyInstrumentData.Context context = new IMassCopyInstrumentData.Context(Settings, m_instrumentSelectionView.SelectedItems);
      IDialogService dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IProgressDialog progressDialog = dialogService.CreateProgressDialog("Mass Copy Progress", null);
      massCopyInstrumentData.StartAsync(progressDialog, context);
      ParentWindow.Close();    
    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }

    private void m_startDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_startDateTime.Text, out DateTime result)) Settings.FromDateTime = result;
      m_copyBtn.IsEnabled = enableCopyButton();
    }

    private void m_endDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_endDateTime.Text, out DateTime result)) Settings.ToDateTime = result;
      m_copyBtn.IsEnabled = enableCopyButton();
    }

    private void m_resolutionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      m_copyBtn.IsEnabled = enableCopyButton();
    }

    private void m_resolutionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      m_copyBtn.IsEnabled = enableCopyButton();
    }

    private void m_instrumentSelectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_copyBtn.IsEnabled = enableCopyButton();
    }
  }
}
