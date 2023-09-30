using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI.ViewModels
{
    /// <summary>
    /// Detail item view model interface.
    /// </summary>
    public interface IItemViewModel<out T>
    {
        T? Item { get; }
    }
}
