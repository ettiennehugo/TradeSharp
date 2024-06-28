using System.Threading;
using System.Collections.ObjectModel;
using TradeSharp.Data;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Events;

namespace TradeSharp.CoreUI.Services
{
  /// <summary>
  /// Instrument cache shared by instrument services.
  /// </summary>
  public interface IInstrumentCacheService
  {
    //constants


    //enums


    //types


    //attributes


    //events
    event ItemAddedEventHandler? ItemAdded;
    event ItemUpdatedEventHandler? ItemUpdated;
    event ItemRemovedEventHandler? ItemRemoved;
    event RefreshEventHandler? Refreshed;

    //properties
    ObservableCollection<Instrument> Items { get; }
    LoadedState LoadedState { get; }

    //methods
    void Refresh();
    Task RefreshAsync();
    void Add(Instrument item);
    void Update(Instrument item);
    void Delete(Instrument item);
  }
}
