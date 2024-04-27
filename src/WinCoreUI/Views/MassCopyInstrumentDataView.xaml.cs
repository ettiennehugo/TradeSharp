using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MassCopyInstrumentDataView : Page
  {
    //constants


    //enums


    //types


    //attributes
    private IMassCopyInstrumentDataService m_massCopyInstrumentDataService;

    //constructors
    public MassCopyInstrumentDataView()
    {
      Settings = new MassCopySettings();
      m_massCopyInstrumentDataService = (IMassCopyInstrumentDataService)IApplication.Current.Services.GetService(typeof(IMassCopyInstrumentDataService));
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
      return m_startDateTime != null && DateTime.TryParse(m_startDateTime.Text, out DateTime startDateTime) && DateTime.TryParse(m_endDateTime.Text, out DateTime endDateTime) && startDateTime < endDateTime &&
        ((bool)m_resolutionHour.IsChecked || (bool)m_resolutionDay.IsChecked || (bool)m_resolutionWeek.IsChecked || (bool)m_resolutionMonth.IsChecked);
    }

    private void m_copyBtn_Click(object sender, RoutedEventArgs e)
    {
      m_massCopyInstrumentDataService.DataProvider = DataProvider;
      m_massCopyInstrumentDataService.Settings = Settings;
      m_massCopyInstrumentDataService.Logger = null;    //TODO: Currently we do not set the logger for the mass export service - this can be done as an improvement when we have a progress dialog working.
      IDialogService dialogService = (IDialogService)IApplication.Current.Services.GetService(typeof(IDialogService));
      IProgressDialog progressDialog = dialogService.CreateProgressDialog("Mass Copy Progress", m_massCopyInstrumentDataService.Logger);
      m_massCopyInstrumentDataService.StartAsync(progressDialog);
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
  }
}
