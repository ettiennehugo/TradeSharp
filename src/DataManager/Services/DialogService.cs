using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinRT.Interop;
using System.Threading.Tasks;
using Windows.UI.Popups;
using System.Runtime.InteropServices;
using TradeSharp.CoreUI.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using System.Diagnostics.Eventing.Reader;
using CommunityToolkit.Mvvm.DependencyInjection;
using TradeSharp.Common;
using Windows.Globalization;
using TradeSharp.CoreUI.ViewModels;
using TradeSharp.Data;

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
      Views.CountrySelectorView view = new Views.CountrySelectorView();
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

      if (result == ContentDialogResult.Primary)
        return view.SelectedCountry;
      else
        return null;
    }

    public async Task<Holiday> ShowCreateHolidayAsync(Guid parentId)
    {
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      Views.HolidayView view = new Views.HolidayView(parentId);
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

      if (result == ContentDialogResult.Primary)
        return view.Holiday;
      else
        return null;
    }

    public async Task<Holiday> ShowUpdateHolidayAsync(Holiday holiday)
    {
      InitNavigationService initNavigationService = Ioc.Default.GetRequiredService<InitNavigationService>();
      Views.HolidayView view = new Views.HolidayView(holiday);
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

      if (result == ContentDialogResult.Primary)
        return view.Holiday;
      else
        return null;
    }

    //properties


    //methods
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

  }
}
