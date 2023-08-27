using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeSharp.CoreUI;

namespace TradeSharp.CoreUI
{
  public abstract class ItemViewModel<T> : ViewModelBase, IItemViewModel<T>
  {
    public ItemViewModel(T? item) => _item = item;
    private T? _item;
    public virtual T? Item
    {
      get => _item;
      set => SetProperty(ref _item, value);
    }
  }
}
