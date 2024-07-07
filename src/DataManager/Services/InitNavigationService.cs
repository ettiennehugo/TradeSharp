using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TradeSharp.WinCoreUI.Services;

namespace TradeSharp.WinDataManager.Services
{
  /// <summary>
  /// Special service to initialise the navigation service at application startup.
  /// </summary>
  public class InitNavigationService: IInitNavigationService
  {
    //constants


    //enums


    //types


    //attributes


    //constructors


    //finalizers


    //interface implementations


    //properties
    private Frame? m_frame;
    public Frame Frame => m_frame ?? throw new InvalidOperationException($"{nameof(InitNavigationService)} not initalized");

    private Dictionary<string, Type>? m_pages;
    public Dictionary<string, Type> Pages => m_pages ?? throw new InvalidOperationException($"{nameof(InitNavigationService)} not initalized");

    //methods
    /// <summary>
    /// Setup the navigation service initialisation properties during application startup.
    /// </summary>
    public void Initialize(Frame frame, Dictionary<string, Type> pages)
    {
      m_frame = frame ?? throw new ArgumentNullException(nameof(frame));
      m_pages = pages ?? throw new ArgumentNullException(nameof(pages));
    }
  }
}
