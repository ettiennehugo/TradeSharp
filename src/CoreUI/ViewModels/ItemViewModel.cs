using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.CoreUI.ViewModels
{
  /// <summary>
  /// View model base class to display items in read-only mode (no editing operations, see EditableItemViewModel for editable item).
  /// </summary>
  public abstract class ItemViewModel<T> : ViewModelBase, IItemViewModel<T>
  {

    //constants


    //enums


    //types


    //attributes
    private T? m_item;

    //constructors
    public ItemViewModel(T? item, INavigationService navigationService, IDialogService dialogService) : base(navigationService, dialogService)
    {
      m_item = item;
    }

    //finalizers


    //interface implementations


    //properties
    public virtual T? Item
    {
      get => m_item;
      set => SetProperty(ref m_item, value);
    }

    //methods


  }
}
