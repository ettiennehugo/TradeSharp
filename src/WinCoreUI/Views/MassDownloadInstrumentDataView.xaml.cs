using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using TradeSharp.Common;
using TradeSharp.CoreUI.Common;
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
    private IMassDownloadInstrumentDataService m_massDownloadInstrumentDataService;
    private CancellationToken m_cancellationToken;

    //constructors
    public MassDownloadInstrumentDataView()
    {
      Settings = new MassDownloadSettings();
      m_cancellationToken = new CancellationToken();
      m_massDownloadInstrumentDataService = (IMassDownloadInstrumentDataService)IApplication.Current.Services.GetService(typeof(IMassDownloadInstrumentDataService));
      this.InitializeComponent();
      updateCopyCheckBoxEnabled();
    }

    //finalizers


    //interface implementations


    //properties
    public string DefaultStartDateTime { get => Constants.DefaultMinimumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public string DefaultEndDateTime { get => Constants.DefaultMaximumDateTime.ToString("yyyy-MM-dd HH:mm"); }
    public MassDownloadSettings Settings { get; internal set; }
    public int ThreadCountMax { get => Environment.ProcessorCount; }
    public Window ParentWindow { get; set; }

    //methods
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
      Common.Utilities.populateComboBoxFromEnum(ref m_dateTimeTimeZone, typeof(ImportDataDateTimeTimeZone));
      ParentWindow.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)ActualWidth, (int)ActualHeight));
    }

    private bool enableDownloadButton()
    {
      return m_startDateTime != null && DateTime.TryParse(m_startDateTime.Text, out _) && DateTime.TryParse(m_endDateTime.Text, out _) &&
        ((bool)m_resolutionMinute.IsChecked || (bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_downloadBtn_Click(object sender, RoutedEventArgs e)
    {
      throw new NotImplementedException();
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

    private void updateCopyCheckBoxEnabled()
    {
      m_copyWeekFromDay.IsEnabled = (bool)m_resolutionWeek.IsChecked && (bool)m_resolutionDay.IsChecked;
      m_copyMonthFromDayWeek.IsEnabled = (bool)m_resolutionMonth.IsChecked && ((bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked);
    }

    private void m_resolutionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      m_downloadBtn.IsEnabled = enableDownloadButton();
      updateCopyCheckBoxEnabled();
    }

    private void m_resolutionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      m_downloadBtn.IsEnabled = enableDownloadButton();
      updateCopyCheckBoxEnabled();
    }
  }
}
