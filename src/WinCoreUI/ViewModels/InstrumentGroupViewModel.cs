using TradeSharp.CoreUI.Services;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System;
using TradeSharp.Data;

namespace TradeSharp.WinCoreUI.ViewModels
{
  /// <summary>
  /// Windows specific implementation of the InstrumentGroupViewModel to facilitate Windows specific functionality.
  /// </summary>
  public class InstrumentGroupViewModel : CoreUI.ViewModels.InstrumentGroupViewModel
  {
    //constants


    //enums


    //types


    //attributes


    //constructors
    public InstrumentGroupViewModel(IInstrumentGroupService instrumentGroupService, IInstrumentService instrumentService, INavigationService navigationService, IDialogService dialogService) : base(instrumentGroupService, instrumentService, navigationService, dialogService) { }

    //finalizers


    //interface implementations


    //properties
    /// <summary>
    /// UI thread dispatcher queue to signal UI state changes.
    /// </summary>
    public DispatcherQueue UIDispatcherQueue { get; set; }

    /// <summary>
    /// Get/set the find text for the view model and associated items service and route notifications to the UI thread.
    /// </summary>
    public override string FindText
    {
      get => m_findText;
      set
      {
        m_findText = value;
        if (UIDispatcherQueue != null) 
          UIDispatcherQueue.TryEnqueue(() => {
            OnPropertyChanged();
            NotifyCanExecuteChanged();
          });
        else
        {
          OnPropertyChanged();
          NotifyCanExecuteChanged();
        }
      }
    }

    /// <summary>
    /// Get/set single selected node for the view model and associated item service and route notifications to the UI thread.
    /// </summary>
    public override ITreeNodeType<Guid, InstrumentGroup>? SelectedNode
    {
      get => m_itemsService.SelectedNode;
      set
      {
        m_itemsService.SelectedNode = value;
        if (UIDispatcherQueue != null)
          UIDispatcherQueue.TryEnqueue(() => {
            OnPropertyChanged();
            NotifyCanExecuteChanged();
          });
        else
        {
          OnPropertyChanged();
          NotifyCanExecuteChanged();
        }
      }
    }

    /// <summary>
    /// Get/set selected set of nodes for the view model and associated items service and route notifications to the UI thread.
    /// </summary>
    public override ObservableCollection<ITreeNodeType<Guid, InstrumentGroup>> SelectedNodes
    {
      get => m_itemsService.SelectedNodes;
      set
      {
        m_itemsService.SelectedNodes = value;
        if (UIDispatcherQueue != null)
          UIDispatcherQueue.TryEnqueue(() => {
            OnPropertyChanged();
            NotifyCanExecuteChanged();
          });
        else
        {
          OnPropertyChanged();
          NotifyCanExecuteChanged();
        }
      }
    }

    //methods
    /// <summary>
    /// Route node expansion onto the UI thread.
    /// </summary>
    protected override void expandToNode(ITreeNodeType<Guid, InstrumentGroup> node)
    {
      if (UIDispatcherQueue != null)
        UIDispatcherQueue.TryEnqueue(() => base.expandToNode(node));
      else
        base.expandToNode(node);
    }
  }
}
