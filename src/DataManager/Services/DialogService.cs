using System;
using WinRT.Interop;
using System.Threading.Tasks;
using Windows.UI.Popups;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Services;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.Common;
using TradeSharp.Data;
using Microsoft.UI.Dispatching;
using static TradeSharp.CoreUI.Services.IDialogService;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

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


    //interface implementations
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

    public async Task<CountryInfo?> ShowSelectCountryAsync()
    {
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.CountrySelectorView view = new WinCoreUI.Views.CountrySelectorView();
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.HolidayView view = new WinCoreUI.Views.HolidayView(parentId);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.HolidayView view = new WinCoreUI.Views.HolidayView((Holiday)holiday.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.ExchangeView view = new WinCoreUI.Views.ExchangeView();
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.ExchangeView view = new WinCoreUI.Views.ExchangeView((Exchange)exchange.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.SessionView view = new WinCoreUI.Views.SessionView(parentId);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.SessionView view = new WinCoreUI.Views.SessionView((Session)session.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.InstrumentView view = new WinCoreUI.Views.InstrumentView();
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.InstrumentView view = new WinCoreUI.Views.InstrumentView((Instrument)instrument.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.ImportView view = new WinCoreUI.Views.ImportView(false, false, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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

    public async Task<string?> ShowExportInstrumentsAsync()
    {
      FileSavePicker savePicker = new FileSavePicker();
      savePicker.DefaultFileExtension = ".json";  //JSON allows better structure of the instrument definitions
      savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
      savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" }); //default export to JSON
      savePicker.FileTypeChoices.Add("CSV", new List<string>() { ".csv" });

      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(savePicker, hwnd);

      StorageFile file = await savePicker.PickSaveFileAsync();
      if (file != null) return file.Path;

      return null;
    }

    public async Task<InstrumentGroup> ShowCreateInstrumentGroupAsync(Guid parentId)
    {
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.InstrumentGroupView view = new WinCoreUI.Views.InstrumentGroupView(parentId);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.InstrumentGroupView view = new WinCoreUI.Views.InstrumentGroupView((InstrumentGroup)instrumentGroup.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.ImportView view = new WinCoreUI.Views.ImportView(false, false, true);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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

    public async Task<string?> ShowExportInstrumentGroupsAsync()
    {
      //https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/filepicker/
      FileSavePicker savePicker = new FileSavePicker();
      savePicker.DefaultFileExtension = ".json";  //JSON allows better structure of the instrument group definitions
      savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
      savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" }); //default export to JSON
      savePicker.FileTypeChoices.Add("CSV", new List<string>() { ".csv" });

      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(savePicker, hwnd);

      StorageFile file = await savePicker.PickSaveFileAsync();
      if (file != null) return file.Path;

      return null;
    }

    public async Task<IBarData?> ShowCreateBarDataAsync(Resolution resolution, DateTime dateTime, PriceDataType priceDataType)
    {
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.InstrumentBarDataView view = new WinCoreUI.Views.InstrumentBarDataView();
      view.Resolution = resolution;
      view.Date = dateTime.Date;
      view.Time = dateTime.TimeOfDay;
      view.PriceDataType = priceDataType;
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.InstrumentBarDataView view = new WinCoreUI.Views.InstrumentBarDataView(barData.Clone());
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      WinCoreUI.Views.ImportView view = new WinCoreUI.Views.ImportView(true, true, false);
      ContentDialog dialog = new ContentDialog()
      {
        XamlRoot = initNavigationService.Frame.XamlRoot,
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

    public async Task<string?> ShowExportBarDataAsync()
    {
      //https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/filepicker/
      FileSavePicker savePicker = new FileSavePicker();
      savePicker.DefaultFileExtension = ".csv";
      savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
      savePicker.FileTypeChoices.Add("CSV", new List<string>() { ".csv" }); //default to CSV as it is more compact and faster to write
      savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" }); 

      var hwnd = GetActiveWindow();
      InitializeWithWindow.Initialize(savePicker, hwnd);

      StorageFile file = await savePicker.PickSaveFileAsync();
      if (file != null) return file.Path;

      return null;
    }


    //properties
    public FontIcon StatusBarIcon { get; set; }
    public TextBlock StatusBarText { get; set; }
    public ProgressRing StatusBarProgress { get; set; }

    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

  }
}
