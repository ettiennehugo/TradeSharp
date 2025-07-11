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
  /// Mass download of instrument data view.
  /// </summary>
  public sealed partial class MassDownloadInstrumentDataView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private IPluginsService m_pluginsService;
    private IDataProviderPlugin? m_dataProvider;

    //constructors
    public MassDownloadInstrumentDataView()
    {
      Settings = new MassDownloadSettings();
      m_pluginsService = (IPluginsService)IApplication.Current.Services.GetService(typeof(IPluginsService));
      this.InitializeComponent();
    }

    //finalizers


    //interface implementations


    //properties
    public string DefaultStartDateTime { get => Constants.DefaultMinimumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public string DefaultEndDateTime { get => Constants.DefaultMaximumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public MassDownloadSettings Settings { get; internal set; }

    public static readonly DependencyProperty ThreadCountMaxProperty = DependencyProperty.Register("ThreadCountMax", typeof(int), typeof(MassDownloadInstrumentDataView), new PropertyMetadata(1));
    public int ThreadCountMax
    {
      get => (int)GetValue(ThreadCountMaxProperty);
      set => SetValue(ThreadCountMaxProperty, value);
    }

    public Window ParentWindow { get; set; }
    public string DataProvider { get; set; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportExportDataDateTimeTimeZone));
      m_endDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd") + " 23:59";
      m_dataProvider = m_pluginsService.GetDataProviderPlugin(DataProvider);
      if (m_dataProvider != null)
      {
        ThreadCountMax = m_dataProvider.ConnectionCountMax;
        m_threadCount.Value = ThreadCountMax;
      }
    }

    private bool enableDownloadButton()
    {
      return m_startDateTime != null &&
        DateTime.TryParse(m_startDateTime.Text, out DateTime startDateTime) &&
        DateTime.TryParse(m_endDateTime.Text, out DateTime endDateTime) && 
        startDateTime < endDateTime &&
        m_instrumentSelectionView.SelectedItems.Count > 0 &&
        ((bool)m_resolutionMinute.IsChecked || (bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_downloadBtn_Click(object sender, RoutedEventArgs e)
    {
      IMassDownloadInstrumentData massDownloadInstrumentData = new MassDownloadInstrumentData();
      IMassDownloadInstrumentData.Context context = new IMassDownloadInstrumentData.Context();
      context.DataProvider = DataProvider;
      context.Settings = Settings;
      context.Instruments = m_instrumentSelectionView.SelectedItems;
      IDialogService dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IProgressDialog progressDialog = dialogService.CreateProgressDialog("Mass Download Progress", null);
      massDownloadInstrumentData.StartAsync(progressDialog, context);
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

    private void m_instrumentSelectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      m_downloadBtn.IsEnabled = enableDownloadButton();
    }
  }
}
