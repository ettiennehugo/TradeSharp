using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TradeSharp.Common;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// Mass download of instrument data view.
  /// </summary>
  public sealed partial class MassDownloadInstrumentDataView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private IMassDownloadInstrumentDataService m_massDownloadInstrumentDataService;
    private IPluginsService m_pluginsService;
    private IDataProviderPlugin? m_dataProvider;

    //constructors
    public MassDownloadInstrumentDataView()
    {
      Settings = new MassDownloadSettings();
      m_massDownloadInstrumentDataService = (IMassDownloadInstrumentDataService)IApplication.Current.Services.GetService(typeof(IMassDownloadInstrumentDataService));
      m_pluginsService = (IPluginsService)IApplication.Current.Services.GetService(typeof(IPluginsService));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public string DefaultStartDateTime { get => Constants.DefaultMinimumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public string DefaultEndDateTime { get => Constants.DefaultMaximumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public MassDownloadSettings Settings { get; internal set; }
    public int ThreadCountMax { get; internal set; }
    public Window ParentWindow { get; set; }
    public string DataProvider { get; set; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportExportDataDateTimeTimeZone));
      m_endDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd") + " 23:59";
      m_dataProvider = (IDataProviderPlugin)m_pluginsService.Items.FirstOrDefault(p => p is IDataProviderPlugin && p.Name == DataProvider);
      if (m_dataProvider != null) ThreadCountMax = m_dataProvider.ConnectionCountMax;
    }

    private bool enableDownloadButton()
    {
      return m_startDateTime != null && DateTime.TryParse(m_startDateTime.Text, out DateTime startDateTime) && DateTime.TryParse(m_endDateTime.Text, out DateTime endDateTime) && startDateTime < endDateTime &&
        ((bool)m_resolutionMinute.IsChecked || (bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_downloadBtn_Click(object sender, RoutedEventArgs e)
    {
      m_massDownloadInstrumentDataService.DataProvider = m_dataProvider;
      m_massDownloadInstrumentDataService.Settings = Settings;
      m_massDownloadInstrumentDataService.Logger = null;
      IDialogService dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IProgressDialog progressDialog = dialogService.CreateProgressDialog("Mass Download Progress", m_massDownloadInstrumentDataService.Logger);
      m_massDownloadInstrumentDataService.StartAsync(progressDialog, m_instrumentSelectionView.SelectedItems);
      ParentWindow.Close();
    }

    private void m_cancelBtn_Click(object sender, RoutedEventArgs e)
    {
      ParentWindow.Close();
    }

    private void m_startDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_startDateTime.Text, out DateTime result)) Settings.FromDateTime = result;
      m_downloadBtn.IsEnabled = enableDownloadButton();
    }

    private void m_endDateTime_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (DateTime.TryParse(m_endDateTime.Text, out DateTime result)) Settings.ToDateTime = result;
      m_downloadBtn.IsEnabled = enableDownloadButton();
    }

    private void m_resolutionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      m_downloadBtn.IsEnabled = enableDownloadButton();
    }

    private void m_resolutionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      m_downloadBtn.IsEnabled = enableDownloadButton();
    }
  }
}
