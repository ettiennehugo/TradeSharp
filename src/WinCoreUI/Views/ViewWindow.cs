using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;
using WinRT.Interop;
using System.Runtime.InteropServices;

namespace TradeSharp.WinCoreUI.Views
{
  /// <summary>
  /// View window class to host page views.
  /// </summary>
  public class ViewWindow: Window
  {
    //constants
    // from winuser.h
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/
    // https://learn.microsoft.com/en-us/windows/win32/winmsg/window-styles
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x10000;
    private const int WS_MINIMIZEBOX = 0x20000;
    private const int WS_SYSMENU = 0x80000;
    private const int WS_DLGFRAME = 0x400000;
    private const int WS_SIZEBOX = 0x40000;

    //enums


    //types


    //attributes
    protected IWindowedView m_view;

    //properties
    public IWindowedView View { 
    get => m_view; 
    set {
        m_view = value;
        Content = m_view.UIElement;
      }
    }

		//constructors
		public ViewWindow(): base() { }

    //finalizers


    //interface implementations


    //methods
    [DllImport("user32.dll")]
    extern private static IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    extern private static int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

    public void ResetSizeable()
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(this);
      var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
      SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_SIZEBOX));
    }

    public void HideMinimizeAndMaximizeButtons()
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(this);
      var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
      SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
    }

    public void MakeDialog()
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(this);
      var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
      SetWindowLong(hwnd, GWL_STYLE, (currentStyle & WS_DLGFRAME));
    }

    /// <summary>
    /// Center the window on the screen.
    /// </summary>
    public void CenterWindow()
    {
      IntPtr hwnd = WindowNative.GetWindowHandle(this);
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
