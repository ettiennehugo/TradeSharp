using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Events;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.WinDataManager.Services
{
  /// <summary>
  /// Windows specific navigation service for the data manager.
  /// </summary>


  //TODO: Implement this class.


  public class NavigationService : ObservableObject, INavigationService, IRecipient<NavigationMessage>
  {
    //constants


    //enums


    //types


    //attributes
    private readonly InitNavigationService m_initNavigationService;

    //constructors
    public NavigationService(InitNavigationService initNavigationService)
    {
      m_initNavigationService = initNavigationService;
      WeakReferenceMessenger.Default.Register<NavigationMessage>(this);
    }

    //finalizers


    //interface implementations


    //properties
    private bool m_useNavigation;
    public bool UseNavigation
    {
      get => m_useNavigation;
      set => SetProperty(ref m_useNavigation, value);
    }

    private string m_currentPage = string.Empty;
    public string CurrentPage => m_currentPage;

    private Frame? _frame;
    private Frame Frame => _frame ??= m_initNavigationService.Frame;

    private Dictionary<string, Type>? _pages;
    private Dictionary<string, Type> Pages => _pages ??= m_initNavigationService.Pages;

    //methods
    public Task GoBackAsync()
    {
      PageStackEntry stackEntry = Frame.BackStack.Last();
      Type backPageType = stackEntry.SourcePageType;
      var pageEntry = Pages.FirstOrDefault(pair => pair.Value == backPageType);
      m_currentPage = pageEntry.Key;
      Frame.GoBack();
      return Task.CompletedTask;
    }

    public Task NavigateToAsync(string pageName)
    {
      m_currentPage = pageName;
      Frame.Navigate(Pages[pageName]);
      return Task.CompletedTask;
    }

    public void Receive(NavigationMessage message)
    {
      UseNavigation = message.Value.UseNavigation;
    }
  }
}
