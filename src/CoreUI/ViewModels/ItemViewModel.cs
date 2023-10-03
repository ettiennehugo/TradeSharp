using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model base class to display items in read-only mode (no editing operations, see EditableItemViewModel for editable item).
  /// </summary>
  public abstract partial class ItemViewModel<T> : ViewModelBase, IItemViewModel<T>
  {

    //constants


    //enums


    //types


    //attributes
    [ObservableProperty] private T? m_item;

    //constructors
    public ItemViewModel(T? item, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_item = item;
    }

    //finalizers


    //interface implementations


    //properties


    //methods


  }
}
