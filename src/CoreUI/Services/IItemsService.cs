using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeSharp.CoreUI.Services
{
    /// <summary>
    /// Interface to be implemented by services that allow the mnipulation of items. Services are defined to support dependency injection in MVVM.
    /// The service overall supports viewing all the items and then viewing the details of a single item (SelectedItem)
    /// </summary>
    public interface IItemsService<T>
    {
        //constants


        //enums


        //types


        //attributes


        //properties
        Guid ParentId { get; set; }
        T? SelectedItem { get; set; }
        ObservableCollection<T> Items { get; }

        //events
        event EventHandler<T>? SelectedItemChanged;

        //methods
        Task RefreshAsync();
        Task<T> AddAsync(T item);
        Task<T> UpdateAsync(T item);
        Task<bool> DeleteAsync(T item);

    }
}
