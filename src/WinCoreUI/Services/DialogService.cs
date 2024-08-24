using System;
using WinRT.Interop;
using System.Threading.Tasks;
using Windows.UI.Popups;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.UI.Dispatching;
using static TradeSharp.CoreUI.Services.IDialogService;
using Microsoft.UI.Xaml;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Windows.Graphics;
using TradeSharp.WinCoreUI.Services;
using TradeSharp.WinCoreUI.Views;

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

    public async Task<T> PostUIUpdateAsync<T>(Func<T> updateFunc)
    {
      var tcs = new TaskCompletionSource<T>();

      UIDispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
      {
        try
        {
          var result = updateFunc();
          tcs.SetResult(result);
        }
        catch (Exception ex)
        {
          tcs.SetException(ex);
        }
      });

      return await tcs.Task;
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

    public async Task ShowStatusMessageAsync(StatusMessageSeverity severity, string title, string message)
    {
      await Task.Run(() =>
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
      });
    }

    /// <summary>
    /// Creates a progress dialog with the specified title and also takes a logger to echo log operations.
    /// </summary>
    public IProgressDialog CreateProgressDialog(string title, ILogger? logger)
    {
      IProgressDialog? result = null;

      //create the dialog from the UI thread
      TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
      if (UIDispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>    //dialog creation needs precedence since other logs might still be incrementally loading
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.ProgressDialogView progressDialog = new WinCoreUI.Views.ProgressDialogView(window, title, logger);
        result = progressDialog;
        taskCompletionSource.SetResult(true);
      }))
        Task.WaitAll(taskCompletionSource.Task);

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
      Task.WaitAll(taskCompletionSource.Task);
      
      //caller needs to explicitly call the show, since it needs to setup some of the members before the progress dialog is shown
      return result!;
    }

    protected IInitNavigationService getInitNavigationService() => (IInitNavigationService)((IApplication)Application.Current).Services.GetService(typeof(IInitNavigationService));

    public void ShowCreateCountryAsync()
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.CountrySelectorView view = new WinCoreUI.Views.CountrySelectorView(window);
        window.Title = "Select a country";
        window.Activate();
      });
    }

    public Task<Holiday?> ShowCreateHolidayAsync(Guid parentId)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.HolidayView view = new WinCoreUI.Views.HolidayView(parentId, window);
        window.Title = "Create holiday";
        window.Activate();
      });

      return Task.FromResult<Holiday?>(null);
    }

    public Task<Holiday?> ShowUpdateHolidayAsync(Holiday holiday)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.HolidayView view = new WinCoreUI.Views.HolidayView((Holiday)holiday.Clone(), window);
        window.Title = $"Update holiday - {holiday.Name}";
        window.Activate();
      });

      return Task.FromResult<Holiday?>(null);
    }

    public Task<Exchange?> ShowCreateExchangeAsync()
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.ExchangeView view = new WinCoreUI.Views.ExchangeView(window);
        window.Title = "Create exchange";
        window.Activate();
      });

      return Task.FromResult<Exchange?>(null);
    }

    public Task<Exchange?> ShowUpdateExchangeAsync(Exchange exchange)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.ExchangeView view = new WinCoreUI.Views.ExchangeView((Exchange)exchange.Clone(), window);
        window.Title = $"Update exchange - {exchange.Name}";
        window.Activate();
      });

      return Task.FromResult<Exchange?>(null);
    }

    public Task<Session?> ShowCreateSessionAsync(Guid parentId)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.SessionView view = new WinCoreUI.Views.SessionView(parentId, window);
        window.Title = "Create session";
        window.Activate();
      });

      return Task.FromResult<Session?>(null);
    }

    public Task<Session?> ShowUpdateSessionAsync(Session session)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.SessionView view = new WinCoreUI.Views.SessionView((Session)session.Clone(), window);
        window.Title = "Create session";
        window.Activate();
      });

      return Task.FromResult<Session?>(null);
    }

    public Task<Instrument> ShowCreateInstrumentAsync(InstrumentType instrumentType)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.InstrumentView view = new WinCoreUI.Views.InstrumentView(instrumentType, window);
        window.Title = $"Create new instrument";
        window.Activate();
      });

      return Task.FromResult<Instrument?>(null);
    }

    public Task<Instrument?> ShowUpdateInstrumentAsync(Instrument instrument)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.InstrumentView view = new WinCoreUI.Views.InstrumentView((Instrument)instrument.Clone(), window);
        window.Title = $"Update - {instrument.Ticker}";
        window.Activate();
      });

      return Task.FromResult<Instrument?>(null);
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

    public Task<InstrumentGroup?> ShowCreateInstrumentGroupAsync(Guid parentId)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.InstrumentGroupView view = new WinCoreUI.Views.InstrumentGroupView(parentId, window);
        window.Title = $"Create instrument group";
        window.Activate();
      });

      return Task.FromResult<InstrumentGroup?>(null);
    }

    public Task<InstrumentGroup> ShowUpdateInstrumentGroupAsync(InstrumentGroup instrumentGroup)
    {
      PostUIUpdate(() =>
      {
        ViewWindow window = new ViewWindow();
        WinCoreUI.Views.InstrumentGroupView view = new WinCoreUI.Views.InstrumentGroupView(instrumentGroup, window);
        window.Title = $"Update - {instrumentGroup.Name}";
        window.Activate();
      });

      return Task.FromResult<InstrumentGroup>(null);
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
        Width = 575
      };

      ContentDialogResult result = await dialog.ShowAsync();
      if (result == ContentDialogResult.Primary) return view.ExportSettings;

      return null;
    }

    public async Task ShowMassDataImportAsync(string dataProvider)
    {
      PostUIUpdate(() =>
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
        CenterWindow(window);
        window.Activate();
      });
    }

    public async Task ShowMassDataExportAsync(string dataProvider)
    {
      PostUIUpdate(() => {
        Window window = new Window();
        window.Title = "Mass Export of Instrument Data";
        WinCoreUI.Views.MassExportInstrumentDataView exportView = new WinCoreUI.Views.MassExportInstrumentDataView();
        exportView.ParentWindow = window;   //set so view can close the window
        exportView.DataProvider = dataProvider;
        window.Content = exportView;
        //window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1170, 930));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(2000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        ResetSizeable(window);
        CenterWindow(window);
        window.Activate();
      }); 
    }

    public async Task ShowMassDataCopyAsync(string dataProvider)
    {
      PostUIUpdate(() =>
      {
        Window window = new Window();
        window.Title = "Mass Copy of Instrument Data";
        WinCoreUI.Views.MassCopyInstrumentDataView copyView = new WinCoreUI.Views.MassCopyInstrumentDataView();
        copyView.ParentWindow = window;   //set so view can close the window
        copyView.DataProvider = dataProvider;
        window.Content = copyView;
        //window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1170, 520));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(2000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        ResetSizeable(window);
        CenterWindow(window);
        window.Activate();
      });
    }

    public async Task ShowMassDataDownloadAsync(string dataProvider)
    {
      PostUIUpdate(() => {
        Window window = new Window();
        window.Title = "Mass Download of Instrument Data";
        WinCoreUI.Views.MassDownloadInstrumentDataView downloadView = new WinCoreUI.Views.MassDownloadInstrumentDataView();
        downloadView.ParentWindow = window;   //set so view can close the window
        downloadView.DataProvider = dataProvider;
        window.Content = downloadView;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(2000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        ResetSizeable(window);
        CenterWindow(window);
        window.Activate();
      });
    }

    /// <summary>
    /// Shows the account dialog with all the brokers and their associated accounts.
    /// </summary>
    public async Task ShowAccountDialogAsync()
    {
      PostUIUpdate(() => {
        Window window = new Window();
        window.Title = "Accounts";
        WinCoreUI.Views.AccountsView accountsView = new WinCoreUI.Views.AccountsView();
        accountsView.ParentWindow = window;
        window.Content = accountsView;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(3000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        ResetSizeable(window);
        CenterWindow(window);
        window.Activate();
      });
    }

    /// <summary>
    /// Shows the account dialog for the specified broker.
    /// </summary>
    public async Task ShowAccountDialogAsync(IBrokerPlugin broker)
    {
      PostUIUpdate(() =>
      {
        Window window = new Window();
        window.Title = $"Accounts - {broker.Name}";
        WinCoreUI.Views.AccountsView accountsView = new WinCoreUI.Views.AccountsView(broker);
        accountsView.ParentWindow = window;
        window.Content = accountsView;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(3000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        ResetSizeable(window);
        CenterWindow(window);
        window.Activate();
      });
    }

    /// <summary>
    /// Shows the account dialog for the specified account.
    /// </summary>
    public Task ShowAccountDialogAsync(IBrokerPlugin broker, Data.Account account)
    {
      PostUIUpdate(() =>
      {
        Window window = new Window();
        window.Title = $"Account - {account.Name}";
        WinCoreUI.Views.AccountView accountsView = new WinCoreUI.Views.AccountView(broker, account);
        accountsView.ParentWindow = window;
        window.Content = accountsView;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(3000, 2000));   //NOTE: Setting the client size from the download view actual width/height does not work since those values are not computed correctly.
        ResetSizeable(window);
        CenterWindow(window);
        window.Activate();
      });
      return Task.CompletedTask;
    }

    public Task ShowNewChartAsync()
    {

      //TODO: Implement chart Window
      ShowPopupMessageAsync("Chart Window not implemented yet");

      return Task.CompletedTask;

    }
    
    public Task ShowNewScannerAsync()
    {

      //TODO: Implement scanner Window
      ShowPopupMessageAsync("Chart Window not implemented yet");

      return Task.CompletedTask;

    }

    public Task ShowNewEventStudyAsync()
    {

      //TODO: Implement scanner Window
      ShowPopupMessageAsync("Event Study Window not implemented yet");

      return Task.CompletedTask;
    }


    public Task ShowNewPortfolioAsync()
    {
      
      //TODO: Implement portfolio Window
      ShowPopupMessageAsync("Portfolio Window not implemented yet");

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

    internal static void CenterWindow(Window window)
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(window);
      WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
      if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
          DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
      {
        PointInt32 centeredPosition = appWindow.Position;
        centeredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
        centeredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
        appWindow.Move(centeredPosition);
      }
    }
  }
}
