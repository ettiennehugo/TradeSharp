using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace TradeSharp.WinCoreUI.Services
{
	/// <summary>
	/// Interface for Windows XAML-based application navigation service initialization.
	/// Mainly sets the XAML frame to the main window and pages for the navigation service.
	/// </summary>
  public interface IInitNavigationService
  {
    //constants


    //enums


    //types


    //attributes


    //properties
    Frame Frame { get; }
    Dictionary<string, Type> Pages { get; }

    //methods
    void Initialize(Frame frame, Dictionary<string, Type> pages);

  }
}
