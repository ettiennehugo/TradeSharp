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
using System.IO;

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
    public async Task ShowMessageAsync(string message)
    {
      MessageDialog dlg = new(message);
      var hwnd = GetActiveWindow();
      if (hwnd == IntPtr.Zero)
        throw new InvalidOperationException();
      InitializeWithWindow.Initialize(dlg, hwnd);
      await dlg.ShowAsync();
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
        string logoFilename = Exchange.GetExchangeLogoPath(view.Exchange.LogoId);
        if (view.ExchangeLogoPath != logoFilename)
        {
          bool removeCurrentLogo = true;
          if (view.Exchange.LogoId == Guid.Empty)
          {
            removeCurrentLogo = false;  //no logo yet assigned so we don't want to delete the blank/empty logo used
            view.Exchange.LogoId = Guid.NewGuid();
          }

          try
          {
            logoFilename = Exchange.CreateExchangeLogoPath(view.Exchange.LogoId, Path.GetExtension(view.ExchangeLogoPath));
            if (removeCurrentLogo) File.Delete(logoFilename);    //ensure that we do not keep stale file around since new file extension can be different from current file extension
            File.Copy(view.ExchangeLogoPath, logoFilename);
          }
          catch (Exception e)
          {
            await ShowMessageAsync(e.Message);
          }
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
          string newLogoFilename = Exchange.CreateExchangeLogoPath(view.Exchange.LogoId, Path.GetExtension(view.ExchangeLogoPath));
          if (view.Exchange.LogoPath != Exchange.BlankLogoPath && File.Exists(view.Exchange.LogoPath)) File.Delete(view.Exchange.LogoPath);    //ensure that we do not keep stale file around since new file extension can be different from current file extension
          File.Copy(view.ExchangeLogoPath, newLogoFilename);
          view.Exchange.LogoPath = newLogoFilename;
        }
        catch (Exception e)
        {
          await ShowMessageAsync(e.Message);
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

    //properties


    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

  }
}
