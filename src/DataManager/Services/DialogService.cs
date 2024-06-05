using System;
using System.Windows;
using WinRT.Interop;
using System.Threading.Tasks;
using Windows.UI.Popups;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Services;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.UI.Dispatching;
using static TradeSharp.CoreUI.Services.IDialogService;
using Microsoft.UI.Xaml;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Views;
using Microsoft.Extensions.Logging;

namespace TradeSharp.WinDataManager.Services
{
  /// <summary>
  /// Windows implementation of the dialog service.
  /// </summary>
  public class DialogService : IDialogService
  {

    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //external imports
    // from winuser.h
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/
    // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-styles
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x10000;
    private const int WS_MINIMIZEBOX = 0x20000;
    private const int WS_SYSMENU = 0x80000;
    private const int WS_DLGFRAME = 0x400000;
    private const int WS_SIZEBOX = 0x40000;

    [DllImport("user32.dll")]
    extern private static IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    extern private static int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

    //interface implementations
    public void PostUIUpdate(Action updateAction)
    {
      UIDispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => { updateAction(); });
    }

    public async Task ShowPopupMessageAsync(string message)
    {
      MessageDialog dlg = new(message);
      var hwnd = GetActiveWindow();
      if (hwnd == IntPtr.Zero)
        throw new InvalidOperationException();
      InitializeWithWindow.Initialize(dlg, hwnd);
      await dlg.ShowAsync();
    }

    public Task ShowStatusMessageAsync(StatusMessageSeverity severity, string title, string message)
    {
      StatusBarText.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
      {
        //see glyphs used at - https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font
        switch (severity)
        {
          case StatusMessageSeverity.Success:
            StatusBarIcon.Glyph = "\uE73E";
            break;
          case StatusMessageSeverity.Information:
            StatusBarIcon.Glyph = "\uE946";
            break;
          case StatusMessageSeverity.Warning:
            StatusBarIcon.Glyph = "\uE128";
            break;
          case StatusMessageSeverity.Error:
            StatusBarIcon.Glyph = "\uE783";
            break;
        }

        if (title.Length != 0)
          StatusBarText.Text = $"{title} - {message}";
        else
          StatusBarText.Text = $"{message}";
      });

      return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a progress dialog with the specified title and also takes a logger to echo log operations.
    /// </summary>
    public IProgressDialog CreateProgressDialog(string title, ILogger? logger)
    {
      IProgressDialog result = null;
      //create the dialog from the UI thread
      TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
      if (UIDispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>    //dialog creation needs precedence since other logs might still be incrementally loading
      {
        Window window = new Window();
        WinCoreUI.Views.ProgressDialogView progressDialog = new WinCoreUI.Views.ProgressDialogView();
        progressDialog.Title = title;
        progressDialog.Logger = logger;
        progressDialog.ParentWindow = window;
        window.ExtendsContentIntoTitleBar = true;
        window.Content = progressDialog;
        MakeDialog(window);
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1230, 300));
        result = progressDialog;
        taskCompletionSource.SetResult(true);
      }))
        taskCompletionSource.Task.Wait(10000); //wait up to 10-seconds for the dialog to be created
      //caller needs to explicitly call the show, since it needs to setup some of the members before the progress dialog is shown
      return result!;
    }

    public ICorrectiveLoggerDialog CreateCorrectiveLoggerDialog(string title, LogEntry? entry = null)
    {
      ICorrectiveLoggerDialog result = null;
      //create the dialog from the UI thread
      TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
      if (UIDispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>    //dialog creation needs precedence since other logs might still be incrementally loading
      {
        WinCoreUI.Views.LoggerViewDialog loggerViewDialog = new WinCoreUI.Views.LoggerViewDialog(entry);
        loggerViewDialog.Title = title;
        loggerViewDialog.ExtendsContentIntoTitleBar = true;
        MakeDialog(loggerViewDialog);
        result = loggerViewDialog;
        taskCompletionSource.SetResult(true);
      }))
        taskCompletionSource.Task.Wait(10000); //wait up to 10-seconds for the dialog to be created
      //caller needs to explicitly call the show, since it needs to setup some of the members before the progress dialog is shown
      return result!;
    }

    protected InitNavigationService getInitNavigationService() => (InitNavigationService)((IApplication)Application.Current).Services.GetService(typeof(InitNavigationService));

    public async Task<CountryInfo?> ShowSelectCountryAsync()
    {
      WinCoreUI.Views.CountrySelectorView view = new WinCoreUI.Views.CountrySelectorView();
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Select a country",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary
      };

      ContentDialogResult result = await dialog.ShowAsync();

      if (result == ContentDialogResult.Primary) return view.SelectedCountry;

      return null;
    }

    public async Task<Holiday> ShowCreateHolidayAsync(Guid parentId)
    {
      WinCoreUI.Views.HolidayView view = new WinCoreUI.Views.HolidayView(parentId);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Create Holiday",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();

      if (result == ContentDialogResult.Primary) return view.Holiday;

      return null;
    }

    public async Task<Holiday?> ShowUpdateHolidayAsync(Holiday holiday)
    {
      WinCoreUI.Views.HolidayView view = new WinCoreUI.Views.HolidayView((Holiday)holiday.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Update Holiday",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();

      if (result == ContentDialogResult.Primary) return view.Holiday;

      return null;
    }

    public async Task<Exchange?> ShowCreateExchangeAsync()
    {
      WinCoreUI.Views.ExchangeView view = new WinCoreUI.Views.ExchangeView();
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Create Exchange",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();

      if (result == ContentDialogResult.Primary)
      {
        string logoFilename = Exchange.GetLogoPath(view.Exchange.LogoId);
        if (view.ExchangeLogoPath != logoFilename)
          try
          {
            Exchange.ReplaceLogo(view.Exchange, view.ExchangeLogoPath);
          }
          catch (Exception e)
          {
            await ShowPopupMessageAsync(e.Message);
          }

        return view.Exchange;
      }
      return null;
    }

    public async Task<Exchange?> ShowUpdateExchangeAsync(Exchange exchange)
    {
      WinCoreUI.Views.ExchangeView view = new WinCoreUI.Views.ExchangeView((Exchange)exchange.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Update Exchange",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();

      if (result == ContentDialogResult.Primary)
      {
        try
        {
          Exchange.ReplaceLogo(view.Exchange, view.ExchangeLogoPath);
        }
        catch (Exception e)
        {
          await ShowPopupMessageAsync(e.Message);
        }

        return view.Exchange;
      }

      return null;
    }

    public async Task<Session?> ShowCreateSessionAsync(Guid parentId)
    {
      WinCoreUI.Views.SessionView view = new WinCoreUI.Views.SessionView(parentId);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Create Session",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.Session;

      return null;
    }

    public async Task<Session?> ShowUpdateSessionAsync(Session session)
    {
      WinCoreUI.Views.SessionView view = new WinCoreUI.Views.SessionView((Session)session.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Title = "Update Session",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.Session;

      return null;
    }

    public async Task<Instrument> ShowCreateInstrumentAsync()
    {
      WinCoreUI.Views.InstrumentView view = new WinCoreUI.Views.InstrumentView();
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Create Instrument",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.Instrument;

      return null;
    }

    public async Task<Instrument> ShowUpdateInstrumentAsync(Instrument instrument)
    {
      WinCoreUI.Views.InstrumentView view = new WinCoreUI.Views.InstrumentView((Instrument)instrument.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Update Instrument",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.Instrument;

      return null;
    }

    public async Task<ImportSettings?> ShowImportInstrumentsAsync()
    {
      WinCoreUI.Views.ImportView view = new WinCoreUI.Views.ImportView(false, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Import Instruments",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Width = 575,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ImportSettings;

      return null;
    }

    public async Task<ExportSettings?> ShowExportInstrumentsAsync()
    {
      WinCoreUI.Views.ExportView view = new WinCoreUI.Views.ExportView(false, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Export Instruments",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Width = 575,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ExportSettings;

      return null;
    }

    public async Task<InstrumentGroup> ShowCreateInstrumentGroupAsync(Guid parentId)
    {
      WinCoreUI.Views.InstrumentGroupView view = new WinCoreUI.Views.InstrumentGroupView(parentId);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Create Instrument Group",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.InstrumentGroup;

      return null;
    }

    public async Task<InstrumentGroup> ShowUpdateInstrumentGroupAsync(InstrumentGroup instrumentGroup)
    {
      WinCoreUI.Views.InstrumentGroupView view = new WinCoreUI.Views.InstrumentGroupView((InstrumentGroup)instrumentGroup.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Update Instrument Group",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.InstrumentGroup;

      return null;
    }

    public async Task<ImportSettings?> ShowImportInstrumentGroupsAsync()
    {
      WinCoreUI.Views.ImportView view = new WinCoreUI.Views.ImportView(false, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Import Instrument Groups",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Width = 575,
        MaxWidth = 1920,
        MaxHeight = 1080,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ImportSettings;

      return null;
    }

    public async Task<ExportSettings?> ShowExportInstrumentGroupsAsync()
    {
      WinCoreUI.Views.ExportView view = new WinCoreUI.Views.ExportView(false, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Export Instrument Groups",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Width = 575,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ExportSettings;

      return null;
    }

    public async Task<IBarData?> ShowCreateBarDataAsync(Resolution resolution, DateTime dateTime)
    {
      WinCoreUI.Views.InstrumentBarDataView view = new WinCoreUI.Views.InstrumentBarDataView();
      view.Resolution = resolution;
      view.Date = dateTime.Date;
      view.Time = dateTime.TimeOfDay;
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Create new bar data",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.BarData;

      return null;
    }

    public async Task<IBarData?> ShowUpdateBarDataAsync(IBarData barData)
    {
      WinCoreUI.Views.InstrumentBarDataView view = new WinCoreUI.Views.InstrumentBarDataView(barData.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Create new bar data",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.BarData;

      return null;
    }

    public async Task<ImportSettings?> ShowImportBarDataAsync()
    {
      WinCoreUI.Views.ImportView view = new WinCoreUI.Views.ImportView(true, false);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Import Bar Data",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Width = 575,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ImportSettings;

      return null;
    }

    public async Task<ExportSettings?> ShowExportBarDataAsync()
    {
      WinCoreUI.Views.ExportView view = new WinCoreUI.Views.ExportView(true, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = getInitNavigationService().Frame.XamlRoot,
        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
        Title = "Export Bar Data",
        Content = view,
        PrimaryButtonText = "OK",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary,
        Width = 575,
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ExportSettings;

      return null;
    }

    public Task ShowMassDataImportAsync(string dataProvider)
    {
      //https://learn.microsoft.com/en-us/windows/apps/get-started/samples#windows-app-sdk--winui-3-samples
      Window window = new Window();
      window.Title = "Mass Import of Instrument Data";
      WinCoreUI.Views.MassImportInstrumentDataView importView = new WinCoreUI.Views.MassImportInstrumentDataView();
      importView.ParentWindow = window;   //set so view can close the window
      importView.DataProvider = dataProvider;
      window.Content = importView;
      window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1170, 850));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
      ResetSizeable(window);
      window.Activate();
      return Task.CompletedTask;
    }

    public Task ShowMassDataExportAsync(string dataProvider)
    {
      Window window = new Window();
      window.Title = "Mass Export of Instrument Data";
      WinCoreUI.Views.MassExportInstrumentDataView exportView = new WinCoreUI.Views.MassExportInstrumentDataView();
      exportView.ParentWindow = window;   //set so view can close the window
      exportView.DataProvider = dataProvider;
      window.Content = exportView;
      window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1170, 930));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
      ResetSizeable(window);
      window.Activate();
      return Task.CompletedTask;
    }

    public Task ShowMassDataCopyAsync(string dataProvider)
    {
      Window window = new Window();
      window.Title = "Mass Copy of Instrument Data";
      WinCoreUI.Views.MassCopyInstrumentDataView copyView = new WinCoreUI.Views.MassCopyInstrumentDataView();
      copyView.ParentWindow = window;   //set so view can close the window
      copyView.DataProvider = dataProvider;
      window.Content = copyView;
      window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1170, 520));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
      ResetSizeable(window);
      window.Activate();
      return Task.CompletedTask;
    }

    public Task ShowMassDataDownloadAsync(string dataProvider)
    {
      Window window = new Window();
      window.Title = "Mass Download of Instrument Data";
      WinCoreUI.Views.MassDownloadInstrumentDataView downloadView = new WinCoreUI.Views.MassDownloadInstrumentDataView();
      downloadView.ParentWindow = window;   //set so view can close the window
      downloadView.DataProvider = dataProvider;
      window.Content = downloadView;
      //window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1170, 660));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
      window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(2000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
      ResetSizeable(window);
      window.Activate();
      return Task.CompletedTask;
    }

    public Task ShowAccountDialogAsync()
    {
      return Task.CompletedTask;
    }

    public Task ShowAccountDialogAsync(Account account)
    {
      return Task.CompletedTask;
    }

    //properties
    public FontIcon StatusBarIcon { get; set; }
    public TextBlock StatusBarText { get; set; }
    public DispatcherQueue UIDispatcherQueue { get; set; }

    //methods
    internal static void ResetSizeable(Window window)
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(window);
      var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
      SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_SIZEBOX));
    }

    internal static void HideMinimizeAndMaximizeButtons(Window window)
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(window);
      var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
      SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
    }

    internal static void MakeDialog(Window window)
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(window);
      var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
      SetWindowLong(hwnd, GWL_STYLE, (currentStyle & WS_DLGFRAME));
    }
  }
}
